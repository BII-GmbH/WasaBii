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
    public sealed class CatmullRomSpline<TPos, TDiff> : Spline<TPos, TDiff>.Copyable where TPos : unmanaged where TDiff : unmanaged {

        internal sealed record Cache(
            ImmutableArray<SplineSegment<TPos, TDiff>> SplineSegments,
            ImmutableArray<Length> SegmentOffsetsFromBegin
        );

        public CatmullRomSpline(
            TPos startHandle, IEnumerable<TPos> handles, TPos endHandle, 
            GeometricOperations<TPos, TDiff> ops,
            SplineType? splineType = null
        ) : this(handles.Prepend(startHandle).Append(endHandle), ops, splineType) {}

        public CatmullRomSpline(
            IEnumerable<TPos> allHandlesIncludingMarginHandles, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? splineType = null
        ) {
            // Note CR: Serialization might pass only `default` parameters, so we support this case here
            if (allHandlesIncludingMarginHandles != default) {
                handles = ImmutableArray.CreateRange(allHandlesIncludingMarginHandles);
                if (handles.Length < 4)
                    throw new ArgumentException(
                        $"Cannot construct a Catmull-Rom spline from {handles.Length} handles, at least 4 are needed"
                    );
            } else handles = ImmutableArray<TPos>.Empty;
            
            Type = splineType ?? SplineType.Centripetal;
            cache = new Lazy<Cache>(initCache);
            this.Ops = ops;
        }

        private readonly ImmutableArray<TPos> handles;

        public IReadOnlyList<TPos> HandlesIncludingMargin => handles;
        
        public IReadOnlyList<TPos> Handles => new ReadOnlyListSegment<TPos>(
            HandlesIncludingMargin, 
            offset: 1,
            count: HandlesIncludingMargin.Count - 2
        );
        public int SegmentCount => HandlesIncludingMargin.Count - 3;

        public SplineType Type { get; }
        
        public GeometricOperations<TPos, TDiff> Ops { get; }

        public IEnumerable<SplineSegment<TPos, TDiff>> Segments => cache.Value.SplineSegments;

        public TPos this[SplineHandleIndex index] => handles[index];

        public SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.NormalizeOrThrow(location)];

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] => cache.Value.SplineSegments[index];
        
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => 
            SplineSample<TPos, TDiff>.From(this, location).GetOrThrow(() => 
                new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    $"Must be between 0 and {SegmentCount}"
                ));

        public ImmutableArray<Length> SegmentOffsetsFromBegin => cache.Value.SegmentOffsetsFromBegin;

        [Pure] public Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged => 
            new CatmullRomSpline<TPosNew, TDiffNew>(HandlesIncludingMargin.Select(positionMapping), newOps, Type);

        [Pure] public Spline<TPos, TDiff> Reversed => new CatmullRomSpline<TPos, TDiff>(HandlesIncludingMargin.Reverse(), Ops, Type);
        
        [Pure] public Spline<TPos, TDiff> CopyWithOffset(Func<TDiff, TDiff> tangentToOffset) => 
            CatmullRomSplineCopyUtils.CopyWithOffset(this, tangentToOffset);

        [Pure] public Spline<TPos, TDiff> CopyWithStaticOffset(TDiff offset) =>
            CatmullRomSplineCopyUtils.CopyWithStaticOffset(this, offset);

        [Pure] public Spline<TPos, TDiff> CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            CatmullRomSplineCopyUtils.CopyWithDifferentHandleDistance(this, desiredHandleDistance);
        
#region Segment Length Caching

        [NonSerialized] 
        private readonly Lazy<Cache> cache;

        private Cache initCache() {
            var segmentCount = SegmentCount;
            var segments = ImmutableArray.CreateBuilder<SplineSegment<TPos, TDiff>>(initialCapacity: segmentCount);
            for(var i = 0; i< segmentCount; i++) segments.Add(new SplineSegment<TPos, TDiff>(
                CatmullRomPolynomial.FromSplineAt(this, SplineSegmentIndex.At(i))
                    .GetOrThrow(() =>
                        new Exception(
                            "Could not create a polynomial for this spline. " +
                            "This should not happen and indicates a bug in this method."
                        )
                    )));
            var segmentOffsets = ImmutableArray.CreateBuilder<Length>(initialCapacity: segmentCount);
            var lastOffset = Length.Zero;
            segmentOffsets.Add(Length.Zero);
            for (var i = 1; i < segmentCount; i++) 
                segmentOffsets.Add(lastOffset += segments[i - 1].Length);
            return new(segments.MoveToImmutable(), segmentOffsets.MoveToImmutable());
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

        public static TPos BeginMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : unmanaged where TDiff : unmanaged =>
            spline.HandlesIncludingMargin[0];

        public static TPos EndMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : unmanaged where TDiff : unmanaged =>
            spline.HandlesIncludingMargin[^1];
        
        public static TPos FirstHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : unmanaged where TDiff : unmanaged =>
            spline.Handles[0];

        public static TPos LastHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : unmanaged where TDiff : unmanaged =>
            spline.Handles[^1];

        /// Returns all the positions of spline handles that are between the given locations on the spline.
        /// The positions of the locations on the spline themselves are included at begin and end.
        /// This is a more performant way to sample a spline than the methods in
        /// <see cref="SplineSampleExtensions"/>.
        /// This is because sampling has to normalize the locations for every sample made,
        /// while this operation is only done twice here.
        [Pure]
        public static IEnumerable<TPos> HandlesBetween<TPos, TDiff>(
            this CatmullRomSpline<TPos, TDiff> spline, SplineLocation start, SplineLocation end
        ) where TPos : unmanaged where TDiff : unmanaged {
            var fromNormalized = spline.NormalizeOrThrow(start);
            var toNormalized = spline.NormalizeOrThrow(end);

            yield return spline[fromNormalized].Position;

            if (fromNormalized > toNormalized) {
                // Iterate from end of spline to begin
                for (var nodeIndex = spline.Handles.Count - 1; nodeIndex >= 0; --nodeIndex)
                    if (nodeIndex < fromNormalized && nodeIndex > toNormalized)
                        yield return spline.Handles[nodeIndex];
            } else {
                // Iterate from begin of spline to end
                for (var nodeIndex = 0; nodeIndex < spline.Handles.Count; ++nodeIndex)
                    if (nodeIndex > fromNormalized && nodeIndex < toNormalized)
                        yield return spline.Handles[nodeIndex];
            }

            yield return spline[toNormalized].Position;
        }

    }

}