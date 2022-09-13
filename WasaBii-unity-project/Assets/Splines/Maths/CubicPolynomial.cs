using System;

namespace BII.WasaBii.Splines.Maths {
    internal readonly struct CubicPolynomial<TPos, TDiff> 
        where TPos : struct 
        where TDiff : struct {
    
        /// Coefficients of the polynomial function: At³ + Bt² + Ct + D
        public readonly TDiff A, B, C;
        public readonly TPos D;

        internal readonly GeometricOperations<TPos, TDiff> Ops;
        
        public CubicPolynomial(TDiff a, TDiff b, TDiff c, TPos d, GeometricOperations<TPos, TDiff> ops) {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
            this.Ops = ops;
        }

        public TPos Evaluate(double t) {
            if(t is < 0 or > 1) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var tt = t * t;
            var ttt = tt * t;
            return Ops.Add(D, Ops.Mul(C, t), Ops.Mul(B, tt), Ops.Mul(A, ttt));
        }

        public TDiff EvaluateDerivative(double t) {
            if(t is < 0 or > 1) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            var tt = t * t;
            return Ops.Add(C, Ops.Mul(B, 2 * t), Ops.Mul(A, 3 * tt));
        }

        public TDiff EvaluateSecondDerivative(double t) {
            if(t is < 0 or > 1) throw new ArgumentException($"The parameter 't' must be between 0 and 1 but it was {t}");
            return Ops.Add(Ops.Mul(B, 2), Ops.Mul(A, 6 * t));
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
                // Since this would trigger a Contract Assertion elsewhere,
                // we ensure that the value is clamped.
                res = Math.Clamp(res, 0, 1);
            }

            return res;
        }
    }
}
