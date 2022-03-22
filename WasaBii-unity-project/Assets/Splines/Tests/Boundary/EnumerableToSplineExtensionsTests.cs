using NUnit.Framework;
using UnityEngine;

namespace BII.CatmullRomSplines.Tests {
    public class EnumerableToSplineExtensionsTests {

        [Test]
        public void ToSpline_WhenLessThanTwoNodes_ThenThrowsInsufficientNodePositionsException() {
            var positions = new[] { Vector3.zero };

            Assert.That(() => positions.ToSplineOrThrow(), Throws.TypeOf<InsufficientNodePositionsException>());
        }

        [Test]
        public void ToSpline_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var positions = new[] { first, last };

            var expectedBeginHandle = new Vector3(-3, 0, 0);
            var expectedEndHandle = new Vector3(3, 0, 0);

            var uut = positions.ToSplineOrThrow();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(expectedBeginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(expectedEndHandle));
        }

        [Test]
        public void ToSplineOrDefault_WhenLessThanTwoNodes_ThenReturnsNull() {
            var positions = new[] { Vector3.zero };

            var uut = positions.ToSpline();

            Assert.That(uut, Is.Null);
        }

        [Test]
        public void ToSplineOrDefault_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var positions = new[] { first, last };

            var expectedBeginHandle = new Vector3(-3, 0, 0);
            var expectedEndHandle = new Vector3(3, 0, 0);

            var uut = positions.ToSpline();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(expectedBeginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(expectedEndHandle));
        }

        [Test]
        public void ToSplineWithHandles_WhenLessThanFourNodes_ThenThrowsInsufficientNodePositionsException() {
            var positions = new[] { Vector3.zero, Vector3.one, Vector3.one };

            Assert.That(() => positions.ToSplineWithMarginHandlesOrThrow(), Throws.TypeOf<InsufficientNodePositionsException>());
        }

        [Test]
        public void ToSpline_WhenFourNodes_ThenReturnsCorrectSpline() {
            var beginHandle = new Vector3(-3, 0, 0);
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var endHandle = new Vector3(3, 0, 0);
            var positions = new[] { beginHandle, first, last, endHandle };

            var uut = positions.ToSplineWithMarginHandlesOrThrow();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(beginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(endHandle));
        }
    }
}
