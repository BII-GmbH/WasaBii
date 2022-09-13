using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.CatmullRom {
    
    /// Utilities for constructing generic splines.
    /// For explicitly typed versions, see `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly
    public static class GenericEnumerableToCatmullRomSplineExtensions {

        /// <summary>
        /// Calculates margin handles for a spline that interpolates the given handle positions.
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
        internal static (TPos BeginHandle, TPos EndHandle) calculateSplineMarginHandles<TPos, TDiff>(
            this IEnumerable<TPos> handlePositions, GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct {
            var positions = handlePositions.AsReadOnlyList();
            if (positions.Count < 2)
                throw new InsufficientNodePositionsException(positions.Count, 2);

            var beginHandle = positions[1].pointReflect(positions[0], ops);
            var endHandle = positions[^2].pointReflect(positions[^1], ops);
            return (beginHandle, endHandle);
        }

        /// <summary>
        /// Creates a spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// </summary>
        /// <exception cref="InsufficientNodePositionsException">
        /// When less than 2 handle positions were provided
        /// </exception>
        public static CatmullRomSpline<TPos, TDiff> ToSplineOrThrow<TPos, TDiff>(
            this IEnumerable<TPos> source, GeometricOperations<TPos, TDiff> ops, SplineType? splineType = null
        ) where TPos : struct where TDiff : struct {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 2)
                throw new InsufficientNodePositionsException(positions.Count, 2);

            var (beginHandle, endHandle) = positions.calculateSplineMarginHandles(ops);
            return new CatmullRomSpline<TPos, TDiff>(beginHandle, positions, endHandle, ops, splineType);
        }

        /// Creates a spline that interpolates the provided positions.
        /// The margin handles of the spline are created automatically
        /// using <see cref="calculateSplineMarginHandles{TPos,TDiff}"/>.
        ///
        /// This should be used when the trajectory at the spline's begin / end
        /// should just be similar to the trajectory of the rest of the spline.
        /// 
        /// Returns None if too few positions are provided
        public static Option<CatmullRomSpline<TPos, TDiff>> ToSpline<TPos, TDiff>(
            this IEnumerable<TPos> source, GeometricOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 2) return Option.None;
            var (beginHandle, endHandle) = positions.calculateSplineMarginHandles(ops);
            return new CatmullRomSpline<TPos, TDiff>(beginHandle, positions, endHandle, ops, type);
        }

        /// <summary>
        /// Creates a spline from the provided positions.
        /// The first and last position of the IEnumerable become the begin and end handles,
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
        public static CatmullRomSpline<TPos, TDiff> ToSplineWithMarginHandlesOrThrow<TPos, TDiff>(
            this IEnumerable<TPos> source, GeometricOperations<TPos, TDiff> ops, SplineType? type = null
        ) where TPos : struct where TDiff : struct {
            var positions = source.AsReadOnlyCollection();
            if (positions.Count < 4)
                throw new InsufficientNodePositionsException(positions.Count, 4);

            return new CatmullRomSpline<TPos, TDiff>(positions, ops, type);
        }

        private static TPos pointReflect<TPos, TDiff>(this TPos self, TPos on, GeometricOperations<TPos, TDiff> ops)
            where TPos : struct where TDiff : struct 
            => ops.Add(on, ops.Sub(on, self));
    }

}
