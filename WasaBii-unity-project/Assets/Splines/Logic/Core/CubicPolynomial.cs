using System;
using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.CatmullRomSplines.Logic {
    public readonly struct CubicPolynomial<TPos, TDiff> 
        where TPos : struct 
        where TDiff : struct {
    
        /// Coefficients of the polynomial function 
        private readonly TDiff _a, _b, _c;
        private readonly TPos _d;

        internal readonly PositionOperations<TPos, TDiff> Ops;
        
        public CubicPolynomial(TDiff a, TDiff b, TDiff c, TPos d, PositionOperations<TPos, TDiff> ops) {
            this._a = a;
            this._b = b;
            this._c = c;
            this._d = d;
            this.Ops = ops;
        }

        public TPos Evaluate(double t) {
            LazyContract.Assert(t is >= 0 and <= 1, () => $"The parameter 't' must be between 0 and 1 but it was {t}");
            var tt = t * t;
            var ttt = tt * t;
            return Ops.Add(_d, Ops.Mul(_c, t), Ops.Mul(_b, tt), Ops.Mul(_a, ttt));
        }

        public TDiff EvaluateDerivative(double t) {
            LazyContract.Assert(t is >= 0 and <= 1, () => $"The parameter 't' must be between 0 and 1 but it was {t}");
            var tt = t * t;
            return Ops.Add(_c, Ops.Mul(_b, 2 * t), Ops.Mul(_a, 3 * tt));
        }

        public TDiff EvaluateSecondDerivative(double t) {
            LazyContract.Assert(t >= 0 && t <= 1, () => $"The parameter 't' must be between 0 and 1 but it was {t}");
            return Ops.Add(Ops.Mul(_b, 2), Ops.Mul(_a, 6 * t));
        }

        public double EvaluateClosestPointTo(TPos p, int iterations) {
            // Needed because "this" of structs cannot be captured by nested functions
            var copyOfThis = this;
            var ops = copyOfThis.Ops;

            double DistanceDerived(double t, TDiff diff, TDiff tan) => 
                2 * ops.Dot(tan, diff);

            double DistanceDerivedDerived(double t, TDiff diff, TDiff tan) => 
                2 * (ops.Dot(copyOfThis.EvaluateSecondDerivative(t), diff) + ops.Dot(tan, tan));

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
                var numerator = DistanceDerived(res, diff, tan);
                var denominator = DistanceDerivedDerived(res, diff, tan);
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

    public static class CubicPolynomial {
        
        public static CubicPolynomial<TPos, TDiff> FromCatmullRomSegment<TPos, TDiff>(CatmullRomSegment<TPos, TDiff> segment, float alpha) 
            where TPos : struct 
            where TDiff : struct {
            var p0 = segment.P0;
            var p1 = segment.P1;
            var p2 = segment.P2;
            var p3 = segment.P3;

            var ops = segment.Ops;

            double DTFor(TPos pos1, TPos pos2, double orWhenZero) =>
                Math.Pow(ops.Distance(pos1, pos2).AsMeters(), alpha)
                    .If(dt => dt < float.Epsilon, _ => orWhenZero);

            var dt1 = DTFor(p1, p2, orWhenZero: 1.0f);
            var dt0 = DTFor(p0, p1, orWhenZero: dt1);
            var dt2 = DTFor(p2, p3, orWhenZero: dt1);
            
            TDiff TFor(TPos pa, TPos pb, TPos pc, double dta, double dtb) =>
                ops.Mul(ops.Add(ops.Sub(ops.Div(ops.Sub(pb, pa), dta), ops.Div(ops.Sub(pc, pa), dta + dtb)), ops.Div(ops.Sub(pc, pb), dtb)), dt1);

            var t1 = TFor(p0, p1, p2, dt0, dt1);
            var t2 = TFor(p1, p2, p3, dt1, dt2);

            var poly = new CubicPolynomial<TPos, TDiff>(
                a: ops.Add(ops.Mul(ops.Sub(p1, p2), 2), t1, t2),
                b: ops.Sub(ops.Mul(ops.Sub(p2, p1), 3), ops.Mul(t1, 2), t2),
                c: t1,
                d: p1,
                ops
            );

            return poly;
        }

    }
}
