using BII.WasaBii.Core;
using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.CatmullRomSplines.Tests {
    public class EnumerableToSplineExtensionsTests {

        [Test]
        public void ToSplineOrThrow_WhenLessThanTwoNodes_ThenThrowsInsufficientNodePositionsException() {
            var positions = new[] { Vector3.zero };

            Assert.That(() => positions.ToSplineOrThrow(), Throws.TypeOf<InsufficientNodePositionsException>());
        }

        [Test]
        public void ToSplineOrThrow_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
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
        public void ToSplineOrNone_WhenLessThanTwoNodes_ThenReturnsNull() {
            var positions = new[] { Vector3.zero };

            var uut = positions.ToSpline();

            Assert.AreEqual(uut, Option<Spline<Vector3, Vector3>>.None);
        }

        [Test]
        public void ToSplineOrNone_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var positions = new[] { first, last };

            var expectedBeginHandle = new Vector3(-3, 0, 0);
            var expectedEndHandle = new Vector3(3, 0, 0);

            var uutO = positions.ToSpline();
            Assert.AreNotEqual(uutO, Option<Spline<Vector3, Vector3>>.None);
            var uut = uutO.GetOrThrow();
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
