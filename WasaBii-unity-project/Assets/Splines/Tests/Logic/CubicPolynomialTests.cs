using NUnit.Framework;
using static BII.CatmullRomSplines.Tests.SplineTestUtils;

namespace BII.CatmullRomSplines.Tests {
    public class CubicPolynomialTests {
        
        [Test]
        public void Evaluate_WhenEvaluatingLinearSpline_ThenReturnsCorrectPositions() {

            var uut = ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.Evaluate(0), ExampleLinearSpline.Expected0Position);
            AssertVectorEquality(uut.Evaluate(0.5f), ExampleLinearSpline.Expected05Position);
            AssertVectorEquality(uut.Evaluate(1.0f), ExampleLinearSpline.Expected1Position);
        }
        
        [Test]
        public void EvaluateDerivative_WhenEvaluatingLinearSpline_ThenReturnsCorrectTangents() {

            var uut = ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateDerivative(0), ExampleLinearSpline.Expected0Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(0.5f), ExampleLinearSpline.Expected05Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(1.0f), ExampleLinearSpline.Expected1Tangent);
        }
        
        [Test]
        public void EvaluateSecondDerivative_WhenEvaluatingLinearSpline_ThenReturnsCorrectTangents() {

            var uut = ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateSecondDerivative(0), ExampleLinearSpline.Expected0Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(0.5f), ExampleLinearSpline.Expected05Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(1.0f), ExampleLinearSpline.Expected1Curvature);
        }
        
        [Test]
        public void Evaluate_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.Evaluate(0), ExampleCurvedSpline.Expected0Position);
            AssertVectorEquality(uut.Evaluate(0.5f), ExampleCurvedSpline.Expected05Position);
            AssertVectorEquality(uut.Evaluate(1.0f), ExampleCurvedSpline.Expected1Position);
        }
        
        [Test]
        public void EvaluateDerivative_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateDerivative(0), ExampleCurvedSpline.Expected0Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(0.5f), ExampleCurvedSpline.Expected05Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(1.0f), ExampleCurvedSpline.Expected1Tangent);
        }
        
        [Test]
        public void EvaluateSecondDerivative_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateSecondDerivative(0), ExampleCurvedSpline.Expected0Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(0.5f), ExampleCurvedSpline.Expected05Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(1.0f), ExampleCurvedSpline.Expected1Curvature);
        }
    }
}