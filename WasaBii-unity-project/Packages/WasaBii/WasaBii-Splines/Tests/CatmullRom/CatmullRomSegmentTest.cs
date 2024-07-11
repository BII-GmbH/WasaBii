using BII.WasaBii.Splines.CatmullRom;
using NUnit.Framework;
using UnityEngine;
using static BII.WasaBii.Splines.CatmullRom.CatmullRomSegment;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {

    public class CatmullRomSegmentTest {
        private void assertExistsAndEquals<TTime, TVel>(
            CatmullRomSegment<Vector3, Vector3, TTime, TVel>? segment,
            Vector3 expectedP0,
            Vector3 expectedP1,
            Vector3 expectedP2,
            Vector3 expectedP3
        ) where TTime : unmanaged where TVel : unmanaged {
            Assert.That(segment.HasValue);
            if (segment is { } val) {
                Assert.That(val.P0, Is.EqualTo(expectedP0));
                Assert.That(val.P1, Is.EqualTo(expectedP1));
                Assert.That(val.P2, Is.EqualTo(expectedP2));
                Assert.That(val.P3, Is.EqualTo(expectedP3));
            }
        }

        private void assertExistsAndEquals(double? actualLocation, double expectedLocation) {
            Assert.That(actualLocation.HasValue);
            if (actualLocation is { } val) 
                Assert.That(val, Is.EqualTo(expectedLocation));
        }

        [Test]
        public void WhenLocationLessThanZero_ThenReturnNull() {
            var queryResult = CatmullRomSegmentAt(
                ExampleLinearSpline.Spline, 
                NormalizedSplineLocation.From(-1)
            );
        
            Assert.That(queryResult, Is.EqualTo(null));
        }
        
        [Test]
        public void WhenInvalidSpline_ThenThrows() {
            Assert.That(() => CatmullRomSegmentAt(
                ExampleInvalidSpline.Spline, 
                NormalizedSplineLocation.Zero
            ), Throws.TypeOf<InsufficientNodePositionsException>());
        }
        
        [Test]
        public void WhenLocationZero_ThenReturnCorrectSegment() {
            var spline = ExampleCurvedSpline.Spline;
        
            var (segment, location) = tryDeconstruct(CatmullRomSegmentAt(spline, NormalizedSplineLocation.Zero));
        
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
        
            var (segment, location) = tryDeconstruct(CatmullRomSegmentAt(node, NormalizedSplineLocation.From(1)));
        
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
        
            var (segment, location) = tryDeconstruct(CatmullRomSegmentAt(
                node,
                NormalizedSplineLocation.From(1 + EndOfSplineOvershootTolerance / 2.0f)
            ));
        
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
        
            var (segment, location) = tryDeconstruct(CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(1)));
        
            assertExistsAndEquals(location, 0);
            assertExistsAndEquals(
                segment,
                ExampleEquidistantLinearSpline.SecondHandle,
                ExampleEquidistantLinearSpline.ThirdHandle,
                ExampleEquidistantLinearSpline.FourthHandle,
                ExampleEquidistantLinearSpline.FifthHandle
            );
        }

        private (T0?, T1?) tryDeconstruct<T0, T1>((T0, T1)? tuple) 
        where T0 : struct where T1 : struct
            => tuple is var (t, s) ? (t, s) : (null, null);
    }
}