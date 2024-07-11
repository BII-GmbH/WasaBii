using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.CatmullRom {
    
    /// <summary>
    /// A spline that is defined by a number of points ("handles") and one "margin handle" at each end.
    /// The spline visits all points in order. The spline's trajectory between two points is influenced
    /// by the two neighboring handles. For the first and last points, the margin handles are used instead.
    /// A loop can be formed by starting and ending with the same point, using the second point as end margin
    /// handle and using the second from last point as start margin handle.
    ///
    /// Because you can construct a catmull-rom spline by only defining the points, it is very easy to set up.
    /// The derivative (tangent / velocity) is always continuous, which is very handy since discontinuous
    /// derivatives produce sudden kinks in the curve. Catmull-rom splines never produce loops within a single
    /// segment, i.e. between two succinct points.
    /// </summary>
    [Serializable]
    public sealed class CatmullRomSpline<TPos, TDiff, TTime, TVel> : Spline<TPos, TDiff, TTime, TVel>.Copyable 
    where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged
    {

        internal sealed record Cache(
            ImmutableArray<SplineSegment<TPos, TDiff, TTime, TVel>> SplineSegments,
            Length Length,
            ImmutableArray<Length> SpatialSegmentOffsets
        );

        public CatmullRomSpline(
            TPos startMarginHandle,
            IEnumerable<(TPos, TTime)> handles,
            TPos endMarginHandle,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops,
            SplineType splineType = SplineType.Centripetal
        ) {
            
            // Note CR: Serialization might pass only `default` parameters, so we support this case here
            if (handles != default) {
                var (handlePositions, handleTimes) = handles.Unzip();
                if (handlePositions.Count < 2)
                    throw new InsufficientNodePositionsException(actual: handlePositions.Count, required: 2);
                var handlesBuilder = ImmutableArray.CreateBuilder<TPos>(handlePositions.Count + 2);
                handlesBuilder.Add(startMarginHandle);
                handlesBuilder.AddRange(handlePositions);
                handlesBuilder.Add(endMarginHandle);
                this.handles = handlesBuilder.MoveToImmutable();
                this.TemporalSegmentOffsets = ImmutableArray.CreateRange(handleTimes);
            }
            Type = splineType;
            cache = new Lazy<Cache>(initCache);
            this.Ops = ops;
        }

        public CatmullRomSpline(
            IEnumerable<TPos> allHandlesIncludingMarginHandles,
            IEnumerable<TTime> handleTimes,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            SplineType splineType = SplineType.Centripetal
        ) {
            // Note CR: Serialization might pass only `default` parameters, so we support this case here
            if (allHandlesIncludingMarginHandles != default && handleTimes != null) {
                handles = allHandlesIncludingMarginHandles.ToImmutableArray();
                TemporalSegmentOffsets = handleTimes.ToImmutableArray();
                if (handles.Length < 4)
                    throw new InsufficientNodePositionsException(actual: handles.Length, required: 4);
                if (TemporalSegmentOffsets.Length + 2 != handles.Length)
                    throw new ArgumentException("Number of timestamps does not match number of non-margin handles");
            } else handles = ImmutableArray<TPos>.Empty;
            
            Type = splineType;
            cache = new Lazy<Cache>(initCache);
            this.Ops = ops;
        }

        private readonly ImmutableArray<TPos> handles;
        public ImmutableArray<TTime> TemporalSegmentOffsets { get; }

        public IReadOnlyList<TPos> HandlesIncludingMargin => handles;
        
        public IReadOnlyList<TPos> Handles => new ReadOnlyListSegment<TPos>(
            HandlesIncludingMargin, 
            offset: 1,
            count: HandlesIncludingMargin.Count - 2
        );
        public int SegmentCount => HandlesIncludingMargin.Count - 3;

        public Length Length => cache.Value.Length;
        public TTime TotalDuration => TemporalSegmentOffsets[^1];

        public SplineType Type { get; }
        
        public GeometricOperations<TPos, TDiff, TTime, TVel> Ops { get; }

        public IEnumerable<SplineSegment<TPos, TDiff, TTime, TVel>> Segments => cache.Value.SplineSegments;

        public TPos this[SplineHandleIndex index] => handles[index];

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

        [Pure] public Spline<TPosNew, TDiffNew, TTime, TVelNew> Map<TPosNew, TDiffNew, TVelNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew, TTime, TVelNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged where TVelNew : unmanaged => 
            new CatmullRomSpline<TPosNew, TDiffNew, TTime, TVelNew>(HandlesIncludingMargin.Select(positionMapping), TemporalSegmentOffsets, newOps, Type);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> Reversed => new CatmullRomSpline<TPos, TDiff, TTime, TVel>(HandlesIncludingMargin.Reverse(), TemporalSegmentOffsets, Ops, Type);
        
        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithOffset(Func<TVel, TDiff> tangentToOffset) => 
            CatmullRomSplineCopyUtils.CopyWithOffset(this, tangentToOffset);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithStaticOffset(TDiff offset) =>
            CatmullRomSplineCopyUtils.CopyWithStaticOffset(this, offset);

        [Pure] public Spline<TPos, TDiff, TTime, TVel> CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            CatmullRomSplineCopyUtils.CopyWithDifferentHandleDistance(this, desiredHandleDistance);
        
#region Segment Length Caching

        [NonSerialized] 
        private readonly Lazy<Cache> cache;

        private Cache initCache() {
            var segmentCount = SegmentCount;
            var segments = ImmutableArray.CreateBuilder<SplineSegment<TPos, TDiff, TTime, TVel>>(initialCapacity: segmentCount);
            for(var i = 0; i< segmentCount; i++) segments.Add(new SplineSegment<TPos, TDiff, TTime, TVel>(
                CatmullRomPolynomial.FromSplineAt(this, SplineSegmentIndex.At(i))
                    .GetOrThrow(() =>
                        new Exception(
                            "Could not create a polynomial for this spline. " +
                            "This should not happen and indicates a bug in this method."
                        )
                    )));
            
            var spatialOffsets = ImmutableArray.CreateBuilder<Length>(initialCapacity: segmentCount);
            
            var lastOffset = Length.Zero;
            foreach (var segment in segments) {
                if (segment.Duration.CompareTo(Ops.ZeroTime) == -1)
                    throw new Exception(
                        $"Tried to construct a spline with a segment of negative duration: {segment.Duration}"
                    );
                spatialOffsets.Add(lastOffset);
                lastOffset += segment.Length;
            }
            return new(segments.MoveToImmutable(), lastOffset, spatialOffsets.MoveToImmutable());
        }
        
        #endregion

    }

    public static partial class CatmullRomSpline {

        /// <summary>
        /// Designed to be used as a <see cref="Result"/> error type in situations
        /// where an operation can fail due to too few catmull-rom handles.
        /// Catmull-Rom splines always need at least 4 handles: two margin handles and two positions to traverse.
        /// Some utilities require you to pass the margin handles, in which case you have to pass at least 4 handles in total.
        /// Other utilities calculate them, such that only two handles need to be defined.
        /// <see cref="HandlesNeeded"/> defines how many handles were required by this specific utility
        /// (4 if margin handles are required, 2 if not).
        /// </summary>
        public readonly struct NotEnoughHandles {
            public readonly int HandlesProvided;
            public readonly int HandlesNeeded;
            public NotEnoughHandles(int handlesProvided, int handlesNeeded) {
                HandlesProvided = handlesProvided;
                HandlesNeeded = handlesNeeded;
            }
        }

        public static TPos BeginMarginHandle<TPos, TDiff, TTime, TVel>(this CatmullRomSpline<TPos, TDiff, TTime, TVel> spline)
        where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            spline.HandlesIncludingMargin[0];

        public static TPos EndMarginHandle<TPos, TDiff, TTime, TVel>(this CatmullRomSpline<TPos, TDiff, TTime, TVel> spline)
        where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            spline.HandlesIncludingMargin[^1];
        
        public static TPos FirstHandle<TPos, TDiff, TTime, TVel>(this CatmullRomSpline<TPos, TDiff, TTime, TVel> spline)
        where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            spline.Handles[0];

        public static TPos LastHandle<TPos, TDiff, TTime, TVel>(this CatmullRomSpline<TPos, TDiff, TTime, TVel> spline)
        where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            spline.Handles[^1];

        /// <summary>
        /// Returns all the positions of spline handles that are between the given locations on the spline.
        /// The positions of the locations on the spline themselves are included at begin and end.
        /// This is a more performant way to sample a spline than the methods in
        /// <see cref="SplineSampleExtensions"/>.
        /// This is because sampling has to normalize the locations for every sample made,
        /// while this operation is only done twice here.
        /// </summary>
        [Pure]
        public static IEnumerable<TPos> HandlesBetween<TPos, TDiff, TTime, TVel>(
            this CatmullRomSpline<TPos, TDiff, TTime, TVel> spline, SplineLocation start, SplineLocation end
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var fromNormalized = spline.NormalizeOrThrow(start);
            var toNormalized = spline.NormalizeOrThrow(end);

            if (fromNormalized > toNormalized) {
                var offset = MathD.CeilToInt(toNormalized.Value);
                var count = MathD.FloorToInt(fromNormalized.Value) - offset;
                return new ReadOnlyListSegment<TPos>(
                    spline.Handles,
                    offset,
                    count
                ).ReverseList()
                    .Prepend(spline[fromNormalized].Position).Append(spline[toNormalized].Position);
            } else {
                var offset = MathD.CeilToInt(fromNormalized.Value);
                var count = MathD.FloorToInt(toNormalized.Value) - offset;
                return new ReadOnlyListSegment<TPos>(
                    spline.Handles,
                    offset,
                    count
                ).Prepend(spline[fromNormalized].Position).Append(spline[toNormalized].Position);
            }
        }

    }

}