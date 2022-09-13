using System;
using BII.WasaBii.Core;
using NUnit.Framework;

namespace Core.Tests {
    
    public class IntegralApproximationTests {

        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsConstant_ThenCorrect() {
            Func<double, double> func = _ => 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 1);
            var expected = 5 * (to - from);
            Assert.That(approx, Is.InRange(expected - 0.01f, expected + 0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsLinear_ThenCorrect() {
            Func<double, double> func = x => x * 5;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 1);
            var expected = 5 * (to*to - from*from) / 2d;
            Assert.That(approx, Is.InRange(expected - 0.01f, expected + 0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsQuadratic_ThenCorrect() {
            Func<double, double> func = x => x * x * 5 + x * 3;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 2);
            var expected = 3 * (to*to - from*from) / 2d + 5 * (to*to*to - from*from*from) / 3d;
            Assert.That(approx, Is.InRange(expected - 0.01f, expected + 0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfDoubleFunc_WhenFunctionIsSine_ThenCorrect() {
            Func<double, double> func = Math.Sin;
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 4);
            var expected = Math.Cos(-3) - Math.Cos(5);
            Assert.That(approx, Is.InRange(expected - 0.01f, expected + 0.01f));
        }
        
        [Test]
        public void SimpsonApproximationOfGenericFloatFunc_WhenFunctionIsCos_ThenCorrect() {
            Func<double, float> func = x => MathF.Cos((float)x);
            var from = -3;
            var to = 5;
            var approx = IntegralApproximation.SimpsonsRule(func, from, to, sections: 4, (a, b) => a+b, (a, f) => a * (float)f);
            var expected = Math.Sin(5) - Math.Sin(-3);
            Assert.That(approx, Is.InRange(expected - 0.01f, expected + 0.01f));
        }
        
    }
    
}
