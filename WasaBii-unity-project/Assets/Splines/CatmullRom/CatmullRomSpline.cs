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
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class CatmullRomSpline<TPos, TDiff> : Spline<TPos, TDiff> where TPos : struct where TDiff : struct {
        
        public CatmullRomSpline(
            TPos startHandle, IEnumerable<TPos> handles, TPos endHandle, 
            GeometricOperations<TPos, TDiff> ops,
            SplineType? splineType = null
        ) : this(handles.Prepend(startHandle).Append(endHandle), ops, splineType)
            => cachedSegmentLengths = new Lazy<IReadOnlyList<Length>>(() => calculateSegmentLengths(this));

        public CatmullRomSpline(IEnumerable<TPos> allHandlesIncludingMarginHandles, GeometricOperations<TPos, TDiff> ops, SplineType? splineType = null) {
            handles = ImmutableArray.CreateRange(allHandlesIncludingMarginHandles);
            Type = splineType ?? SplineType.Centripetal;
            cachedSegmentLengths = new Lazy<IReadOnlyList<Length>>(() => calculateSegmentLengths(this));
            this.Ops = ops;
        }

        // The non-nullable fields are not set and thus null, but
        // they should always be set via reflection, so this is fine.
    #pragma warning disable 8618
        [JsonConstructor] private CatmullRomSpline(){}
    #pragma warning restore 8618

        private readonly ImmutableArray<TPos> handles;

        public IReadOnlyList<TPos> HandlesIncludingMargin => handles;
        public int HandleCountIncludingMargin => HandlesIncludingMargin.Count;
        
        public IReadOnlyList<TPos> Handles => new ReadOnlyListSegment<TPos>(
            HandlesIncludingMargin, 
            offset: 1,
            count: HandleCount
        );
        public int HandleCount => HandleCountIncludingMargin - 2;

        public SplineType Type { get; }
        
        public Spline<TPos, TDiff> Spline => this;

        public GeometricOperations<TPos, TDiff> Ops { get; }

        public TPos this[SplineHandleIndex index] => handles[index];

        public SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] => 
            SplineSegment.From(this, index, cachedSegmentLengths.Value[index])
                .GetOrThrow(() => new ArgumentOutOfRangeException(nameof(index), index, $"Must be between 0 and {this.SegmentCount()}"));
        
        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => SplineSample<TPos, TDiff>.From(this, location) ??
            throw new ArgumentOutOfRangeException(
                nameof(location),
                location,
                $"Must be between 0 and {HandleCount - 1}"
            );

        public bool Equals(Spline<TPos, TDiff> other) => 
            other is CatmullRomSpline<TPos, TDiff> otherSpline 
                && this.HandlesIncludingMargin.SequenceEqual(otherSpline.HandlesIncludingMargin) 
                && Type == otherSpline.Type;

        public override bool Equals(object obj) => obj is CatmullRomSpline<TPos, TDiff> otherSpline && Equals(otherSpline);
        
        public override int GetHashCode() => HashCode.Combine(handles, (int)Type);
        
#region Segment Length Caching
        // The cached lengths for each segment,
        // accessed by the segment index.
        [NonSerialized] 
        private readonly Lazy<IReadOnlyList<Length>> cachedSegmentLengths;

        private static IReadOnlyList<Length> calculateSegmentLengths(CatmullRomSpline<TPos, TDiff> spline) {
            var ret = new Length[spline.SegmentCount()];
            for (var i = 0; i < spline.SegmentCount(); i++) {
                var idx = SplineSegmentIndex.At(i);
                ret[idx] = CatmullRomPolynomial.FromSplineAt(spline, idx)
                    .GetOrThrow(() =>
                        new Exception(
                            "Could not create a cubic polynomial for this spline. " +
                            "This should not happen and indicates a bug in this method."
                        )
                    ).ArcLength;
            }
            return ret;
        }
        
        #endregion

    }
    
    /// Contains generic factory methods for building splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    public static class CatmullRomSpline {
        
        /// Creates a spline that interpolates the given handles.
        [Pure]
        public static CatmullRomSpline<TPos, TDiff> FromInterpolating<TPos, TDiff>(
            IEnumerable<TPos> handles, GeometricOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var interpolatedHandles = handles.AsReadOnlyList();
            var (beginMarginHandle, endMarginHandle) = interpolatedHandles.calculateSplineMarginHandles(ops);
            return FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, ops, type);
        }

        [Pure]
        public static CatmullRomSpline<TPos, TDiff> FromHandles<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct => 
            FromHandlesIncludingMargin(
                interpolatedHandles.Prepend(beginMarginHandle).Append(endMarginHandle), 
                ops,
                type
            );

        /// Creates a spline with the given handles, which include the begin and end margin handles.
        [Pure]
        public static CatmullRomSpline<TPos, TDiff> FromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct 
            => new CatmullRomSpline<TPos, TDiff>(allHandlesIncludingMargin, ops, type);
        
        public static TPos BeginMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.HandlesIncludingMargin[0]);

        public static TPos EndMarginHandle<TPos, TDiff>(this CatmullRomSpline<TPos, TDiff> spline)
        where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.HandlesIncludingMargin[^1]);

    }
}