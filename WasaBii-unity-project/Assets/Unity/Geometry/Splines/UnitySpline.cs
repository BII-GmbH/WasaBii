﻿using System.Collections.Generic;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class UnitySpline {

        /// <inheritdoc cref="ToSplineOrThrow"/>
        [Pure]
        public static CatmullRomSpline<Vector3, Vector3> FromInterpolating(
            IEnumerable<Vector3> handles, SplineType? type = null, bool shouldLoop = false
        ) => handles.ToSplineOrThrow(type, shouldLoop);
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(TPos,System.Collections.Generic.IEnumerable{TPos},TPos,BII.WasaBii.Splines.Maths.GeometricOperations{TPos,TDiff},System.Nullable{BII.WasaBii.Splines.CatmullRom.SplineType})"/>
        [Pure]
        public static CatmullRomSpline<Vector3, Vector3> FromHandles(
            Vector3 beginMarginHandle, 
            IEnumerable<Vector3> interpolatedHandles, 
            Vector3 endMarginHandle, 
            SplineType? type = null
        ) => CatmullRomSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<Vector3, Vector3> FromHandlesIncludingMargin(
            IEnumerable<Vector3> allHandlesIncludingMargin,
            SplineType? type = null
        ) => CatmullRomSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, GeometricOperations.Instance, type);
        
#region Extensions
        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos, TDiff},SplineType?,bool)"/>
        [Pure]
        public static CatmullRomSpline<Vector3, Vector3> ToSplineOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandles{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos, TDiff},SplineType?,bool)"/>
        [Pure]
        public static Option<CatmullRomSpline<Vector3, Vector3>> ToSpline(this IEnumerable<Vector3> source, SplineType? splineType = null, bool shouldLoop = false)
            => CatmullRomSpline.FromHandles(source, GeometricOperations.Instance, splineType, shouldLoop);

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static CatmullRomSpline<Vector3, Vector3> ToSplineWithMarginHandlesOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => CatmullRomSpline.FromHandlesIncludingMargin(source, GeometricOperations.Instance, splineType);

        [Pure]
        public static BezierSpline<Vector3, Vector3> ToSpline(
            this IEnumerable<(Vector3 position, Vector3 velocity)> source, bool shouldLoop = false
        ) => BezierSpline.FromHandlesWithVelocities(source, GeometricOperations.Instance, shouldLoop);

        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineToOrThrow{TPos, TDiff}"/>
        [Pure]
        public static ClosestOnSplineQueryResult< Vector3, Vector3> QueryClosestPositionOnSplineToOrThrow(
            this Spline<Vector3, Vector3> spline,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineToOrThrow<Vector3, Vector3>(position, samples);
        
        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff}"/>
        [Pure]
        public static Option<ClosestOnSplineQueryResult<Vector3, Vector3>> QueryClosestPositionOnSplineTo(
            this Spline<Vector3, Vector3> spline,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => spline.QueryClosestPositionOnSplineTo<Vector3, Vector3>(position, samples);

        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<Vector3, Vector3> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<Vector3, Vector3>
            => splines.QueryClosestPositionOnSplinesTo<TWithSpline, Vector3, Vector3>(position, samples);
        
        /// <inheritdoc cref="EnumerableClosestOnSplineExtensions.QueryClosestPositionOnSplinesToOrThrow{TWithSpline, TPos, TDiff}"/>
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<Vector3, Vector3> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline<Vector3, Vector3>
            => splines.QueryClosestPositionOnSplinesToOrThrow<TWithSpline, Vector3, Vector3>(position, samples);

#endregion
        
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
    
}