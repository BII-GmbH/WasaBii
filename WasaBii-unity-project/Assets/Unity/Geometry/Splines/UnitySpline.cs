using System;
using System.Collections.Generic;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {

    [MustBeSerializable]
    public sealed class UnitySpline : SpecificSplineBase<UnitySpline, Vector3, Vector3> {
        
#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff},SplineType?,bool)"/>
        [Pure]
        public static UnitySpline FromHandles(IEnumerable<Vector3> source, SplineType? splineType = null, bool shouldLoop = false)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,BII.WasaBii.Splines.Maths.GeometricOperations{TPos,TDiff},System.Nullable{BII.WasaBii.Splines.CatmullRom.SplineType})"/>
        [Pure]
        public static UnitySpline FromHandles(
            Vector3 beginMarginHandle, 
            IEnumerable<Vector3> interpolatedHandles, 
            Vector3 endMarginHandle, 
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static UnitySpline FromHandlesIncludingMargin(
            IEnumerable<Vector3> allHandlesIncludingMargin,
            SplineType? type = null
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static UnitySpline FromHandlesWithVelocities(
            IEnumerable<(Vector3 position, Vector3 velocity)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static UnitySpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(Vector3 position, Vector3 velocity, Vector3 acceleration)> handles, bool shouldLoop = false
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance, shouldLoop));

#endregion
        
        public UnitySpline(Spline<Vector3, Vector3> wrapped) : base(wrapped) { }
        protected override UnitySpline mkNew(Spline<Vector3, Vector3> toWrap) => new(toWrap);

        [MustBeImmutable][MustBeSerializable]
        public sealed class GeometricOperations : GeometricOperations<Vector3, Vector3> {

            public static readonly GeometricOperations Instance = new();
            
            private GeometricOperations() { }

            public Length Distance(Vector3 p0, Vector3 p1) => p0.DistanceTo(p1).Meters();

            public Vector3 Sub(Vector3 p0, Vector3 p1) => p0 - p1;

            public Vector3 Add(Vector3 d1, Vector3 d2) => d1 + d2;

            public Vector3 Div(Vector3 diff, double d) => diff / (float)d;

            public Vector3 Mul(Vector3 diff, double f) => diff * (float)f;

            public double Dot(Vector3 a, Vector3 b) => Vector3.Dot(a, b);
            
            public Vector3 ZeroDiff => Vector3.zero;
            
            public Vector3 Lerp(Vector3 from, Vector3 to, double t) => Vector3.Lerp(from, to, (float)t);
        }

    }

    public static class UnitySplineExtensions {
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<Vector3, Vector3> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, UnitySpline> splineSelector,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesTo<TWithSpline, Vector3, Vector3>(splineSelector, position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<Vector3, Vector3> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, UnitySpline> splineSelector,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, Vector3, Vector3>(splineSelector, position, samples);
        
    }
}