using System;
using System.Collections.Generic;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry.Splines {

    [MustBeSerializable]
    public sealed class GlobalSpline : SpecificSplineBase<GlobalSpline, GlobalPosition, GlobalOffset> {
        
#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff},SplineType?,bool)"/>
        [Pure]
        public static GlobalSpline FromHandles(IEnumerable<GlobalPosition> source, SplineType? splineType = null, bool shouldLoop = false)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,BII.WasaBii.Splines.Maths.GeometricOperations{TPos,TDiff},System.Nullable{BII.WasaBii.Splines.CatmullRom.SplineType})"/>
        [Pure]
        public static GlobalSpline FromHandles(
            GlobalPosition beginMarginHandle, 
            IEnumerable<GlobalPosition> interpolatedHandles, 
            GlobalPosition endMarginHandle, 
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static GlobalSpline FromHandlesIncludingMargin(
            IEnumerable<GlobalPosition> allHandlesIncludingMargin,
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static GlobalSpline FromHandlesWithVelocities(
            IEnumerable<(GlobalPosition position, GlobalOffset velocity)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static GlobalSpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(GlobalPosition position, GlobalOffset velocity, GlobalOffset acceleration)> handles, bool shouldLoop = false
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance, shouldLoop));

#endregion

        [Pure]
        public LocalSpline RelativeTo(TransformProvider parent) => 
            new(Map(l => l.RelativeTo(parent), LocalSpline.GeometricOperations.Instance));

        public GlobalSpline(Spline<GlobalPosition, GlobalOffset> wrapped) : base(wrapped) { }
        protected override GlobalSpline mkNew(Spline<GlobalPosition, GlobalOffset> toWrap) => new(toWrap);

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

    public static class GlobalSplineExtensions {
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, GlobalSpline> splineSelector,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesTo<TWithSpline, GlobalPosition, GlobalOffset>(splineSelector, position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<GlobalPosition, GlobalOffset> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, GlobalSpline> splineSelector,
            GlobalPosition position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, GlobalPosition, GlobalOffset>(splineSelector, position, samples);
        
    }
}