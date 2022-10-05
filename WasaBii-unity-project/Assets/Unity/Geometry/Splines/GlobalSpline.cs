using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class GlobalSpline {
        
        /// <inheritdoc cref="ToSplineOrThrow"/>
        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> FromInterpolating(
            IEnumerable<GlobalPosition> handles, SplineType? type = null, bool shouldLoop = false
        ) => handles.ToSplineOrThrow(type, shouldLoop);
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,BII.WasaBii.Splines.Maths.GeometricOperations{TPos,TDiff},System.Nullable{BII.WasaBii.Splines.CatmullRom.SplineType})"/>
        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> FromHandles(
            GlobalPosition beginMarginHandle, 
            IEnumerable<GlobalPosition> interpolatedHandles, 
            GlobalPosition endMarginHandle, 
            SplineType? type = null
        ) => CatmullRomSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> FromHandlesIncludingMargin(
            IEnumerable<GlobalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => CatmullRomSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, GeometricOperations.Instance, type);

#region Extensions
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> ToSplineOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos, TDiff},SplineType?,bool)"/>
        [Pure]
        public static Option<CatmullRomSpline<GlobalPosition, GlobalOffset>> ToSpline(this IEnumerable<GlobalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandles(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> ToSplineWithMarginHandlesOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => CatmullRomSpline.FromHandlesIncludingMargin(source, GeometricOperations.Instance, splineType);

        [Pure]
        public static BezierSpline<GlobalPosition, GlobalOffset> ToSpline(
            this IEnumerable<(GlobalPosition position, GlobalOffset velocity)> source, bool shouldLoop = false
        ) => BezierSpline.FromHandlesWithVelocities(source, GeometricOperations.Instance, shouldLoop);

        [Pure]
        public static Spline<LocalPosition, LocalOffset> RelativeTo(
            this Spline<GlobalPosition, GlobalOffset> global, TransformProvider parent
        ) => global.Map(l => l.RelativeTo(parent), LocalSpline.GeometricOperations.Instance);
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineToOrThrow{TPos, TDiff}"/>
        [Pure]
        public static ClosestOnSplineQueryResult< GlobalPosition, GlobalOffset> QueryClosestPositionOnSplineToOrThrow(
            this Spline<GlobalPosition, GlobalOffset> spline,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineToOrThrow<GlobalPosition, GlobalOffset>(position, samples);
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff}"/>
        [Pure]
        public static Option<ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset>> QueryClosestPositionOnSplineTo(
            this Spline<GlobalPosition, GlobalOffset> spline,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineTo<GlobalPosition, GlobalOffset>(position, samples);

        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<GlobalPosition, GlobalOffset>
            => splines.QueryClosestPositionOnSplinesTo<TWithSpline, GlobalPosition, GlobalOffset>(position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<GlobalPosition, GlobalOffset>
            => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, GlobalPosition, GlobalOffset>(position, samples);

#endregion
        
        [MustBeImmutable][MustBeSerializable]
        public sealed class GeometricOperations : GeometricOperations<GlobalPosition, GlobalOffset> {

            public static readonly GeometricOperations Instance = new();
        
            private GeometricOperations() { }

            public Length Distance(GlobalPosition p0, GlobalPosition p1) => p0.DistanceTo(p1);

            public GlobalOffset Sub(GlobalPosition p0, GlobalPosition p1) => p0 - p1;
            public GlobalPosition Sub(GlobalPosition p, GlobalOffset d) => p - d;

            public GlobalOffset Sub(GlobalOffset d1, GlobalOffset d2) => d1 - d2;

            public GlobalPosition Add(GlobalPosition d1, GlobalOffset d2) => d1 + d2;

            public GlobalOffset Add(GlobalOffset d1, GlobalOffset d2) => d1 + d2;

            public GlobalOffset Div(GlobalOffset diff, double d) => diff / d;

            public GlobalOffset Mul(GlobalOffset diff, double f) => diff * f;

            public double Dot(GlobalOffset a, GlobalOffset b) => a.Dot(b);
            
            public GlobalOffset ZeroDiff => GlobalOffset.Zero;
            
            public GlobalPosition Lerp(GlobalPosition from, GlobalPosition to, double t) => GlobalPosition.Lerp(from, to, t);
            public GlobalOffset Lerp(GlobalOffset from, GlobalOffset to, double t) => GlobalOffset.Lerp(from, to, t);
        }
    }
    
}