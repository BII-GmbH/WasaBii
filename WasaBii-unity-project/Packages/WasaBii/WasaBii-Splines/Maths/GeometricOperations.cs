using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Maths {
    
    /// <summary>
    /// Our splines are representation-agnostic, meaning that you can define a spline using
    /// unity vectors, system vectors, 3D vectors, 2D vectors, 10D vectors, `LocalPosition`s,
    /// `GlobalPosition`s, cats, lazy vectors, GPU buffers or even <see cref="BII.WasaBii.Core.Nothing"/>,
    /// as long as you provide an implementation for all the necessary geometric operations.
    /// Since C# does not have proper support for type classes, this means passing an
    /// implementation of this interface wherever necessary.
    /// </summary>
    [MustBeImmutable]
    public interface GeometricOperations<TPos, TDiff, TTime, TVel>
        where TPos : unmanaged 
        where TDiff : unmanaged 
        where TTime : unmanaged 
        where TVel : unmanaged {
        
        [Pure] TPos Add(TPos d1, TDiff d2);
        [Pure] TDiff Add(TDiff d1, TDiff d2);
        [Pure] TDiff Sub(TPos p0, TPos p1);
        [Pure] TDiff Mul(TDiff diff, double f);
        [Pure] double Dot(TDiff a, TDiff b);
        
        // These can be implemented using the above, but explicitly
        // providing efficient implementations could improve performance
        [Pure] TPos Sub(TPos p, TDiff d) => Add(p, Mul(d, -1));
        [Pure] TDiff Sub(TDiff d1, TDiff d2) => Add(d1, Mul(d2, -1));
        [Pure] TDiff Div(TDiff diff, double d) => d.IsNearly(1) ? diff : Mul(diff, 1 / d);
        [Pure] Length Distance(TPos p0, TPos p1) {
            var diff = Sub(p1, p0);
            return Math.Sqrt(Dot(diff, diff)).Meters();
        }
        [Pure] TPos Lerp(TPos from, TPos to, double t) => Add(from, Mul(Sub(to, from), t));
        [Pure] TDiff Lerp(TDiff from, TDiff to, double t) => Add(from, Mul(Sub(to, from), t));
        
        [Pure] TDiff ZeroDiff { get; }
        [Pure] TVel ZeroVel { get; }
        [Pure] TTime ZeroTime { get; }

        [Pure] double Div(TTime a, TTime b);

        [Pure] TDiff Mul(TVel v, TTime t);
        [Pure] TVel Div(TDiff d, TTime t);
        [Pure] TTime Add(TTime a, TTime b);
        [Pure] TTime Sub(TTime a, TTime b);
        [Pure] TTime Mul(TTime a, double b);

    }

    [MustBeImmutable]
    public interface ScalarTimeGeometricOperations<TPos, TDiff> : GeometricOperations<TPos, TDiff, double, TDiff>
    where TPos : unmanaged where TDiff : unmanaged
    {
        TDiff GeometricOperations<TPos, TDiff, double, TDiff>.ZeroVel => ZeroDiff;

        double GeometricOperations<TPos, TDiff, double, TDiff>.ZeroTime => 0;

        double GeometricOperations<TPos, TDiff, double, TDiff>.Div(double a, double b) => a / b;
        double GeometricOperations<TPos, TDiff, double, TDiff>.Add(double a, double b) => a + b;
        double GeometricOperations<TPos, TDiff, double, TDiff>.Sub(double a, double b) => a - b;
        double GeometricOperations<TPos, TDiff, double, TDiff>.Mul(double a, double b) => a * b;
    }
    
}