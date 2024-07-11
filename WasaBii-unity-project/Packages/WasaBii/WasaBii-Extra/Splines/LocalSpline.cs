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
    public interface LocalGeometricOperations<TTime, TVel> : GeometricOperations<LocalPosition, LocalOffset, TTime, TVel> 
    where TTime : unmanaged where TVel : unmanaged
    {
        Length GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Distance(LocalPosition p0, LocalPosition p1) => p0.DistanceTo(p1);

        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Sub(LocalPosition p0, LocalPosition p1) => p0 - p1;
        LocalPosition GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Sub(LocalPosition p, LocalOffset d) => p - d;
        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Sub(LocalOffset d1, LocalOffset d2) => d1 - d2;

        LocalPosition GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Add(LocalPosition d1, LocalOffset d2) => d1 + d2;
        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Add(LocalOffset d1, LocalOffset d2) => d1 + d2;

        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Div(LocalOffset diff, double d) => diff / d;
        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Mul(LocalOffset diff, double f) => diff * f;
        double GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Dot(LocalOffset a, LocalOffset b) => a.Dot(b).AsSquareMeters();
            
        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.ZeroDiff => LocalOffset.Zero;
            
        LocalPosition GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Lerp(LocalPosition from, LocalPosition to, double t) => LocalPosition.Lerp(from, to, t);
        LocalOffset GeometricOperations<LocalPosition, LocalOffset, TTime, TVel>.Lerp(LocalOffset from, LocalOffset to, double t) => LocalOffset.Lerp(from, to, t);
    }

    [Serializable]
    public sealed class LocalSpline : SpecificSplineBase<LocalSpline, LocalPosition, LocalOffset, Duration, LocalVelocity> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(IEnumerable{Tuple{TPos,TTime}},GeometricOperations{TPos,TDiff,TTime,TVel},Nullable{SplineType})"/>
        [Pure]
        public static LocalSpline FromHandles(IEnumerable<(LocalPosition, Duration)> source, SplineType type = SplineType.Centripetal)
            => new(CatmullRomSpline.FromHandlesOrThrow(source, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesOrThrow{TPos,TDiff,TTime,TVel}(TPos,IEnumerable{Tuple{TPos,TTime}},TPos,GeometricOperations{TPos,TDiff,TTime,TVel},Nullable{SplineType})"/>
        [Pure]
        public static LocalSpline FromHandles(
            LocalPosition beginMarginHandle, 
            IEnumerable<(LocalPosition, Duration)> interpolatedHandles, 
            LocalPosition endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.FromHandlesIncludingMargin{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static LocalSpline FromHandlesIncludingMargin(
            IEnumerable<LocalPosition> allHandlesIncludingMargin,
            IEnumerable<Duration> segmentStartTimes,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.FromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, segmentStartTimes, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static LocalSpline FromHandlesWithVelocities(
            IEnumerable<(LocalPosition position, LocalVelocity velocity, Duration time)> handles,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.FromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.FromHandlesWithVelocities{TPos,TDiff,TTime,TVel}"/>
        [Pure]
        public static LocalSpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(LocalPosition position, LocalVelocity velocity, LocalVelocity acceleration, Duration time)> handles
        ) => new(BezierSpline.FromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance));

#endregion

        [Pure]
        public GlobalSpline ToGlobalWith(TransformProvider parent) => 
            new(Map(l => l.ToGlobalWith(parent), GlobalSpline.GeometricOperations.Instance));

        public LocalSpline(Spline<LocalPosition, LocalOffset, Duration, LocalVelocity> wrapped) : base(wrapped) { }
        protected override LocalSpline mkNew(Spline<LocalPosition, LocalOffset, Duration, LocalVelocity> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : LocalGeometricOperations<Duration, LocalVelocity>
        {
            public static readonly GeometricOperations Instance = new();
            public LocalVelocity ZeroVel => LocalVelocity.Zero;
            public Duration ZeroTime => Duration.Zero;
            public Duration UnitTime => new() { SiValue = 1 };
            public double Div(Duration a, Duration b) => a / b;
            public LocalOffset Mul(LocalVelocity v, Duration t) => v * t;
            public LocalVelocity Div(LocalOffset d, Duration t) => d / t;
            public Duration Add(Duration a, Duration b) => a + b;
            public Duration Sub(Duration a, Duration b) => a - b;
            public Duration Mul(Duration a, double b) => a * b;
        }
        
    }

    [Serializable]
    public sealed class UniformLocalSpline : SpecificSplineBase<UniformLocalSpline, LocalPosition, LocalOffset, double, LocalOffset> {

#region Factory Methods
        
        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(IEnumerable{TPos},GeometricOperations{TPos,TDiff,double,TDiff},Nullable{SplineType},bool)"/>
        [Pure]
        public static UniformLocalSpline FromHandles(IEnumerable<LocalPosition> source, SplineType type = SplineType.Centripetal, bool shouldLoop = false)
            => new(CatmullRomSpline.UniformFromHandlesOrThrow(source, GeometricOperations.Instance, type, shouldLoop));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesOrThrow{TPos,TDiff}(TPos,IEnumerable{TPos},TPos,GeometricOperations{TPos,TDiff,double,TDiff},Nullable{SplineType})"/>
        [Pure]
        public static UniformLocalSpline FromHandles(
            LocalPosition beginMarginHandle, 
            IEnumerable<LocalPosition> interpolatedHandles, 
            LocalPosition endMarginHandle, 
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesOrThrow(beginMarginHandle, interpolatedHandles, endMarginHandle, GeometricOperations.Instance, type));

        /// <inheritdoc cref="CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow{TPos,TDiff}"/>
        [Pure]
        public static UniformLocalSpline FromHandlesIncludingMargin(
            IEnumerable<LocalPosition> allHandlesIncludingMargin,
            SplineType type = SplineType.Centripetal
        ) => new(CatmullRomSpline.UniformFromHandlesIncludingMarginOrThrow(allHandlesIncludingMargin, GeometricOperations.Instance, type));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithVelocities{TPos,TDiff}"/>
        [Pure]
        public static UniformLocalSpline FromHandlesWithVelocities(
            IEnumerable<(LocalPosition position, LocalOffset velocity)> handles, bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) => new(BezierSpline.UniformFromHandlesWithVelocities(handles, GeometricOperations.Instance, shouldLoop, shouldAccelerationBeContinuous));

        /// <inheritdoc cref="BezierSpline.UniformFromHandlesWithVelocitiesAndAccelerations{TPos,TDiff}"/>
        [Pure]
        public static UniformLocalSpline FromHandlesWithVelocitiesAndAccelerations(
            IEnumerable<(LocalPosition position, LocalOffset velocity, LocalOffset acceleration)> handles, bool shouldLoop = false
        ) => new(BezierSpline.UniformFromHandlesWithVelocitiesAndAccelerations(handles, GeometricOperations.Instance, shouldLoop));

#endregion

        [Pure]
        public UniformGlobalSpline ToGlobalWith(TransformProvider parent) => 
            new(Map(l => l.ToGlobalWith(parent), UniformGlobalSpline.GeometricOperations.Instance));

        public UniformLocalSpline(Spline<LocalPosition, LocalOffset, double, LocalOffset> wrapped) : base(wrapped) { }
        protected override UniformLocalSpline mkNew(Spline<LocalPosition, LocalOffset, double, LocalOffset> toWrap) => new(toWrap);

        [MustBeImmutable][Serializable]
        public sealed class GeometricOperations : LocalGeometricOperations<double, LocalOffset>
        {
            public static readonly GeometricOperations Instance = new();
            public LocalOffset ZeroVel => LocalOffset.Zero;
            public double ZeroTime => 0;
            public double UnitTime => 1;
            public double Div(double a, double b) => a / b;
            public double Add(double a, double b) => a + b;
            public double Sub(double a, double b) => a - b;
            public double Mul(double a, double b) => a * b;
            public LocalOffset Mul(LocalOffset diff, double f) => diff * f;
            public LocalOffset Div(LocalOffset diff, double f) => diff / f;
        }

    }
}