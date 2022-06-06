using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Logic;
using BII.WasaBii.UnitSystem;

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
        SplineSample<TPos, TDiff> this[SplineLocation location] { get; }
        SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] { get; }
        
        internal PositionOperations<TPos, TDiff> Ops { get; }

        Length Length(int samplesPerSegment = 10) => 
            this.WhenValidOrThrow(
                _ => Enumerable.Range(0, this.SegmentCount())
                    .Sum(idx => this[SplineSegmentIndex.At(idx)].Length(samplesPerSegment))
            );
    }

    public interface WithSpline<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        Spline<TPos, TDiff> Spline { get; }
    }

    /// Contains generic factory methods for building splines.
    /// For explicitly typed variants with <see cref="PositionOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    public static class GenericSpline {
        
        /// Creates a spline builder for a spline that interpolates the given handles.
        /// The begin and end margin handles will be generated automatically
        /// using <see cref="EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        [Pure]
        public static Spline<TPos, TDiff> FromInterpolating<TPos, TDiff>(
            IEnumerable<TPos> handles, PositionOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var interpolatedHandles = handles.AsReadOnlyList();
            var (beginMarginHandle, endMarginHandle) = interpolatedHandles.CalculateSplineMarginHandles(ops);
            return FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, ops, type);
        }

        [Pure]
        public static Spline<TPos, TDiff> FromHandles<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            PositionOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct => 
            FromHandlesIncludingMargin(
                interpolatedHandles.Prepend(beginMarginHandle).Append(endMarginHandle), 
                ops,
                type
            );

        /// Creates a spline builder for a spline with the given handles.
        /// These include the begin and end margin handles.
        [Pure]
        public static Spline<TPos, TDiff> FromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            PositionOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct 
            => new ImmutableSpline<TPos, TDiff>(allHandlesIncludingMargin, ops, type);
    }
}