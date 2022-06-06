using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    /// Our splines are representation-agnostic, meaning that you can define a spline using
    /// unity vectors, system vectors, 3D vectors, 2D vectors, 10D vectors, `LocalPosition`s,
    /// `GlobalPosition`s, cats, lazy vectors or even <see cref="BII.WasaBii.Core.Nothing"/>,
    /// as long as you provide an implementation for all the necessary geometric operations.
    /// Since C# does not have proper support for type classes 😢, this means passing an
    /// implementation of this interface wherever necessary.
    [MustBeImmutable][MustBeSerializable]
    public interface PositionOperations<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        [Pure] TPos Add(TPos d1, TDiff d2);
        [Pure] TDiff Add(TDiff d1, TDiff d2);
        [Pure] TDiff Sub(TPos p0, TPos p1);
        [Pure] TDiff Mul(TDiff diff, double f);
        [Pure] double Dot(TDiff a, TDiff b);
        
        // These can be implemented using the above, but explicitly
        // providing efficient implementations could improve performance
        [Pure] TPos Sub(TPos p, TDiff d) => Add(p, Mul(d, -1));
        [Pure] TDiff Sub(TDiff d1, TDiff d2) => Add(d1, Mul(d2, -1));
        [Pure] TDiff Div(TDiff diff, double d) => Mul(diff, 1 / d);
        [Pure] Length Distance(TPos p0, TPos p1) {
            var diff = Sub(p1, p0);
            return Math.Sqrt(Dot(diff, diff)).Meters();
        }
        [Pure] TPos Lerp(TPos from, TPos to, double t) => Add(from, Mul(Sub(to, from), t));
    }

    public static class PositionOperationsExtensions {

        [Pure]
        public static TPos Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TPos p, TDiff d1, TDiff d2) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(p, d1), d2);

        [Pure]
        public static TPos Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TPos p, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(p, d1, d2), d3);

        [Pure]
        public static TDiff Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(d1, d2), d3);

        [Pure]
        public static TDiff Sub<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Sub(ops.Sub(d1, d2), d3);

    }
    
}