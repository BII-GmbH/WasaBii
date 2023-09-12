using System;
using NUnit.Framework;

namespace BII.WasaBii.Core.Tests {
    
    public class IntegralApproximationTests {

        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsConstant_ThenCorrect() {
            double Func(double _) => 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(Func, from, to, sections: 1);
            var expected = 5 * (to - from);
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsLinear_ThenCorrect() {
            double Func(double x) => x * 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(Func, from, to, sections: 1);
            var expected = 5 * (to*to - from*from) / 2d;
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsQuadratic_ThenCorrect() {
            double Func(double x) => 2 + x * 3 + x * x * 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(Func, from, to, sections: 2);
            var expected = 2 * (to - from) + 3 * (to*to - from*from) / 2d + 5 * (to*to*to - from*from*from) / 3d;
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsSine_ThenCorrect() {
            Func<double, double> func = Math.Sin;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 4);
            var expected = Math.Cos(-3) - Math.Cos(5);
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfGenericFloatFunc_WhenFunctionIsCos_ThenCorrect() {
            float Func(double x) => MathF.Cos((float)x);
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(Func, from, to, sections: 4, (a, b) => a+b, (a, f) => a * (float)f);
            var expected = Math.Sin(5) - Math.Sin(-3);
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void TrapezoidalApproximationOfDoubleFunc_WhenFunctionIsConstant_ThenCorrect() {
            double Func(double _) => 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.Trapezoidal(Func, from, to, samples: 2);
            var expected = 5 * (to - from);
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void TrapezoidalApproximationOfDoubleFunc_WhenFunctionIsLinear_ThenCorrect() {
            double Func(double x) => x * 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.Trapezoidal(Func, from, to, samples: 2);
            var expected = 5 * (to*to - from*from) / 2d;
            Assert.That(approx, Is.EqualTo(expected).Within(0.01f));
        }
        
        [Test]
        public void TrapezoidalApproximationOfDoubleFunc_WhenFunctionIsQuadratic_ThenCorrect() {
            double Func(double x) => 2 + x * 3 + x * x * 5;
            var from = -1;
            var to = 1;
            var approx = IntegralApproximation.Trapezoidal(Func, from, to, samples: 150);
            var expected = 2 * (to - from) + 3 * (to*to - from*from) / 2d + 5 * (to*to*to - from*from*from) / 3d;
            Assert.That(approx, Is.EqualTo(expected).Within(0.1f));
        }
        
        [Test]
        public void TrapezoidalApproximationOfDoubleFunc_WhenFunctionIsSine_ThenCorrect() {
            Func<double, double> func = Math.Sin;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.Trapezoidal(func, from, to, samples: 30);
            var expected = Math.Cos(-3) - Math.Cos(5);
            Assert.That(approx, Is.EqualTo(expected).Within(0.1f));
        }
        
        [Test]
        public void TrapezoidalApproximationOfGenericFloatFunc_WhenFunctionIsCos_ThenCorrect() {
            float Func(double x) => MathF.Cos((float)x);
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.Trapezoidal(Func, from, to, samples: 30, (a, b) => a+b, (a, f) => a * (float)f);
            var expected = Math.Sin(5) - Math.Sin(-3);
            Assert.That(approx, Is.EqualTo(expected).Within(0.1f));
        }
        
    }
    
}
