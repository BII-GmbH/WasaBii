using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {

    public static class BezierSegment {
        
        [Pure]
        public static BezierSegment<TPos, TDiff>.Cubic Cubic<TPos, TDiff>(
            TPos p0, TPos p1, TPos p2, TPos p3,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => new(p0, p1, p2, p3, ops);

        [Pure]
        public static BezierSegment<TPos, TDiff>.Cubic Cubic<TPos, TDiff>(
            TPos start, TDiff startVelocity, TPos end, TDiff endVelocity,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => Cubic(
                p0: start, 
                p1: ops.Add(start, ops.Div(startVelocity, 3)),
                p2: ops.Sub(end, ops.Div(endVelocity, 3)), 
                p3: end, 
                ops
            );

        [Pure]
        public static BezierSegment<TPos, TDiff>.Quadratic Quadratic<TPos, TDiff>(
            TPos p0, TPos p1, TPos p2,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => new(p0, p1, p2, ops);

    }
    
    [MustBeSerializable]
    public abstract record BezierSegment<TPos, TDiff> where TPos : struct where TDiff : struct {
        
        public abstract GeometricOperations<TPos, TDiff> Ops { get; init; }
        public abstract TPos Start { get; }
        public abstract TPos End { get; }
        public abstract TDiff StartVelocity { get; }
        public abstract TDiff EndVelocity { get; }

        [NonSerialized] public readonly Lazy<Length> Length;
        
        private BezierSegment() => Length = new Lazy<Length>(() => ToPolynomial().ArcLength);

        [Pure] internal abstract Polynomial<TPos, TDiff> ToPolynomial();

        [Pure] public SplineSegment<TPos, TDiff> ToSplineSegment() 
            => new(ToPolynomial(), Length);
        
        [Pure] public abstract BezierSegment<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : struct where TDiffNew : struct;

        [Pure] public BezierSegment<TPos, TDiff> Map(Func<TPos, TPos> positionMapping) => Map(positionMapping, Ops);
        
        public abstract BezierSegment<TPos, TDiff> Reversed { get; }

        /// Describes the area between two spline handles (p0 and p3), 
        /// with the supporting handles p1 and p2
        [MustBeSerializable]
        public sealed record Cubic(TPos P0, TPos P1, TPos P2, TPos P3, GeometricOperations<TPos, TDiff> Ops) : BezierSegment<TPos, TDiff> {
        
            public override TPos Start => P0;
            public override TPos End => P3;

            public override TDiff StartVelocity => Ops.Mul(Ops.Sub(P1, P0), 3);
            public override TDiff EndVelocity => Ops.Mul(Ops.Sub(P3, P2), 3);

            public override BezierSegment<TPos, TDiff> Reversed => new Cubic(P3, P2, P1, P0, Ops);
            
            [Pure]
            internal override Polynomial<TPos, TDiff> ToPolynomial() {
                var startVelocity = StartVelocity;
                
                var a = P0;
                var b = startVelocity;
                var c = Ops.Sub(Ops.Mul(Ops.Sub(P2, P1), 3), b);
                var d = Ops.Add(Ops.Mul(Ops.Sub(P0, P3), 2), startVelocity, EndVelocity);
                
                return Polynomial.Cubic(a, b, c, d, Ops);
            }

            public override BezierSegment<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
                Func<TPos, TPosNew> mapping, GeometricOperations<TPosNew, TDiffNew> newOps
            ) => BezierSegment.Cubic(mapping(P0), mapping(P1), mapping(P2), mapping(P3), newOps);

        }

        /// Describes the area between two spline handles (p0 and p2), 
        /// with the supporting handle p1
        [MustBeSerializable]
        public sealed record Quadratic(TPos P0, TPos P1, TPos P2, GeometricOperations<TPos, TDiff> Ops) : BezierSegment<TPos, TDiff> {
        
            public override TPos Start => P0;
            public override TPos End => P2;

            public override TDiff StartVelocity => Ops.Mul(Ops.Sub(P1, P0), 2);
            public override TDiff EndVelocity => Ops.Mul(Ops.Sub(P2, P1), 2);

            public override BezierSegment<TPos, TDiff> Reversed => new Quadratic(P2, P1, P0, Ops);

            [Pure]
            internal override Polynomial<TPos, TDiff> ToPolynomial() {
                var startVelocity = StartVelocity;
                
                var a = P0;
                var b = startVelocity;
                var c = Ops.Mul(Ops.Sub(EndVelocity, startVelocity), 0.5);
                
                return Polynomial.Quadratic(a, b, c, Ops);
            }
            
            public override BezierSegment<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
                Func<TPos, TPosNew> mapping, GeometricOperations<TPosNew, TDiffNew> newOps
            ) => BezierSegment.Quadratic(mapping(P0), mapping(P1), mapping(P2), newOps);

        }

    }

}
