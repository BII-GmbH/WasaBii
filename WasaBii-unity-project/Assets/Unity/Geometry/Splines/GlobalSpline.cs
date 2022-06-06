using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Splines;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class GlobalSpline {
        
        /// <inheritdoc cref="GenericSpline.FromInterpolating{TPos,TDiff}"/>
        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> FromInterpolating(
            IEnumerable<GlobalPosition> handles, SplineType? type = null
        ) => GenericSpline.FromInterpolating(handles, PositionOperations.Instance, type);
        
        /// <inheritdoc cref="GenericSpline.FromHandles{TPos,TDiff}"/>
        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> FromHandles(
            GlobalPosition beginMarginHandle, 
            IEnumerable<GlobalPosition> interpolatedHandles, 
            GlobalPosition endMarginHandle, 
            SplineType? type = null
        ) => GenericSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, PositionOperations.Instance, type);

        /// <inheritdoc cref="GenericSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> FromHandlesIncludingMargin(
            IEnumerable<GlobalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => GenericSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, PositionOperations.Instance, type);

#region Extensions
        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> ToSplineOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        [Pure]
        public static Option<Spline<GlobalPosition, GlobalOffset>> ToSpline(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSpline(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> ToSplineWithMarginHandlesOrThrow(this IEnumerable<GlobalPosition> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(PositionOperations.Instance, splineType);
        
        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        [Pure]
        public static (GlobalPosition BeginHandle, GlobalPosition EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<GlobalPosition> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(PositionOperations.Instance);

        [Pure]
        public static Spline<LocalPosition, LocalOffset> RelativeTo(
            this Spline<GlobalPosition, GlobalOffset> global, TransformProvider parent
        ) => global.HandlesIncludingMargin.Select(l => l.RelativeTo(parent)).ToSplineWithMarginHandlesOrThrow();
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineToOrThrow{TPos, TDiff}"/>
        [Pure]
        public static ClosestOnSplineQueryResult< GlobalPosition, GlobalOffset> QueryClosestPositionOnSplineToOrThrow(
            this Spline<GlobalPosition, GlobalOffset> spline,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineToOrThrow<GlobalPosition, GlobalOffset>(position, samples);
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff}"/>
        [Pure]
        public static ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset>? QueryClosestPositionOnSplineTo(
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
        public sealed class PositionOperations : PositionOperations<GlobalPosition, GlobalOffset> {

            public static readonly PositionOperations Instance = new();
        
            private PositionOperations() { }

            public Length Distance(GlobalPosition p0, GlobalPosition p1) => p0.DistanceTo(p1);

            public GlobalOffset Sub(GlobalPosition p0, GlobalPosition p1) => p0 - p1;
            public GlobalPosition Sub(GlobalPosition p, GlobalOffset d) => p - d;

            public GlobalOffset Sub(GlobalOffset d1, GlobalOffset d2) => d1 - d2;

            public GlobalPosition Add(GlobalPosition d1, GlobalOffset d2) => d1 + d2;

            public GlobalOffset Add(GlobalOffset d1, GlobalOffset d2) => d1 + d2;

            public GlobalOffset Div(GlobalOffset diff, double d) => diff / d;

            public GlobalOffset Mul(GlobalOffset diff, double f) => diff * f;

            public double Dot(GlobalOffset a, GlobalOffset b) => a.Dot(b);
        }
    }
    
}