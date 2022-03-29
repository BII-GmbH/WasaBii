using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.WasaBii.CatmullRomSplines {
    
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
        
        Length Distance(TPos p0, TPos p1);
        TDiff Sub(TPos p0, TPos p1);
        TPos Sub(TPos p, TDiff d);
        TDiff Sub(TDiff d1, TDiff d2);
        TPos Add(TPos d1, TDiff d2);
        TDiff Add(TDiff d1, TDiff d2);
        TDiff Div(TDiff diff, double d);
        TDiff Mul(TDiff diff, double f);
        double Dot(TDiff a, TDiff b);
    }

    public static class PositionOperationsExtensions {

        public static TPos Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TPos p, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(ops.Add(p, d1), d2), d3);

        public static TPos Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TPos p, TDiff d1, TDiff d2) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(p, d1), d2);

        public static TDiff Add<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Add(ops.Add(d1, d2), d3);

        public static TDiff Sub<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TDiff d1, TDiff d2, TDiff d3) 
            where TPos : struct where TDiff : struct => ops.Sub(ops.Sub(d1, d2), d3);

        public static TDiff Mul<TPos, TDiff>(this PositionOperations<TPos, TDiff> ops, TDiff diff, double f1, double f2) 
            where TPos : struct where TDiff : struct => ops.Mul(ops.Mul(diff, f1), f2);

    }
    
}