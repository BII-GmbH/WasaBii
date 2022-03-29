using BII.WasaBii.CatmullRomSplines;
using System.Collections.Generic;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    using UnitySpline = Spline<Vector3, Vector3>;
    using LocalSpline = Spline<LocalPosition, LocalOffset>;
    using GlobalSpline = Spline<GlobalPosition, GlobalOffset>;
    
    /// Non-generic variants of the functions in <see cref="CatmullRomSplines.EnumerableToSplineExtensions"/> for local,
    /// global and Unity splines with the respective <see cref="PositionOperations{TPos,TDiff}"/> given.
    public static class EnumerableToSplineExtensions {

        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        public static (Vector3 BeginHandle, Vector3 EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<Vector3> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(UnityVectorOps.Instance);

        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        public static (LocalPosition BeginHandle, LocalPosition EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<LocalPosition> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(LocalSplineOps.Instance);

        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        public static (GlobalPosition BeginHandle, GlobalPosition EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<GlobalPosition> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(GlobalSplineOps.Instance);


        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        public static UnitySpline ToSplineOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(UnityVectorOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        public static LocalSpline ToSplineOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(LocalSplineOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        public static GlobalSpline ToSplineOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(GlobalSplineOps.Instance, splineType);
        
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        public static Option<UnitySpline> ToSpline(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSpline(UnityVectorOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        public static Option<LocalSpline> ToSpline(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSpline(LocalSplineOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        public static Option<GlobalSpline> ToSpline(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSpline(GlobalSplineOps.Instance, splineType);
        
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        public static UnitySpline ToSplineWithMarginHandlesOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(UnityVectorOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        public static LocalSpline ToSplineWithMarginHandlesOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(LocalSplineOps.Instance, splineType);
        
        /// <inheritdoc cref="CatmullRomSplines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        public static GlobalSpline ToSplineWithMarginHandlesOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(GlobalSplineOps.Instance, splineType);
        
        
    }
    
}