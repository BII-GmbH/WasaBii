﻿using BII.WasaBii.Units;
using NUnit.Framework;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {
    public class SplineLengthTests {
        
        [Test]
        public void LengthOfSplineSegment_WhenNormalizedNode_ThenReturnsCorrectly() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            var length = uut[SplineSegmentIndex.Zero].Length;
        
            Assert.That((double)length.AsMeters(), Is.EqualTo((double)SplineTestUtils.ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
        
        
        [Test]
        public void SplineLengthTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            var length = uut.Length();
        
            Assert.That((double)length.AsMeters(), Is.EqualTo((double)SplineTestUtils.ExampleCurvedSpline.ExpectedSplineLength.AsMeters()).Within(SplineLocationTolerance));
        }
    }
}