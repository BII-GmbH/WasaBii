using System;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Unity.Geometry;
using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.Splines.Tests {

    public class PolynomialTests {

        private static readonly GlobalPosition a = GlobalPosition.FromGlobal(2, 8, 8);
        private static readonly GlobalOffset b = GlobalOffset.FromGlobal(4, 2, 0);
        private static readonly GlobalOffset c = GlobalOffset.FromGlobal(1, 3, 3);
        private static readonly GlobalOffset d = GlobalOffset.FromGlobal(7, 6, 9);
        private static readonly GlobalOffset e = GlobalOffset.FromGlobal(-1.0f / 12.0f, 2 * Mathf.PI, (float)Math.E);
        private static readonly GlobalOffset f = GlobalOffset.FromGlobal(1.618f, 2.414f, 3.303f);

        private static readonly Polynomial<GlobalPosition, GlobalOffset> linearPolynomial = new(
            GlobalSpline.GeometricOperations.Instance,
            a, b
        );

        private static readonly Polynomial<GlobalPosition, GlobalOffset> cubicPolynomial = Polynomial.Cubic(
            a, b, c, d, GlobalSpline.GeometricOperations.Instance
        );
        
        private static readonly Polynomial<GlobalPosition, GlobalOffset> sixthOrderPolynomial = new(
            GlobalSpline.GeometricOperations.Instance,
            a, b, c, d, e, f
        );

        private static GlobalPosition evaluateLinear(double t) => a + t * b;
        private static GlobalOffset evaluateLinearDerivative(double t) => b;
        private static GlobalOffset evaluateLinearSecondDerivative(double t) => GlobalOffset.Zero;
        
        private static GlobalPosition evaluateCubic(double t) => a + t * b + t*t * c + t*t*t * d;
        private static GlobalOffset evaluateCubicDerivative(double t) => b + 2*t * c + 3*t*t * d;
        private static GlobalOffset evaluateCubicSecondDerivative(double t) => 2 * c + 6*t * d;
        private static GlobalOffset evaluateCubicThirdDerivative(double t) => 6 * d;
        private static GlobalOffset evaluateCubicFourthDerivative(double t) => GlobalOffset.Zero;
        private static GlobalOffset evaluateCubicFifthDerivative(double t) => GlobalOffset.Zero;
        
        private static GlobalPosition evaluateSixth(double t) => a + t * b + t*t * c + t*t*t * d + t*t*t*t * e + t*t*t*t*t * f;
        private static GlobalOffset evaluateSixthSecondDerivative(double t) => 2 * c + 6*t * d + 12*t*t * e + 20*t*t*t * f;
        private static GlobalOffset evaluateSixthFourthDerivative(double t) => 24 * e + 120*t * f;
        private static GlobalOffset evaluateSixthSixthDerivative(double t) => GlobalOffset.Zero;

        [Test]
        public void EvaluateLinear() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinear(t);
                var actual = linearPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateLinear_Derivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinearDerivative(t);
                var actual = linearPolynomial.EvaluateDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateLinear_SecondDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinearSecondDerivative(t);
                var actual = linearPolynomial.EvaluateSecondDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubic(t);
                var actual = cubicPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_Derivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 1);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_SecondDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicSecondDerivative(t);
                var actual = cubicPolynomial.EvaluateSecondDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_ThirdDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicThirdDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 3);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_FourthDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicFourthDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 4);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_FifthDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicFifthDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 5);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateSixth() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixth(t);
                var actual = sixthOrderPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
  
        [Test]
        public void EvaluateSixth_SecondDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthSecondDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 2);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }

        [Test]
        public void EvaluateSixth_FourthDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthFourthDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 4);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }

        [Test]
        public void EvaluateSixth_SixthDerivative() {
            foreach (var t in SampleRange.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthSixthDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 6);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        // Note DS: There is no simple way to (de-) normalize the location on any spline as it
        // involves integrating the magnitude of the velocity, so we must approximate it. However,
        // we can calculate it exactly for a specific type of spline: monotone rising (between 0 and 1) 1D splines.
        // Their velocity is already 1D and positive, so its magnitude is itself, which means
        // that its anti-derivative is simply the polynomial evaluation (ignoring the constant bias, which cancels out).
        // Hence, we only test against those, as we can validate the result there.

        private sealed class OneDimensionalOps : GeometricOperations<double, double> {
            public double Add(double a, double b) => a + b;
            public double Sub(double a, double b) => a - b;
            public double Dot(double a, double b) => a * b;
            public double Mul(double a, double b) => a * b;
            public double ZeroDiff => 0;
        }

        private const int normalizationTestSampleCount = 20;
        
        [Test]
        public void DeNormalizeMonotoneRisingCubic1DSplineLocations() {
            
            // Positive parameters ensure a positive derivative (for positive t values) and thus a monotone rising polynomial.
            var oneDimensionalPolynomial = Polynomial.Cubic(1, 3, 3, 7, new OneDimensionalOps());

            foreach (var t in SampleRange.Sample01(normalizationTestSampleCount, includeZero: true, includeOne: true)) {
                var expected = oneDimensionalPolynomial.Evaluate(t) - oneDimensionalPolynomial.Evaluate(0);
                var actual = oneDimensionalPolynomial.ProgressToLength(t).AsMeters();
                Assert.That(actual, Is.EqualTo(expected).Within(1E-3));
            }
        }

        [Test]
        public void NormalizeMonotoneRisingQuadratic1DSplineLocations() {
            
            // Calculating the t values for a given length is difficult, so we only use a quadratic polynomial.
            var p0 = 2;
            var p1 = 8;
            var p2 = 8;
            var polynomial = new Polynomial<double, double>(new OneDimensionalOps(), p0, p1, p2);

            // length(t) is p1*t + p2*t²
            // thus, 0 == p2*t² + p1*t - length
            // abc formula (taking the positive t value) yields
            // (-p1 + sqrt(p1² + 4*p2*length)) / (2*p2)
            double tForLength(Length length) {
                var a = p2;
                var b = p1;
                var c = -length.AsMeters();
                return (Math.Sqrt(b*b - 4*a*c) - b) / (2 * a);
            }

            foreach (var l in SampleRange.From(Length.Zero, inclusive: true)
                .To(polynomial.ArcLength, inclusive: true)
                .Sample(normalizationTestSampleCount, (from, to, p) => Units.Lerp(from, to, p))) {
                var expected = tForLength(l);
                var actual = polynomial.LengthToProgress(l);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-2));
            }
        }

    }
}