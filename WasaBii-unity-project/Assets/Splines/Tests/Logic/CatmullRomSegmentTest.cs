using BII.CatmullRomSplines.Logic;
using NUnit.Framework;
using UnityEngine;
using static BII.CatmullRomSplines.Logic.CatmullRomSegment;
using static BII.CatmullRomSplines.Tests.SplineTestUtils;

namespace BII.CatmullRomSplines.Tests {
    public class CatmullRomSegmentTest {
        private void assertExistsAndEquals(
            CatmullRomSegment? segment,
            Vector3 expectedP0,
            Vector3 expectedP1,
            Vector3 expectedP2,
            Vector3 expectedP3
        ) {
            Assert.That(segment.HasValue);
            Assert.That(segment.Value.P0, Is.EqualTo(expectedP0));
            Assert.That(segment.Value.P1, Is.EqualTo(expectedP1));
            Assert.That(segment.Value.P2, Is.EqualTo(expectedP2));
            Assert.That(segment.Value.P3, Is.EqualTo(expectedP3));
        }

        private void assertExistsAndEquals(double? actualLocation, double expectedLocation) {
            Assert.That(actualLocation.HasValue);
            Assert.That(actualLocation.Value, Is.EqualTo(expectedLocation));
        }

        [Test]
        public void WhenLocationLessThanZero_ThenReturnNull() {
            var queryResult = CatmullRomSegmentAt(ExampleLinearSpline.Spline, NormalizedSplineLocation.From(-1));

            Assert.That(queryResult, Is.EqualTo(null));
        }

        [Test]
        public void WhenSplineNull_ThenThrows() {
            Assert.That(
                () => CatmullRomSegmentAt(spline: null, NormalizedSplineLocation.Zero),
                Throws.ArgumentNullException
            );
        }

        [Test]
        public void WhenInvalidSpline_ThenThrows() {
            var spline = ExampleInvalidSpline.Spline;

            Assert.That(() =>  CatmullRomSegmentAt(spline, NormalizedSplineLocation.Zero), Throws.ArgumentException);
        }

        [Test]
        public void WhenLocationZero_ThenReturnCorrectSegment() {
            var spline = ExampleCurvedSpline.Spline;

            var (segment, location) = CatmullRomSegmentAt(spline, NormalizedSplineLocation.Zero).Value;

            assertExistsAndEquals(
                segment,
                ExampleCurvedSpline.FirstHandle,
                ExampleCurvedSpline.SecondHandle,
                ExampleCurvedSpline.ThirdHandle,
                ExampleCurvedSpline.FourthHandle
            );
            assertExistsAndEquals(location, 0);
        }

        [Test]
        public void WhenLocation1AndLastLocation_ThenReturnLastValidSegment() {
            var node = ExampleCurvedSpline.Spline;

            var (segment, location) = CatmullRomSegmentAt(node, NormalizedSplineLocation.From(1)).Value;

            assertExistsAndEquals(
                segment,
                ExampleCurvedSpline.FirstHandle,
                ExampleCurvedSpline.SecondHandle,
                ExampleCurvedSpline.ThirdHandle,
                ExampleCurvedSpline.FourthHandle
            );
            assertExistsAndEquals(location, 1);
        }

        [Test]
        public void WhenLocation1AndLastLocationButWithinTolerance_ThenReturnLastValidSegment() {
            var node = ExampleCurvedSpline.Spline;

            var (segment, location) = CatmullRomSegmentAt(
                    node,
                    NormalizedSplineLocation.From(1 + EndOfSplineOvershootTolerance / 2.0f)
                )
                .Value;

            assertExistsAndEquals(
                segment,
                ExampleCurvedSpline.FirstHandle,
                ExampleCurvedSpline.SecondHandle,
                ExampleCurvedSpline.ThirdHandle,
                ExampleCurvedSpline.FourthHandle
            );
            assertExistsAndEquals(location, 1);
        }

        [Test]
        public void WhenLocationOneAndNotLastLocation_ThenReturnNextSegment() {
            var spline = ExampleEquidistantLinearSpline.Spline;

            var (segment, location) = CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(1)).Value;

            assertExistsAndEquals(location, 0);
            assertExistsAndEquals(
                segment,
                ExampleEquidistantLinearSpline.SecondHandle,
                ExampleEquidistantLinearSpline.ThirdHandle,
                ExampleEquidistantLinearSpline.FourthHandle,
                ExampleEquidistantLinearSpline.FifthHandle
            );
        }
    }
}