using BII.WasaBii.Core;
using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {
    public class ClosestOnSplineTests {
      
        [Test]
        public void QueryGreedyClosestPositionOnSplineTo_WhenInvalidSpline_ThenReturnsNone() {
            var invalidSpline = SplineTestUtils.ExampleInvalidSpline.Spline;
            Assert.That(invalidSpline.QueryClosestPositionOnSplineTo(Vector3.zero), Is.EqualTo(Option<ClosestOnSplineQueryResult<Vector3, Vector3>>.None));
        }
        
        [Test]
        public void QueryGreedyClosestPositionOnSplineTo_WhenEquidistantNodes_ThenReturnsCorrectLocationAndDistance() {
            var uut = UnitySpline.FromHandlesIncludingMargin(new []{
                new Vector3(-1, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(2, 0, 0),
                new Vector3(3, 0, 0),
                new Vector3(4, 0, 0)
            });
        
            for (var xCoord = -2f; xCoord < 5; xCoord += 0.1f) {
                var position = new Vector3(xCoord, -1, 0);
                var queryResult =
                    uut.QueryClosestPositionOnSplineToOrThrow(position);
                var expectedLocationOnSpline = Mathf.Clamp(xCoord, 0, 3);
                var expectedPositionOnSpline = new Vector3(Mathf.Clamp(xCoord, 0, 3), 0, 0);
                var expectedPositionToNodeDistance = (double) Vector3.Distance(
                    position,
                    new Vector3(expectedLocationOnSpline, 0, 0)
                );
        
                Assert.That(
                    queryResult.Location.Value.SiValue,
                    Is.EqualTo(expectedLocationOnSpline).Within(0.01d),
                    $"The actual location {queryResult.Location} didn't match the expected {expectedLocationOnSpline} for Position {position}"
                );
                Assert.That(
                    queryResult.Distance.SiValue,
                    Is.EqualTo(expectedPositionToNodeDistance).Within(0.01d),
                    $"The actual distance {queryResult.Distance} didn't match the expected {expectedPositionToNodeDistance} for Position {position}"
                );
                AssertVectorEquality(queryResult.ClosestOnSpline, expectedPositionOnSpline);
            }
        }
    }
}