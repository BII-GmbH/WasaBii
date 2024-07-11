using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.CatmullRom {
    
    /// <summary>
    /// Utilities for constructing generic catmull-rom splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff,TTime,TVel}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    /// </summary>
    public static partial class CatmullRomSpline {
        
        /// <summary>
        /// Creates a non-uniform catmull-rom spline from the provided positions
        /// and the respective times at which they should be traversed.
        /// The begin and end margin handles are not interpolated by
        /// the spline and merely affect its trajectory at the spline's
        /// start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// </summary>
        /// <returns><see cref="NotEnoughHandles"/> if less than 2 interpolated handle positions were provided</returns>
        [Pure]
        public static Result<CatmullRomSpline<TPos, TDiff, TTime, TVel>, NotEnoughHandles> FromHandles<TPos, TDiff, TTime, TVel>(
            TPos beginMarginHandle, 
            IEnumerable<(TPos, TTime)> interpolatedHandlesAndTimeSteps, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var handles = interpolatedHandlesAndTimeSteps.AsReadOnlyList();
            return handles.Count >= 2
                ? new CatmullRomSpline<TPos, TDiff, TTime, TVel>(beginMarginHandle, handles, endMarginHandle, ops, type)
                : new NotEnoughHandles(handles.Count + 2, 4);
        }

        /// <summary>
        /// Creates a uniform catmull-rom spline from the provided positions.
        /// The begin and end margin handles are not interpolated by
        /// the spline and merely affect its trajectory at the spline's
        /// start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// 
        /// </summary>
        /// <returns><see cref="NotEnoughHandles"/> if less than 2 interpolated handle positions were provided</returns>
        [Pure]
        public static Result<CatmullRomSpline<TPos, TDiff, double, TDiff>, NotEnoughHandles> UniformFromHandles<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged {
            var handles = interpolatedHandles.AsReadOnlyList();
            var durationPerSegment = 1.0 / (handles.Count - 1);
            return handles.Count >= 2
                ? new CatmullRomSpline<TPos, TDiff, double, TDiff>(
                    beginMarginHandle.PrependTo(handles).Append(endMarginHandle), 
                    Enumerable.Repeat(durationPerSegment, handles.Count - 1), 
                    ops, 
                    type)
                : new NotEnoughHandles(handles.Count + 2, 4);
        }

        /// <summary>
        /// Creates a non-uniform catmull-rom spline from the provided positions
        /// and the respective times at which they should be traversed.
        /// The begin and end margin handles are not interpolated by
        /// the spline and merely affect its trajectory at the spline's
        /// start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 interpolated handle positions were provided
        /// </exception>
        [Pure]
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> FromHandlesOrThrow<TPos, TDiff, TTime, TVel>(
            TPos beginMarginHandle, 
            IEnumerable<(TPos, TTime)> interpolatedHandlesAndTimeSteps, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => 
            new(beginMarginHandle, interpolatedHandlesAndTimeSteps, endMarginHandle, ops, type);

        /// <summary>
        /// Creates a uniform catmull-rom spline from the provided positions.
        /// The begin and end margin handles are not interpolated by
        /// the spline and merely affect its trajectory at the spline's
        /// start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// 
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 interpolated handle positions were provided
        /// </exception>
        [Pure]
        public static CatmullRomSpline<TPos, TDiff, double, TDiff> UniformFromHandlesOrThrow<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged {
            var handles = ImmutableArray.CreateBuilder<TPos>();
            handles.Add(beginMarginHandle);
            handles.AddRange(interpolatedHandles);
            handles.Add(endMarginHandle);
            if (handles.Count < 4)
                throw new InsufficientNodePositionsException(handles.Count - 2, 2);
            var segmentCount = handles.Count - 3;
            var times = ImmutableArray.CreateBuilder<double>(segmentCount + 1);
            times.AddRange(Enumerable.Range(0, segmentCount + 1).Select(i => i / (double) segmentCount));
            return new(handles.MoveToImmutable(), times.MoveToImmutable(), ops, type);
        }

        /// <summary>
        /// Creates a non-uniform catmull-rom spline that interpolates the provided positions at the provided times.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff,TTime,TVel}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 handle positions were provided
        /// </exception>
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> FromHandlesOrThrow<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos, TTime)> source, GeometricOperations<TPos, TDiff, TTime, TVel> ops, SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            FromHandles(source, ops, type).ResultOrThrow(notEnoughHandles => 
                new InsufficientNodePositionsException(notEnoughHandles.HandlesProvided, notEnoughHandles.HandlesNeeded));

        /// <summary>
        /// Creates a uniform catmull-rom spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff,TTime,TVel}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 handle positions were provided
        /// </exception>
        public static CatmullRomSpline<TPos, TDiff, double, TDiff> UniformFromHandlesOrThrow<TPos, TDiff>(
            IEnumerable<TPos> source, GeometricOperations<TPos, TDiff, double, TDiff> ops, SplineType type = SplineType.Centripetal, bool shouldLoop = false
        ) where TPos : unmanaged where TDiff : unmanaged =>
            UniformFromHandles(source, ops, type, shouldLoop).ResultOrThrow(notEnoughHandles =>
                new InsufficientNodePositionsException(notEnoughHandles.HandlesProvided, notEnoughHandles.HandlesNeeded));

        /// <summary>
        /// Creates a non-uniform catmull-rom spline that interpolates the provided positions at the provided times.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff,TTime,TVel}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// 
        /// Returns <see cref="NotEnoughHandles"/> if too few positions are provided.
        /// </summary>
        public static Result<CatmullRomSpline<TPos, TDiff, TTime, TVel>, NotEnoughHandles> FromHandles<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos, TTime)> source, GeometricOperations<TPos, TDiff, TTime, TVel> ops, SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var (positions, times) = source.Unzip();
            if (positions.Count < 2)
                return new NotEnoughHandles(positions.Count, 2);

            var (beginHandle, endHandle) = positions.calculateSplineMarginHandles(ops, shouldLoop: false);
            var allHandles = ImmutableArray.CreateBuilder<TPos>(positions.Count + 2);
            allHandles.Add(beginHandle);
            allHandles.AddRange(positions);
            allHandles.Add(endHandle);
            return new CatmullRomSpline<TPos, TDiff, TTime, TVel>(allHandles.MoveToImmutable(), times, ops, type);
        }

        /// <summary>
        /// Creates a uniform catmull-rom spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff,TTime,TVel}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// 
        /// Returns <see cref="NotEnoughHandles"/> if too few positions are provided.
        /// </summary>
        public static Result<CatmullRomSpline<TPos, TDiff, double, TDiff>, NotEnoughHandles> UniformFromHandles<TPos, TDiff>(
            IEnumerable<TPos> source, GeometricOperations<TPos, TDiff, double, TDiff> ops, SplineType type = SplineType.Centripetal, bool shouldLoop = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 2)
                return new NotEnoughHandles(positions.Count, 2);

            var (beginHandle, endHandle) = positions.calculateSplineMarginHandles(ops, shouldLoop);
            var allHandles = ImmutableArray.CreateBuilder<TPos>(positions.Count + (shouldLoop ? 3 : 2));
            allHandles.Add(beginHandle);
            allHandles.AddRange(positions);
            if(shouldLoop) allHandles.Add(positions.First());
            allHandles.Add(endHandle);
            var segmentCount = allHandles.Count - 3;
            var times = ImmutableArray.CreateBuilder<double>(segmentCount + 1);
            times.AddRange(Enumerable.Range(0, segmentCount + 1).Select(i => i / (double) segmentCount));
            return new CatmullRomSpline<TPos, TDiff, double, TDiff>(allHandles.MoveToImmutable(), times.MoveToImmutable(), ops, type);
        }

        /// <summary>
        /// Creates a non-uniform catmull-rom spline that interpolates the provided positions at the provided times.
        /// The first and last position of <paramref name="allHandlesIncludingMargin"/> become the begin and end handles,
        /// which means that they are not interpolated by the spline and merely affect its
        /// trajectory at the spline's start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// </summary>
        /// <returns><see cref="NotEnoughHandles"/> if <see cref="allHandlesIncludingMargin"/> has less than 4 entries.</returns>
        /// <exception cref="ArgumentException"> if the amount f segment start times does not match the segment count</exception>
        [Pure]
        public static Result<CatmullRomSpline<TPos, TDiff, TTime, TVel>, NotEnoughHandles> FromHandlesIncludingMargin<TPos, TDiff, TTime, TVel>(
            IEnumerable<TPos> allHandlesIncludingMargin,
            IEnumerable<TTime> segmentStartTimes,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var positions = allHandlesIncludingMargin.AsReadOnlyCollection();
            var times = segmentStartTimes.AsReadOnlyCollection();
            if (times.Count != positions.Count - 3)
                throw new ArgumentException("Amount of segment start times does not match segment count");
            return Result.If(
                positions.Count >= 4,
                () => new CatmullRomSpline<TPos, TDiff, TTime, TVel>(positions, times, ops, type),
                () => new NotEnoughHandles(positions.Count, 4)
            );
        }
        
        /// <summary>
        /// Tries to create a uniform catmull-rom spline from the provided positions.
        /// The first and last position of <paramref name="allHandlesIncludingMargin"/> become the begin and end handles,
        /// which means that they are not interpolated by the spline and merely affect its
        /// trajectory at the spline's start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// </summary>
        /// <returns><see cref="NotEnoughHandles"/> if <see cref="allHandlesIncludingMargin"/> has less than 4 entries.</returns>
        [Pure]
        public static Result<CatmullRomSpline<TPos, TDiff, double, TDiff>, NotEnoughHandles> UniformFromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged {
            var positions = allHandlesIncludingMargin.AsReadOnlyCollection();
            if (positions.Count < 4) return new NotEnoughHandles(positions.Count, 4);
            var segmentCount = positions.Count - 3;
            var times = ImmutableArray.CreateBuilder<double>(segmentCount + 1);
            times.AddRange(Enumerable.Range(0, segmentCount + 1).Select(i => i / (double) segmentCount));
            return new CatmullRomSpline<TPos, TDiff, double, TDiff>(positions, times.MoveToImmutable(), ops, type);
        }
        
        /// <summary>
        /// Creates a non-uniform catmull-rom spline from the provided positions at the provided times.
        /// The first and last position of <paramref name="allHandlesIncludingMargin"/> become the begin and end handles,
        /// which means that they are not interpolated by the spline and merely affect its
        /// trajectory at the spline's start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// 
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 4 handle positions were provided
        /// </exception>
        /// <exception cref="ArgumentException"> if the amount f segment start times does not match the segment count</exception>
        [Pure]
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> FromHandlesIncludingMarginOrThrow<TPos, TDiff, TTime, TVel>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            IEnumerable<TTime> segmentStartTimes,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => 
            FromHandlesIncludingMargin(allHandlesIncludingMargin, segmentStartTimes, ops, type)
                .ResultOrThrow(error => new InsufficientNodePositionsException(error.HandlesProvided, 4));

        /// <summary>
        /// Creates a uniform catmull-rom spline from the provided positions.
        /// The first and last position of <paramref name="allHandlesIncludingMargin"/> become the begin and end handles,
        /// which means that they are not interpolated by the spline and merely affect its
        /// trajectory at the spline's start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// 
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 4 handle positions were provided
        /// </exception>
        [Pure]
        public static CatmullRomSpline<TPos, TDiff, double, TDiff> UniformFromHandlesIncludingMarginOrThrow<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            SplineType type = SplineType.Centripetal
        ) where TPos : unmanaged where TDiff : unmanaged => 
            UniformFromHandlesIncludingMargin(allHandlesIncludingMargin, ops, type)
                .ResultOrThrow(error => new InsufficientNodePositionsException(error.HandlesProvided, 4));

        /// <summary>
        /// Calculates margin handles for a catmull-rom spline that interpolates the given handle positions.
        /// These margin handles are calculated on the easiest way possible by
        /// mirroring the position of the second / second from last handle position
        /// relative to the first / last handle position, respectively.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 handle positions were provided
        /// </exception>
        private static (TPos BeginHandle, TPos EndHandle) calculateSplineMarginHandles<TPos, TDiff, TTime, TVel>(
            this IEnumerable<TPos> handlePositions, GeometricOperations<TPos, TDiff, TTime, TVel> ops, bool shouldLoop
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged {
            var positions = handlePositions.AsReadOnlyList();
            if (positions.Count < 2)
                throw new InsufficientNodePositionsException(positions.Count, 2);

            var beginHandle = shouldLoop ? positions[^2] : positions[1].pointReflect(positions[0], ops);
            var endHandle = shouldLoop ? positions[1] : positions[^2].pointReflect(positions[^1], ops);
            return (beginHandle, endHandle);
        }

        private static TPos pointReflect<TPos, TDiff, TTime, TVel>(this TPos self, TPos on, GeometricOperations<TPos, TDiff, TTime, TVel> ops)
            where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged where TVel : unmanaged =>
            ops.Add(on, ops.Sub(on, self));
    }

}
