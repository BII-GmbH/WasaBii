using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Units;

namespace BII.WasaBii.Splines {
    
    /// The base interface for catmull-rom splines.
    [MustBeSerializable][MustBeImmutable]
    public interface Spline<TPos, TDiff> : IEquatable<Spline<TPos, TDiff>>, WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        IReadOnlyList<TPos> HandlesIncludingMargin { get; }
        IReadOnlyList<TPos> Handles => new ReadOnlyListSegment<TPos>(
            HandlesIncludingMargin, 
            offset: 1,
            count: HandleCount
        );

        int HandleCountIncludingMargin => HandlesIncludingMargin.Count;
        int HandleCount => HandleCountIncludingMargin - 2;
        
        /// The type of the spline.
        /// It determines how closely the handles are interpolated. 
        SplineType Type { get; }

        /// Retrieves the handle at the given index.
        TPos this[SplineHandleIndex index] => HandlesIncludingMargin[index];

        SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] { get; }
        SplineSample<TPos, TDiff> this[SplineLocation location] => this[this.Normalize(location)];
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        internal GeometricOperations<TPos, TDiff> Ops { get; }

        Length Length() => 
            this.WhenValidOrThrow(
                _ => Enumerable.Range(0, this.SegmentCount())
                    .Sum(idx => this[SplineSegmentIndex.At(idx)].Length)
            );
    }

    public interface WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        Spline<TPos, TDiff> Spline { get; }
    }

    /// Contains generic factory methods for building splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    public static class GenericSpline {
        
        /// Creates a spline that interpolates the given handles.
        [Pure]
        public static Spline<TPos, TDiff> FromInterpolating<TPos, TDiff>(
            IEnumerable<TPos> handles, GeometricOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var interpolatedHandles = handles.AsReadOnlyList();
            var (beginMarginHandle, endMarginHandle) = interpolatedHandles.calculateSplineMarginHandles(ops);
            return FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, ops, type);
        }

        [Pure]
        public static Spline<TPos, TDiff> FromHandles<TPos, TDiff>(
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
        public static Spline<TPos, TDiff> FromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct 
            => new ImmutableSpline<TPos, TDiff>(allHandlesIncludingMargin, ops, type);
    }
}