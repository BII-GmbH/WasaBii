using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Extra.Geometry {

    [Serializable]
    public sealed class LocalSpline : SpecificSplineBase<LocalSpline, LocalPosition, LocalOffset> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff},SplineType?,bool)"/>
        [Pure]
        public static LocalSpline FromHandles(IEnumerable<LocalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,GeometricOperations{TPos,TDiff},System.Nullable{SplineType})"/>
        [Pure]
        public static LocalSpline FromHandles(
            LocalPosition beginMarginHandle, 
            IEnumerable<LocalPosition> interpolatedHandles, 
            LocalPosition endMarginHandle, 
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static LocalSpline FromHandlesIncludingMargin(
            IEnumerable<LocalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static LocalSpline FromHandlesWithVelocities(
            IEnumerable<(LocalPosition position, LocalOffset velocity)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static LocalSpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(LocalPosition position, LocalOffset velocity, LocalOffset acceleration)> handles, bool shouldLoop = false
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance, shouldLoop));

#endregion

        [Pure]
        public GlobalSpline ToGlobalWith(TransformProvider parent) => 
            new(Map(l => l.ToGlobalWith(parent), GlobalSpline.GeometricOperations.Instance));

        public LocalSpline(Spline<LocalPosition, LocalOffset> wrapped) : base(wrapped) { }
        protected override LocalSpline mkNew(Spline<LocalPosition, LocalOffset> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
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

            public double Dot(LocalOffset a, LocalOffset b) => a.Dot(b).AsSquareMeters();
            
            public LocalOffset ZeroDiff => LocalOffset.Zero;

            public LocalPosition Lerp(LocalPosition from, LocalPosition to, double t) => LocalPosition.Lerp(from, to, t);
            public LocalOffset Lerp(LocalOffset from, LocalOffset to, double t) => LocalOffset.Lerp(from, to, t);
        }

    }

    public static class LocalSplineExtensions {
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<LocalPosition, LocalOffset> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, LocalSpline> splineSelector,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesTo<TWithSpline, LocalPosition, LocalOffset>(splineSelector, position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<LocalPosition, LocalOffset> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, LocalSpline> splineSelector,
            LocalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, LocalPosition, LocalOffset>(splineSelector, position, samples);
        
    }
}