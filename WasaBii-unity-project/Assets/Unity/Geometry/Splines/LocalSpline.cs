using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class LocalSpline {
    
        /// <inheritdoc cref="CatmullRomSpline.FromInterpolating{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> FromInterpolating(
            IEnumerable<LocalPosition> handles, SplineType? type = null
        ) => CatmullRomSpline.FromInterpolating(handles, GeometricOperations.Instance, type);
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> FromHandles(
            LocalPosition beginMarginHandle, 
            IEnumerable<LocalPosition> interpolatedHandles, 
            LocalPosition endMarginHandle, 
            SplineType? type = null
        ) => CatmullRomSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> FromHandlesIncludingMargin(
            IEnumerable<LocalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => CatmullRomSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, GeometricOperations.Instance, type);

#region Extensions
        /// <inheritdoc cref="GenericEnumerableToCatmullRomSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> ToSplineOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(GeometricOperations.Instance, splineType);

        /// <inheritdoc cref="GenericEnumerableToCatmullRomSplineExtensions.ToSpline{TPos,TDiff}"/>
        [Pure]
        public static Option<CatmullRomSpline<LocalPosition, LocalOffset>> ToSpline(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSpline(GeometricOperations.Instance, splineType);

        /// <inheritdoc cref="GenericEnumerableToCatmullRomSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> ToSplineWithMarginHandlesOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(GeometricOperations.Instance, splineType);

        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> ToGlobalWith(
            this CatmullRomSpline<LocalPosition, LocalOffset> local, TransformProvider parent
        ) => local.HandlesIncludingMargin.Select(l => l.ToGlobalWith(parent)).ToSplineWithMarginHandlesOrThrow();

        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineToOrThrow{TPos, TDiff}"/>
        [Pure]
        public static ClosestOnSplineQueryResult< LocalPosition, LocalOffset> QueryClosestPositionOnSplineToOrThrow(
            this Spline<LocalPosition, LocalOffset> spline,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineToOrThrow<LocalPosition, LocalOffset>(position, samples);
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff}"/>
        [Pure]
        public static Option<ClosestOnSplineQueryResult<LocalPosition, LocalOffset>> QueryClosestPositionOnSplineTo(
            this Spline<LocalPosition, LocalOffset> spline,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineTo<LocalPosition, LocalOffset>(position, samples);

        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<LocalPosition, LocalOffset> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<LocalPosition, LocalOffset>
            => splines.QueryClosestPositionOnSplinesTo<TWithSpline, LocalPosition, LocalOffset>(position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<LocalPosition, LocalOffset> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<LocalPosition, LocalOffset>
            => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, LocalPosition, LocalOffset>(position, samples);
        
#endregion
        
        [MustBeImmutable][MustBeSerializable]
        public sealed class GeometricOperations : GeometricOperations<LocalPosition, LocalOffset> {

            public static readonly GeometricOperations Instance = new();
            
            private GeometricOperations() { }

            public Length Distance(LocalPosition p0, LocalPosition p1) => p0.DistanceTo(p1);

            public LocalOffset Sub(LocalPosition p0, LocalPosition p1) => p0 - p1;
            public LocalPosition Sub(LocalPosition p, LocalOffset d) => p - d;

            public LocalOffset Sub(LocalOffset d1, LocalOffset d2) => d1 - d2;

            public LocalPosition Add(LocalPosition d1, LocalOffset d2) => d1 + d2;

            public LocalOffset Add(LocalOffset d1, LocalOffset d2) => d1 + d2;

            public LocalOffset Div(LocalOffset diff, double d) => diff / d;

            public LocalOffset Mul(LocalOffset diff, double f) => diff * f;

            public double Dot(LocalOffset a, LocalOffset b) => a.Dot(b);
        }
        
    }
}