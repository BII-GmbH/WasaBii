using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using Newtonsoft.Json;

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
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable]
    public sealed class CatmullRomSpline<TPos, TDiff> : Spline<TPos, TDiff> where TPos : struct where TDiff : struct {

        public CatmullRomSpline(
            TPos startHandle, IEnumerable<TPos> handles, TPos endHandle, 
            GeometricOperations<TPos, TDiff> ops,
            SplineType? splineType = null
        ) : this(handles.Prepend(startHandle).Append(endHandle), ops, splineType) {}

        public CatmullRomSpline(IEnumerable<TPos> allHandlesIncludingMarginHandles, GeometricOperations<TPos, TDiff> ops, SplineType? splineType = null) {
            handles = ImmutableArray.CreateRange(allHandlesIncludingMarginHandles);
            if (handles.Length < 4)
                throw new ArgumentException(
                    $"Cannot construct a Catmull-Rom spline from {handles.Length} handles, at least 4 are needed"
                );
            Type = splineType ?? SplineType.Centripetal;
            cachedSegmentLengths = new Lazy<ImmutableArray<Lazy<Length>>>(() => prepareSegmentLengthCache(this));
            this.Ops = ops;
        }

        // The non-nullable fields are not set and thus null, but
        // they should always be set via reflection, so this is fine.
    #pragma warning disable 8618
        [JsonConstructor] private CatmullRomSpline() => cachedSegmentLengths = new Lazy<ImmutableArray<Lazy<Length>>>(() => prepareSegmentLengthCache(this));
    #pragma warning restore 8618

        private readonly ImmutableArray<TPos> handles;

        public IReadOnlyList<TPos> HandlesIncludingMargin => handles;
        
        public IReadOnlyList<TPos> Handles => new ReadOnlyListSegment<TPos>(
            HandlesIncludingMargin, 
            offset: 1,
            count: HandlesIncludingMargin.Count - 2
        );
        public int SegmentCount => Handles.Count - 1;

        public SplineType Type { get; }
        
        public GeometricOperations<TPos, TDiff> Ops { get; }

        public IEnumerable<SplineSegment<TPos, TDiff>> Segments 
            => Enumerable.Range(0, SegmentCount).Select(i => this[new SplineSegmentIndex(i)]);

        public TPos this[SplineHandleIndex index] => handles[index];

        public SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] => 
            CatmullRomPolynomial.FromSplineAt(this, index)
                .Map(val => new SplineSegment<TPos, TDiff>(val, cachedSegmentLengths.Value[index]))
                .GetOrThrow(() => new ArgumentOutOfRangeException(nameof(index), index, $"Must be between 0 and {SegmentCount}"));
        
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => 
            SplineSample<TPos, TDiff>.From(this, location).GetOrThrow(() => 
                new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    $"Must be between 0 and {SegmentCount}"
                ));

        public Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : struct where TDiffNew : struct => 
            new CatmullRomSpline<TPosNew, TDiffNew>(HandlesIncludingMargin.Select(positionMapping), newOps, Type);

        public bool Equals(Spline<TPos, TDiff> other) => 
            other is CatmullRomSpline<TPos, TDiff> otherSpline 
                && this.HandlesIncludingMargin.SequenceEqual(otherSpline.HandlesIncludingMargin) 
                && Type == otherSpline.Type
                && Equals(Ops, other.Ops);

        public override bool Equals(object obj) => obj is CatmullRomSpline<TPos, TDiff> otherSpline && Equals(otherSpline);
        
        public override int GetHashCode() => HashCode.Combine(handles, (int)Type, Ops);
        
#region Segment Length Caching
        // The cached lengths for each segment,
        // accessed by the segment index.
        [NonSerialized] 
        private readonly Lazy<ImmutableArray<Lazy<Length>>> cachedSegmentLengths;

        private static ImmutableArray<Lazy<Length>> prepareSegmentLengthCache(CatmullRomSpline<TPos, TDiff> spline) {
            var ret = new Lazy<Length>[spline.SegmentCount];
            for (var i = 0; i < spline.SegmentCount; i++) {
                var idx = SplineSegmentIndex.At(i);
                ret[idx] = new Lazy<Length>(() => CatmullRomPolynomial.FromSplineAt(spline, idx)
                    .GetOrThrow(() =>
                        new Exception(
                            "Could not create a polynomial for this spline. " +
                            "This should not happen and indicates a bug in this method."
                        )
                    ).ArcLength);
            }
            return ImmutableArray.Create(ret);
        }
        
        #endregion

    }

    public static partial class CatmullRomSpline {
        
        public static TPos BeginMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
            spline.HandlesIncludingMargin[0];

        public static TPos EndMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
            spline.HandlesIncludingMargin[^1];
        
        public static TPos FirstHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
            spline.Handles[0];

        public static TPos LastHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
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
        ) where TPos : struct where TDiff : struct {
            var fromNormalized = spline.Normalize(start);
            var toNormalized = spline.Normalize(end);

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