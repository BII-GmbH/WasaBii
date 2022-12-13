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
    public sealed class BezierSpline<TPos, TDiff> : Spline<TPos, TDiff>.Copyable where TPos : struct where TDiff : struct {

        internal sealed record Cache(
            ImmutableArray<Lazy<SplineSegment<TPos, TDiff>>> SplineSegments
        );

        public readonly ImmutableArray<BezierSegment<TPos, TDiff>> Segments;
        [NonSerialized] private readonly Lazy<Cache> cache;
        public int SegmentCount => Segments.Length;

        IEnumerable<SplineSegment<TPos, TDiff>> Spline<TPos, TDiff>.Segments =>
            cache.Value.SplineSegments.Select(lazy => lazy.Value);

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] 
            => cache.Value.SplineSegments[index].Value;
        public SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => 
            SplineSample<TPos, TDiff>.From(this, location).GetOrThrow(() => 
                new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    $"Must be between 0 and {SegmentCount}"
                ));

        public GeometricOperations<TPos, TDiff> Ops { get; }
        
        public BezierSpline(
            IEnumerable<BezierSegment<TPos, TDiff>> segments, 
            GeometricOperations<TPos, TDiff> ops
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
            } else Segments = ImmutableArray<BezierSegment<TPos, TDiff>>.Empty;
            
            Ops = ops;
            cache = new Lazy<Cache>(initCache);
        }

        [Pure] public Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : struct where TDiffNew : struct
            => new BezierSpline<TPosNew, TDiffNew>(Segments.Select(s => s.Map<TPosNew, TDiffNew>(positionMapping)), newOps);

        [Pure] public Spline<TPos, TDiff> Reversed => new BezierSpline<TPos, TDiff>(Segments.Reverse().Select(s => s.Reversed), Ops);
        
        [Pure] public Spline<TPos, TDiff> CopyWithOffset(Func<TDiff, TDiff> tangentToOffset) => 
            BezierSplineCopyUtils.CopyWithOffset(this, tangentToOffset);

        [Pure] public Spline<TPos, TDiff> CopyWithStaticOffset(TDiff offset) =>
            BezierSplineCopyUtils.CopyWithStaticOffset(this, offset);

        [Pure] public Spline<TPos, TDiff> CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            BezierSplineCopyUtils.CopyWithDifferentHandleDistance(this, desiredHandleDistance);

        private Cache initCache() => new(
            Segments.Select(s => new Lazy<SplineSegment<TPos, TDiff>>(() => s.ToSplineSegment(Ops))).ToImmutableArray()
        );

    }

}