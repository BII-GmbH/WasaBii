using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Splines;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class LocalSpline {
    
        /// <inheritdoc cref="GenericSpline.FromInterpolating{TPos,TDiff}"/>
        [Pure]
        public static Spline<LocalPosition, LocalOffset> FromInterpolating(
            IEnumerable<LocalPosition> handles, SplineType? type = null
        ) => GenericSpline.FromInterpolating(handles, PositionOperations.Instance, type);
        
        /// <inheritdoc cref="GenericSpline.FromHandles{TPos,TDiff}"/>
        [Pure]
        public static Spline<LocalPosition, LocalOffset> FromHandles(
            LocalPosition beginMarginHandle, 
            IEnumerable<LocalPosition> interpolatedHandles, 
            LocalPosition endMarginHandle, 
            SplineType? type = null
        ) => GenericSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, PositionOperations.Instance, type);

        /// <inheritdoc cref="GenericSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static Spline<LocalPosition, LocalOffset> FromHandlesIncludingMargin(
            IEnumerable<LocalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => GenericSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, PositionOperations.Instance, type);

#region Extensions
        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<LocalPosition, LocalOffset> ToSplineOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        [Pure]
        public static Option<Spline<LocalPosition, LocalOffset>> ToSpline(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSpline(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<LocalPosition, LocalOffset> ToSplineWithMarginHandlesOrThrow(this IEnumerable<LocalPosition> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        [Pure]
        public static (LocalPosition BeginHandle, LocalPosition EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<LocalPosition> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(PositionOperations.Instance);

        [Pure]
        public static Spline<GlobalPosition, GlobalOffset> ToGlobalWith(
            this Spline<LocalPosition, LocalOffset> local, TransformProvider parent
        ) => local.HandlesIncludingMargin.Select(l => l.ToGlobalWith(parent)).ToSplineWithMarginHandlesOrThrow();
#endregion
        
        [MustBeImmutable][MustBeSerializable]
        public sealed class PositionOperations : PositionOperations<LocalPosition, LocalOffset> {

            public static readonly PositionOperations Instance = new();
            
            private PositionOperations() { }

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