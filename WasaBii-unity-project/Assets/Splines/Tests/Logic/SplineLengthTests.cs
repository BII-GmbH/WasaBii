using BII.WasaBii.Units;
using NUnit.Framework;
using static BII.WasaBii.CatmullRomSplines.Tests.SplineTestUtils;

namespace BII.WasaBii.CatmullRomSplines.Tests {
    public class SplineLengthTests {
        
        [Test]
        public void LengthOfSplineSegment_WhenNormalizedNode_ThenReturnsCorrectly() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = uut[SplineSegmentIndex.Zero].Length();
        
            Assert.That((double)length.AsMeters(), Is.EqualTo((double)ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
        
        
        [Test]
        public void SplineLengthTest() {
            var uut = ExampleCurvedSpline.Spline;
        
            var length = uut.Length();
        
            Assert.That((double)length.AsMeters(), Is.EqualTo((double)ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
    }
}