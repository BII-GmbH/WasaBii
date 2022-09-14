using System;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.Unity.Geometry;
using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;
using Range = BII.WasaBii.Core.Range;

namespace BII.WasaBii.Splines.Tests {

    public class PolynomialTests {

        private static readonly GlobalPosition a = GlobalPosition.FromGlobal(2, 8, 8);
        private static readonly GlobalOffset b = GlobalOffset.FromGlobal(4, 2, 0);
        private static readonly GlobalOffset c = GlobalOffset.FromGlobal(1, 3, 3);
        private static readonly GlobalOffset d = GlobalOffset.FromGlobal(7, 6, 9);
        private static readonly GlobalOffset e = GlobalOffset.FromGlobal(-1.0f / 12.0f, Mathf.PI, (float)Math.E);
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
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinear(t);
                var actual = linearPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateLinear_Derivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinearDerivative(t);
                var actual = linearPolynomial.EvaluateDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateLinear_SecondDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateLinearSecondDerivative(t);
                var actual = linearPolynomial.EvaluateSecondDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubic(t);
                var actual = cubicPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_Derivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 1);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_SecondDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicSecondDerivative(t);
                var actual = cubicPolynomial.EvaluateSecondDerivative(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_ThirdDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicThirdDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 3);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_FourthDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicFourthDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 4);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateCubic_FifthDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateCubicFifthDerivative(t);
                var actual = cubicPolynomial.EvaluateNthDerivative(t, 5);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
        
        [Test]
        public void EvaluateSixth() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixth(t);
                var actual = sixthOrderPolynomial.Evaluate(t);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }
  
        [Test]
        public void EvaluateSixth_SecondDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthSecondDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 2);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }

        [Test]
        public void EvaluateSixth_FourthDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthFourthDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 4);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }

        [Test]
        public void EvaluateSixth_SixthDerivative() {
            foreach (var t in Range.Sample01(count: 10, includeZero: true, includeOne: true)) {
                var expected = evaluateSixthSixthDerivative(t);
                var actual = sixthOrderPolynomial.EvaluateNthDerivative(t, 6);
                Assert.That(actual, Is.EqualTo(expected).Within(1E-7));
            }
        }

    }
}