using BII.WasaBii.UnitSystem;
using NUnit.Framework;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {
    public class SplineLengthTests {
        
        [Test]
        public void LengthOfSplineSegment_WhenNormalizedNode_ThenReturnsCorrectly() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = uut[SplineSegmentIndex.Zero].Length();
        
            Assert.That(length.AsMeters(), Is.EqualTo(ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
        
        
        [Test]
        public void SplineLengthTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            var length = uut.Length();
        
            Assert.That(length.AsMeters(), Is.EqualTo(ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
    }
}