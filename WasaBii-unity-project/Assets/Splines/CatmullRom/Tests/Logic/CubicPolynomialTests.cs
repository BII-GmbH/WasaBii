using NUnit.Framework;
using static BII.WasaBii.Splines.CatmullRom.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.CatmullRom.Tests {
    
    public class CubicPolynomialTests {
        
        [Test]
        public void Evaluate_WhenEvaluatingLinearSpline_ThenReturnsCorrectPositions() {

            var uut = SplineTestUtils.ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.Evaluate(0), SplineTestUtils.ExampleLinearSpline.Expected0Position);
            AssertVectorEquality(uut.Evaluate(0.5f), SplineTestUtils.ExampleLinearSpline.Expected05Position);
            AssertVectorEquality(uut.Evaluate(1.0f), SplineTestUtils.ExampleLinearSpline.Expected1Position);
        }
        
        [Test]
        public void EvaluateDerivative_WhenEvaluatingLinearSpline_ThenReturnsCorrectTangents() {

            var uut = SplineTestUtils.ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateDerivative(0), SplineTestUtils.ExampleLinearSpline.Expected0Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(0.5f), SplineTestUtils.ExampleLinearSpline.Expected05Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(1.0f), SplineTestUtils.ExampleLinearSpline.Expected1Tangent);
        }
        
        [Test]
        public void EvaluateSecondDerivative_WhenEvaluatingLinearSpline_ThenReturnsCorrectTangents() {

            var uut = SplineTestUtils.ExampleLinearSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateSecondDerivative(0), SplineTestUtils.ExampleLinearSpline.Expected0Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(0.5f), SplineTestUtils.ExampleLinearSpline.Expected05Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(1.0f), SplineTestUtils.ExampleLinearSpline.Expected1Curvature);
        }
        
        [Test]
        public void Evaluate_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = SplineTestUtils.ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.Evaluate(0), SplineTestUtils.ExampleCurvedSpline.Expected0Position);
            AssertVectorEquality(uut.Evaluate(0.5f), SplineTestUtils.ExampleCurvedSpline.Expected05Position);
            AssertVectorEquality(uut.Evaluate(1.0f), SplineTestUtils.ExampleCurvedSpline.Expected1Position);
        }
        
        [Test]
        public void EvaluateDerivative_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = SplineTestUtils.ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateDerivative(0), SplineTestUtils.ExampleCurvedSpline.Expected0Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(0.5f), SplineTestUtils.ExampleCurvedSpline.Expected05Tangent);
            AssertVectorEquality(uut.EvaluateDerivative(1.0f), SplineTestUtils.ExampleCurvedSpline.Expected1Tangent);
        }
        
        [Test]
        public void EvaluateSecondDerivative_WhenEvaluatingCurvedSpline_ThenReturnsCorrectPositions() {

            var uut = SplineTestUtils.ExampleCurvedSpline.Polynomial;
            
            AssertVectorEquality(uut.EvaluateSecondDerivative(0), SplineTestUtils.ExampleCurvedSpline.Expected0Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(0.5f), SplineTestUtils.ExampleCurvedSpline.Expected05Curvature);
            AssertVectorEquality(uut.EvaluateSecondDerivative(1.0f), SplineTestUtils.ExampleCurvedSpline.Expected1Curvature);
        }
    }
}