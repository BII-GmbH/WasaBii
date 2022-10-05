using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class LocalSpline {
    
        /// <inheritdoc cref="ToSplineOrThrow"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> FromInterpolating(
            IEnumerable<LocalPosition> handles, SplineType? type = null, bool shouldLoop = false
        ) => handles.ToSplineOrThrow(type, shouldLoop);
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,BII.WasaBii.Splines.Maths.GeometricOperations{TPos,TDiff},System.Nullable{BII.WasaBii.Splines.CatmullRom.SplineType})"/>
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
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> ToSplineOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos, TDiff},SplineType?,bool)"/>
        [Pure]
        public static Option<CatmullRomSpline<LocalPosition, LocalOffset>> ToSpline(this IEnumerable<LocalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandles(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<LocalPosition, LocalOffset> ToSplineWithMarginHandlesOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => CatmullRomSpline.FromHandlesIncludingMargin(source, GeometricOperations.Instance, splineType);

        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> ToGlobalWith(
            this Spline<LocalPosition, LocalOffset> local, TransformProvider parent
        ) => local.Map(l => l.ToGlobalWith(parent), GlobalSpline.GeometricOperations.Instance);

        [Pure]
        public static BezierSpline<LocalPosition, LocalOffset> ToSpline(
            this IEnumerable<(LocalPosition position, LocalOffset velocity)> source, bool shouldLoop = false
        ) => BezierSpline.FromHandlesWithVelocities(source, GeometricOperations.Instance, shouldLoop);

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
            
            public LocalOffset ZeroDiff => LocalOffset.Zero;

            public LocalPosition Lerp(LocalPosition from, LocalPosition to, double t) => LocalPosition.Lerp(from, to, t);
            public LocalOffset Lerp(LocalOffset from, LocalOffset to, double t) => LocalOffset.Lerp(from, to, t);
        }
        
    }
}