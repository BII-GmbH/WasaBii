using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.CatmullRom {
    
    /// Utilities for constructing generic catmull-rom splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    public static partial class CatmullRomSpline {
        
        /// <summary>
        /// Creates a catmull-rom spline from the provided positions.
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
        public static Result<CatmullRomSpline<TPos, TDiff>, NotEnoughHandles> FromHandles<TPos, TDiff>(
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

        /// <summary>
        /// Creates a catmull-rom spline from the provided positions.
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
        public static CatmullRomSpline<TPos, TDiff> FromHandlesOrThrow<TPos, TDiff>(
            TPos beginMarginHandle, 
            IEnumerable<TPos> interpolatedHandles, 
            TPos endMarginHandle, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct => 
            FromHandlesIncludingMarginOrThrow(
                interpolatedHandles.Prepend(beginMarginHandle).Append(endMarginHandle), 
                ops,
                type
            );

        /// <summary>
        /// Creates a catmull-rom spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 handle positions were provided
        /// </exception>
        public static CatmullRomSpline<TPos, TDiff> FromHandlesOrThrow<TPos, TDiff>(
            IEnumerable<TPos> source, GeometricOperations<TPos, TDiff> ops, SplineType? splineType = null, bool shouldLoop = false
        ) where TPos : struct where TDiff : struct {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 2)
                throw new InsufficientNodePositionsException(positions.Count, 2);

            var handles = shouldLoop ? positions.Append(positions.First()) : positions;
            var (beginHandle, endHandle) = handles.calculateSplineMarginHandles(ops, shouldLoop);
            return new CatmullRomSpline<TPos, TDiff>(beginHandle, handles, endHandle, ops, splineType);
        }

        /// Creates a catmull-rom spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// 
        /// Returns <see cref="NotEnoughHandles"/> if too few positions are provided
        public static Result<CatmullRomSpline<TPos, TDiff>, NotEnoughHandles> FromHandles<TPos, TDiff>(
            IEnumerable<TPos> source, GeometricOperations<TPos, TDiff> ops, SplineType? type = null, bool shouldLoop = false
        ) where TPos : struct where TDiff : struct {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 2) return new NotEnoughHandles(positions.Count, 2);
            var handles = shouldLoop ? positions.Append(positions.First()) : positions;
            var (beginHandle, endHandle) = handles.calculateSplineMarginHandles(ops, shouldLoop);
            return new CatmullRomSpline<TPos, TDiff>(beginHandle, handles, endHandle, ops, type);
        }

        /// <summary>
        /// Tries to create a catmull-rom spline from the provided positions.
        /// The first and last position of <paramref name="allHandlesIncludingMargin"/> become the begin and end handles,
        /// which means that they are not interpolated by the spline and merely affect its
        /// trajectory at the spline's start and end.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// needs to be clearly defined.
        /// </summary>
        /// <returns><see cref="NotEnoughHandles"/> if <see cref="allHandlesIncludingMargin"/> has less than
        /// 4 entries.</returns>
        [Pure]
        public static Result<CatmullRomSpline<TPos, TDiff>, NotEnoughHandles> FromHandlesIncludingMargin<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var positions = allHandlesIncludingMargin.AsReadOnlyCollection();
            return Result.If(
                positions.Count >= 4,
                () => new CatmullRomSpline<TPos, TDiff>(positions, ops, type),
                () => new NotEnoughHandles(positions.Count, 4)
            );
        }
        
        /// <summary>
        /// Creates a catmull-rom spline from the provided positions.
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
        public static CatmullRomSpline<TPos, TDiff> FromHandlesIncludingMarginOrThrow<TPos, TDiff>(
            IEnumerable<TPos> allHandlesIncludingMargin, 
            GeometricOperations<TPos, TDiff> ops, 
            SplineType? type = null
        ) where TPos : struct where TDiff : struct 
            => FromHandlesIncludingMargin(allHandlesIncludingMargin, ops, type)
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
        private static (TPos BeginHandle, TPos EndHandle) calculateSplineMarginHandles<TPos, TDiff>(
            this IEnumerable<TPos> handlePositions, GeometricOperations<TPos, TDiff> ops, bool shouldLoop
        ) where TPos : struct where TDiff : struct {
            var positions = handlePositions.AsReadOnlyList();
            if (positions.Count < 2)
                throw new InsufficientNodePositionsException(positions.Count, 2);

            var beginHandle = shouldLoop ? positions[^2] : positions[1].pointReflect(positions[0], ops);
            var endHandle = shouldLoop ? positions[1] : positions[^2].pointReflect(positions[^1], ops);
            return (beginHandle, endHandle);
        }

        private static TPos pointReflect<TPos, TDiff>(this TPos self, TPos on, GeometricOperations<TPos, TDiff> ops)
            where TPos : struct where TDiff : struct 
            => ops.Add(on, ops.Sub(on, self));
    }

}
