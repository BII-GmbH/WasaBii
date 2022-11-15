using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

[assembly:InternalsVisibleTo("WasaBii.Splines.Editor.Tests")]

namespace BII.WasaBii.Splines.Maths {

    internal static class Polynomial {
        
        public static Polynomial<TPos, TDiff> Quadratic<TPos, TDiff> (TPos a, TDiff b, TDiff c, GeometricOperations<TPos, TDiff> ops)
        where TPos : struct 
        where TDiff : struct =>
            new(ops, a, b, c);

        public static Polynomial<TPos, TDiff> Cubic<TPos, TDiff> (TPos a, TDiff b, TDiff c, TDiff d, GeometricOperations<TPos, TDiff> ops)
        where TPos : struct 
        where TDiff : struct =>
            new(ops, a, b, c, d);

    }
    
    internal readonly struct Polynomial<TPos, TDiff> 
        where TPos : struct 
        where TDiff : struct {
    
        /// All coefficients of the polynomial function except the first. Eg. a cubic polynomial has
        /// three <see cref="TailC"/> entries and is calculated in the form of: A + Bt + Ct² + Dt³
        /// where A = <see cref="FirstC"/>, [B, C, D] = <see cref="TailC"/>
        private readonly IReadOnlyList<TDiff> TailC;
        private readonly TPos FirstC;

        internal readonly GeometricOperations<TPos, TDiff> Ops;

        public Length ArcLength => SplineSegmentUtils.SimpsonsLengthOf(this);
        
        public Polynomial(GeometricOperations<TPos, TDiff> ops, TPos firstC, IReadOnlyList<TDiff> tailC) {
            this.FirstC = firstC;
            this.TailC = tailC;
            this.Ops = ops;
        }

        public Polynomial(GeometricOperations<TPos, TDiff> ops, TPos firstC, params TDiff[] tailC) {
            this.FirstC = firstC;
            this.TailC = tailC;
            this.Ops = ops;
        }

        public TPos Evaluate(double t) {
            if(!t.IsInsideInterval(0, 1, threshold: 0.001)) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var ops = Ops;
            return TailC.Aggregate(
                seed: (res: FirstC, t),
                (acc, diff) => (ops.Add(acc.res, ops.Mul(diff, acc.t)), acc.t * t)
            ).res;
        }

        public TDiff EvaluateDerivative(double t) {
            if(!t.IsInsideInterval(0, 1, threshold: 0.001)) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var ops = Ops;
            return TailC.ZipWithIndices().Aggregate(
                seed: (res: Ops.ZeroDiff, t: 1.0),
                (acc, diff) => (
                    ops.Add(
                        acc.res,
                        ops.Mul(diff.item, acc.t * (diff.index + 1))
                    ), 
                    acc.t * t
                )
            ).res;
        }

        public TDiff EvaluateSecondDerivative(double t) {
            if(!t.IsInsideInterval(0, 1, threshold: 0.001)) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var ops = Ops;
            return TailC.ZipWithIndices().Skip(1).Aggregate(
                seed: (res: Ops.ZeroDiff, t: 1.0),
                (acc, diff) => (
                    ops.Add(
                        acc.res,
                        ops.Mul(diff.item, acc.t * diff.index * (diff.index + 1))
                    ), 
                    acc.t * t
                )
            ).res;
        }

        public TDiff EvaluateNthDerivative(double t, int n) {
            if(!t.IsInsideInterval(0, 1, threshold: 0.001)) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var ops = Ops;
            var factorials = new int[TailC.Count + 1];
            factorials[0] = 1;
            for (var i = 1; i <= TailC.Count; i++) factorials[i] = factorials[i - 1] * i;
            return TailC.Skip(n - 1).ZipWithIndices().Aggregate(
                seed: (res: Ops.ZeroDiff, t: 1.0),
                (acc, diff) => (
                    ops.Add(
                        acc.res,
                        // ReSharper disable once PossibleLossOfFraction
                        ops.Mul(diff.item, acc.t * (factorials[diff.index + n] / factorials[diff.index]))
                    ), 
                    acc.t * t
                )
            ).res;
        }

        /// <returns>The progress along the polynomial at which the interpolated position is closest to <see cref="p"/></returns>
        public double EvaluateClosestPointTo(TPos p, int iterations) {
            // Needed because "this" of structs cannot be captured by nested functions
            var copyOfThis = this;
            var ops = copyOfThis.Ops;

            double SqrDistanceFactorDerived(double t, TDiff diff, TDiff tan) => 
                ops.Dot(tan, diff);

            double SqrDistanceFactorTwiceDerived(double t, TDiff diff, TDiff tan) => 
                ops.Dot(copyOfThis.EvaluateSecondDerivative(t), diff) + ops.Dot(tan, tan);

            // We describe the squared distance from the queried position p to the spline as the distance function d(t, qp).
            // (we use the squared distance, instead of the normal distance,
            // to avoid taking the root, which is unnecessary)
            // Given:
            // - t is a location on the spline
            // - qp is the queried position and
            // - pos(t) is the position on the spline at the given location
            // Follows: d(t, qp) = (pos(t).x - qp.x) * (pos(t).y - qp.y) * (pos(t).z - qp.z)
            //
            // By finding a low point of that function (e.g. minimum distance),
            // we find the closest location on the spline.
            // This low point is found by using newton's method (the loop below).
            // The first derivative and second derivative needed for this are found above. 
            //
            // This algorithm is based on the following:
            // https://www.tinaja.com/glib/cmindist.pdf
            // https://en.wikipedia.org/wiki/Newton%27s_method
            var res = 0.5;
            for (var i = 0; i < iterations; ++i) {
                var pos = copyOfThis.Evaluate(res);
                var tan = copyOfThis.EvaluateDerivative(res);
                var diff = ops.Sub(pos, p);
                var numerator = SqrDistanceFactorDerived(res, diff, tan);
                var denominator = SqrDistanceFactorTwiceDerived(res, diff, tan);
                if (Math.Abs(denominator) < float.Epsilon)
                    return res;
                res -= numerator / denominator;
                
                // res sometimes goes very slightly below 0 or very slightly above 1.
                // Since this would trigger a Debug.Assertion elsewhere,
                // we ensure that the value is clamped.
                res = Math.Clamp(res, 0, 1);
            }

            return res;
        }
    }
}
