using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {
    
    /// <summary>
    /// A spline that is defined by a number of points and one (quadratic curve) or two (cubic curve) control points
    /// in between each pair of points. The spline visits the former points in order. Its trajectory between two
    /// points is influenced by the control point(s) between them, as it goes "in the direction" of these, usually
    /// without actually touching them.
    ///
    /// The derivative (tangent / velocity) has a continuous direction iff every point is collinear with
    /// the two control points before and after it. Its magnitude is continuous as well iff these two
    /// control points are also at the same distance from their shared point.
    /// A bezier spline is a good choice when you want explicit control over the spline's trajectory. However,
    /// this also makes it easier to get unwanted results if you're not careful. For example, non-collinear
    /// control points around a position produce sudden kinks and badly-placed control points can lead to small
    /// loops within a segment.
    /// It is also possible to define the velocity and optionally even the acceleration at each point instead
    /// of defining control points (see the <see cref="BezierSpline"/> factory). This makes it possible to construct
    /// splines with not only continuous velocity, but also acceleration / curvature. This is desirable for
    /// animations since humans are very good at seeing discontinuous acceleration in a movement, which makes
    /// it look less smooth.
    /// </summary>
    [Serializable]
    public sealed class BezierSpline<TPos, TDiff, TTime, TVel> : Spline<TPos, TDiff, TTime, TVel>.Copyable 
    where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged
    {

        internal sealed record Cache(
            ImmutableArray<SplineSegment<TPos, TDiff, TTime, TVel>> SplineSegments,
            Length Length,
            TTime TotalDuration,
            ImmutableArray<Length> SpatialSegmentOffsets,
            ImmutableArray<TTime> TemporalSegmentOffsets
        );

        public readonly ImmutableArray<BezierSegment<TPos, TDiff, TTime, TVel>> Segments;
        [NonSerialized] private readonly Lazy<Cache> cache;
        public int SegmentCount => Segments.Length;

        public Length Length => cache.Value.Length;
        public TTime TotalDuration => cache.Value.TotalDuration;

        IEnumerable<SplineSegment<TPos, TDiff, TTime, TVel>> Spline<TPos, TDiff, TTime, TVel>.Segments => cache.Value.SplineSegments;

        public SplineSample<TPos, TDiff, TTime, TVel> this[TTime t] =>
            SplineSample<TPos, TDiff, TTime, TVel>.From(this, t);
        public SplineSegment<TPos, TDiff, TTime, TVel> this[SplineSegmentIndex index] => cache.Value.SplineSegments[index];
        public SplineSample<TPos, TDiff, TTime, TVel> this[SplineLocation location] => this[this.NormalizeOrThrow(location)];
        public SplineSample<TPos, TDiff, TTime, TVel> this[NormalizedSplineLocation location] => 
            SplineSample<TPos, TDiff, TTime, TVel>.From(this, location).GetOrThrow(() => 
                new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    $"Must be between 0 and {SegmentCount}"
                ));

        public ImmutableArray<Length> SpatialSegmentOffsets => cache.Value.SpatialSegmentOffsets;
        public ImmutableArray<TTime> TemporalSegmentOffsets => cache.Value.TemporalSegmentOffsets;

        public GeometricOperations<TPos, TDiff, TTime, TVel> Ops { get; }
        
        public BezierSpline(
            IEnumerable<BezierSegment<TPos, TDiff, TTime, TVel>> segments, 
            GeometricOperations<TPos, TDiff, TTime, TVel> ops
        ) {
            // Note CR: Serialization might pass only `default` parameters, so we support this case here
            if (segments != default) {
                Segments = segments.ToImmutableArray();
                foreach(var (l, r) in Segments.PairwiseSliding())
                    if (ops.Distance(l.End, r.Start) > Length.Epsilon)
                        throw new ArgumentException(
                            "Tried to construct a discontinuous spline. Each segment must "
                            + "start at the exact position where the previous one ended."
                        );
            } else Segments = ImmutableArray<BezierSegment<TPos, TDiff, TTime, TVel>>.Empty;
            
            Ops = ops;
            cache = new Lazy<Cache>(initCache);
        }

        [Pure] public Spline<TPosNew, TDiffNew, TTime, TVelNew> Map<TPosNew, TDiffNew, TVelNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew, TTime, TVelNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged where TVelNew : unmanaged => 
            new BezierSpline<TPosNew, TDiffNew, TTime, TVelNew>(Segments.Select(s => s.Map<TPosNew, TDiffNew, TVelNew>(positionMapping)), newOps);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> Reversed => new BezierSpline<TPos, TDiff, TTime, TVel>(Segments.Reverse().Select(s => s.Reversed), Ops);
        
        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithOffset(Func<TVel, TDiff> tangentToOffset) => 
            BezierSplineCopyUtils.CopyWithOffset(this, tangentToOffset);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithStaticOffset(TDiff offset) =>
            BezierSplineCopyUtils.CopyWithStaticOffset(this, offset);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            BezierSplineCopyUtils.CopyWithDifferentHandleDistance(this, desiredHandleDistance);

        private Cache initCache() {
            var segments = ImmutableArray.CreateRange(Segments, (s, ops) => s.ToSplineSegment(ops), Ops);
            var spatialOffsets = ImmutableArray.CreateBuilder<Length>(initialCapacity: Segments.Length);
            var temporalOffsets = ImmutableArray.CreateBuilder<TTime>(initialCapacity: Segments.Length);
            
            var lastOffset = Length.Zero;
            var lastTime = Ops.ZeroTime;

            foreach (var segment in segments) {
                if (segment.Duration.CompareTo(Ops.ZeroTime) == -1)
                    throw new Exception(
                        $"Tried to construct a spline with a segment of negative duration: {segment.Duration}"
                    );
                spatialOffsets.Add(lastOffset);
                temporalOffsets.Add(lastTime);
                lastOffset += segment.Length;
                lastTime = Ops.Add(lastTime, segment.Duration);
            }
            return new(segments, lastOffset, lastTime, spatialOffsets.MoveToImmutable(), temporalOffsets.MoveToImmutable());
        }
    }

}