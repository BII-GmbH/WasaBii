using BII.WasaBii.UnitSystem;
using NUnit.Framework;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {
    public class SplineLengthTests {
        
        [Test]
        public void LengthOfSplineSegment_WhenNormalizedNode_ThenReturnsCorrectly() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = uut[SplineSegmentIndex.Zero].Length;
        
            Assert.That(length.AsMeters(), Is.EqualTo(ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
        
        [Test]
        public void LengthOfSplineSegment_WithTrapezoidalApproximation_IsCorrect() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = SplineSegmentUtils.TrapezoidalLengthOf(uut[SplineSegmentIndex.Zero].Polynomial);
        
            Assert.That(length.AsMeters(), Is.EqualTo(ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
        
        [Test]
        public void LengthOfSplineSegment_WithSimpsonsApproximation_IsCorrect() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = SplineSegmentUtils.SimpsonsLengthOf(uut[SplineSegmentIndex.Zero].Polynomial);
        
            Assert.That(length.AsMeters(), Is.EqualTo(ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }

    }
}