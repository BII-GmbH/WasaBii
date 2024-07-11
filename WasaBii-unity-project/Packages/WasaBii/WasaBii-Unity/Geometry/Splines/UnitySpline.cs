using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    [MustBeImmutable]
    public interface UnityGeometricOperations<TTime, TVel> : GeometricOperations<Vector3, Vector3, TTime, TVel> 
    where TTime : unmanaged where TVel : unmanaged
    {
        Length GeometricOperations<Vector3, Vector3, TTime, TVel>.Distance(Vector3 p0, Vector3 p1) => p0.DistanceTo(p1).Meters();

        Vector3 GeometricOperations<Vector3, Vector3, TTime, TVel>.Div(Vector3 diff, double d) => diff / (float) d;
        Vector3 GeometricOperations<Vector3, Vector3, TTime, TVel>.Mul(Vector3 diff, double f) => diff * (float) f;
        double GeometricOperations<Vector3, Vector3, TTime, TVel>.Dot(Vector3 a, Vector3 b) => a.Dot(b);
            
        Vector3 GeometricOperations<Vector3, Vector3, TTime, TVel>.ZeroDiff => Vector3.zero;
    }

    [Serializable]
    public sealed class UnitySpline : SpecificSplineBase<UnitySpline, Vector3, Vector3, Duration, Vector3> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(IEnumerable{Tuple{TPos,TTime}},GeometricOperations{TPos,TDiff,TTime,TVel},Nullable{SplineType})"/>
        [Pure]
        public static UnitySpline FromHandles(IEnumerable<(Vector3, Duration)> source, SplineType type = SplineType.Centripetal)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(TPos,IEnumerable{Tuple{TPos,TTime}},TPos,GeometricOperations{TPos,TDiff,TTime,TVel},Nullable{SplineType})"/>
        [Pure]
        public static UnitySpline FromHandles(
            Vector3 beginMarginHandle, 
            IEnumerable<(Vector3, Duration)> interpolatedHandles, 
            Vector3 endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static UnitySpline FromHandlesIncludingMargin(
            IEnumerable<Vector3> allHandlesIncludingMargin,
            IEnumerable<Duration> segmentStartTimes,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, segmentStartTimes, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static UnitySpline FromHandlesWithVelocities(
            IEnumerable<(Vector3 position, Vector3 velocity, Duration time)> handles,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static UnitySpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(Vector3 position, Vector3 velocity, Vector3 acceleration, Duration time)> handles
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance));

#endregion

        public UnitySpline(Spline<Vector3, Vector3, Duration, Vector3> wrapped) : base(wrapped) { }
        protected override UnitySpline mkNew(Spline<Vector3, Vector3, Duration, Vector3> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : UnityGeometricOperations<Duration, Vector3>
        {
            public static readonly GeometricOperations Instance = new();
            public Vector3 ZeroVel => Vector3.zero;
            public Duration ZeroTime => Duration.Zero;
            public Duration UnitTime => new() { SiValue = 1 };
            public double Div(Duration a, Duration b) => a / b;
            public Vector3 Add(Vector3 a, Vector3 b) => a + b;
            public Vector3 Mul(Vector3 v, Duration t) => v * (float) t.AsSeconds();
            public Vector3 Div(Vector3 d, Duration t) => d / (float) t.AsSeconds();
            public Duration Add(Duration a, Duration b) => a + b;
            public Duration Sub(Duration a, Duration b) => a - b;
            public Duration Mul(Duration a, double b) => a * b;
            public Vector3 Sub(Vector3 p0, Vector3 p1) => p0 - p1;
            public Vector3 Lerp(Vector3 from, Vector3 to, double t) => Vector3.Lerp(from, to, (float)t);
        }
        
    }

    [Serializable]
    public sealed class UniformUnitySpline : SpecificSplineBase<UniformUnitySpline, Vector3, Vector3, double, Vector3> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff,double,TDiff},Nullable{SplineType},bool)"/>
        [Pure]
        public static UniformUnitySpline FromHandles(IEnumerable<Vector3> source, SplineType type = SplineType.Centripetal, bool shouldLoop = false)
            => new(CatmullRomSpline.UniformFromHandlesOrThrow(source, GeometricOperations.Instance, type, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(TPos,IEnumerable{TPos},TPos,GeometricOperations{TPos,TDiff,double,TDiff},Nullable{SplineType})"/>
        [Pure]
        public static UniformUnitySpline FromHandles(
            Vector3 beginMarginHandle, 
            IEnumerable<Vector3> interpolatedHandles, 
            Vector3 endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow{TPos,TDiff}"/>
        [Pure]
        public static UniformUnitySpline FromHandlesIncludingMargin(
            IEnumerable<Vector3> allHandlesIncludingMargin,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static UniformUnitySpline FromHandlesWithVelocities(
            IEnumerable<(Vector3 position, Vector3 velocity)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.UniformFromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithVelocitiesAndAccelerations{TPos,TDiff}"/>
        [Pure]
        public static UniformUnitySpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(Vector3 position, Vector3 velocity, Vector3 acceleration)> handles, bool shouldLoop = false
        ) => new(BezierSpline.UniformFromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance, shouldLoop));

#endregion

        public UniformUnitySpline(Spline<Vector3, Vector3, double, Vector3> wrapped) : base(wrapped) { }
        protected override UniformUnitySpline mkNew(Spline<Vector3, Vector3, double, Vector3> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : UnityGeometricOperations<double, Vector3>
        {
            public static readonly GeometricOperations Instance = new();
            public Vector3 ZeroVel => Vector3.zero;
            public double ZeroTime => 0;
            public double UnitTime => 1;
            public double Div(double a, double b) => a / b;
            public double Add(double a, double b) => a + b;
            public double Sub(double a, double b) => a - b;
            public double Mul(double a, double b) => a * b;
            public Vector3 Add(Vector3 a, Vector3 b) => a + b;
            public Vector3 Mul(Vector3 a, double b) => a * (float) b;
            public Vector3 Div(Vector3 a, double b) => a / (float) b;
            public Vector3 Sub(Vector3 p0, Vector3 p1) => p0 - p1;
            public Vector3 Lerp(Vector3 from, Vector3 to, double t) => Vector3.Lerp(from, to, (float)t);
        }

    }
}