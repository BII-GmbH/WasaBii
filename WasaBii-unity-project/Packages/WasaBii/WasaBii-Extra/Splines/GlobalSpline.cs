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

    [MustBeImmutable]
    public interface GlobalGeometricOperations<TTime, TVel> : GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel> 
    where TTime : unmanaged where TVel : unmanaged
    {
        Length GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Distance(GlobalPosition p0, GlobalPosition p1) => p0.DistanceTo(p1);

        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Sub(GlobalPosition p0, GlobalPosition p1) => p0 - p1;
        GlobalPosition GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Sub(GlobalPosition p, GlobalOffset d) => p - d;
        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Sub(GlobalOffset d1, GlobalOffset d2) => d1 - d2;

        GlobalPosition GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Add(GlobalPosition d1, GlobalOffset d2) => d1 + d2;
        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Add(GlobalOffset d1, GlobalOffset d2) => d1 + d2;

        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Div(GlobalOffset diff, double d) => diff / d;
        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Mul(GlobalOffset diff, double f) => diff * f;
        double GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Dot(GlobalOffset a, GlobalOffset b) => a.Dot(b).AsSquareMeters();
            
        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.ZeroDiff => GlobalOffset.Zero;
            
        GlobalPosition GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Lerp(GlobalPosition from, GlobalPosition to, double t) => GlobalPosition.Lerp(from, to, t);
        GlobalOffset GeometricOperations<GlobalPosition, GlobalOffset, TTime, TVel>.Lerp(GlobalOffset from, GlobalOffset to, double t) => GlobalOffset.Lerp(from, to, t);
    }

    [Serializable]
    public sealed class GlobalSpline : SpecificSplineBase<GlobalSpline, GlobalPosition, GlobalOffset, Duration, GlobalVelocity> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(IEnumerable{Tuple{TPos,TTime}},GeometricOperations{TPos,TDiff,TTime,TVel},SplineType)"/>
        [Pure]
        public static GlobalSpline FromHandles(IEnumerable<(GlobalPosition, Duration)> source, SplineType type = SplineType.Centripetal)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(TPos,IEnumerable{Tuple{TPos,TTime}},TPos,GeometricOperations{TPos,TDiff,TTime,TVel},SplineType)"/>
        [Pure]
        public static GlobalSpline FromHandles(
            GlobalPosition beginMarginHandle, 
            IEnumerable<(GlobalPosition, Duration)> interpolatedHandles, 
            GlobalPosition endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static GlobalSpline FromHandlesIncludingMargin(
            IEnumerable<GlobalPosition> allHandlesIncludingMargin,
            IEnumerable<Duration> segmentStartTimes,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, segmentStartTimes, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static GlobalSpline FromHandlesWithVelocities(
            IEnumerable<(GlobalPosition position, GlobalVelocity velocity, Duration time)> handles,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static GlobalSpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(GlobalPosition position, GlobalVelocity velocity, GlobalVelocity acceleration, Duration time)> handles
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance));

#endregion

        [Pure]
        public LocalSpline RelativeTo(TransformProvider parent) => 
            new(Map(l => l.RelativeTo(parent), LocalSpline.GeometricOperations.Instance));

        public GlobalSpline(Spline<GlobalPosition, GlobalOffset, Duration, GlobalVelocity> wrapped) : base(wrapped) { }
        protected override GlobalSpline mkNew(Spline<GlobalPosition, GlobalOffset, Duration, GlobalVelocity> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : GlobalGeometricOperations<Duration, GlobalVelocity>
        {
            public static readonly GeometricOperations Instance = new();
            public GlobalVelocity ZeroVel => GlobalVelocity.Zero;
            public Duration ZeroTime => Duration.Zero;
            public double Div(Duration a, Duration b) => a / b;
            public GlobalOffset Mul(GlobalVelocity v, Duration t) => v * t;
            public GlobalVelocity Div(GlobalOffset d, Duration t) => d / t;
            public Duration Add(Duration a, Duration b) => a + b;
            public Duration Sub(Duration a, Duration b) => a - b;
            public Duration Mul(Duration a, double b) => a * b;
        }
        
    }

    [Serializable]
    public sealed class UniformGlobalSpline : SpecificSplineBase<UniformGlobalSpline, GlobalPosition, GlobalOffset, double, GlobalOffset> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff,double,TDiff},SplineType,bool)"/>
        [Pure]
        public static UniformGlobalSpline FromHandles(IEnumerable<GlobalPosition> source, SplineType type = SplineType.Centripetal, bool shouldLoop = false)
            => new(CatmullRomSpline.UniformFromHandlesOrThrow(source, GeometricOperations.Instance, type, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(TPos,IEnumerable{TPos},TPos,GeometricOperations{TPos,TDiff,double,TDiff},SplineType)"/>
        [Pure]
        public static UniformGlobalSpline FromHandles(
            GlobalPosition beginMarginHandle, 
            IEnumerable<GlobalPosition> interpolatedHandles, 
            GlobalPosition endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow{TPos,TDiff}"/>
        [Pure]
        public static UniformGlobalSpline FromHandlesIncludingMargin(
            IEnumerable<GlobalPosition> allHandlesIncludingMargin,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithTangents{TPos,TDiff}"/>
        [Pure]
        public static UniformGlobalSpline FromHandlesWithTangents(
            IEnumerable<(GlobalPosition position, GlobalOffset tangent)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.UniformFromHandlesWithTangents(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithTangentsAndCurvature{TPos,TDiff}"/>
        [Pure]
        public static UniformGlobalSpline FromHandlesWithTangentsAndCurvature(
            IEnumerable<(GlobalPosition position, GlobalOffset tangent, GlobalOffset curvature)> handles, bool shouldLoop = false
        ) => new(BezierSpline.UniformFromHandlesWithTangentsAndCurvature(handles, GeometricOperations.Instance, shouldLoop));

#endregion

        [Pure]
        public UniformLocalSpline RelativeTo(TransformProvider parent) => 
            new(Map(l => l.RelativeTo(parent), UniformLocalSpline.GeometricOperations.Instance));

        public UniformGlobalSpline(Spline<GlobalPosition, GlobalOffset, double, GlobalOffset> wrapped) : base(wrapped) { }
        protected override UniformGlobalSpline mkNew(Spline<GlobalPosition, GlobalOffset, double, GlobalOffset> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : GlobalGeometricOperations<double, GlobalOffset>
        {
            public static readonly GeometricOperations Instance = new();
            public GlobalOffset ZeroVel => GlobalOffset.Zero;
            public double ZeroTime => 0;
            public double Div(double a, double b) => a / b;
            public double Add(double a, double b) => a + b;
            public double Sub(double a, double b) => a - b;
            public double Mul(double a, double b) => a * b;
            public GlobalOffset Mul(GlobalOffset diff, double f) => diff * f;
            public GlobalOffset Div(GlobalOffset diff, double f) => diff / f;
        }

    }
}