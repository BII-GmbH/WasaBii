using BII.WasaBii.Core;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.Unity;
using BII.WasaBii.Unity.Geometry;
using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.Splines.Tests {
    public class EnumerableToSplineExtensionsTests {

        [Test]
        public void ToSplineOrThrow_WhenLessThanTwoNodes_ThenThrowsInsufficientNodePositionsException() {
            var positions = new[] { Vector3.zero };

            Assert.That(() => UnitySpline.FromHandles(positions), Throws.TypeOf<InsufficientNodePositionsException>());
        }

        [Test]
        public void ToSplineOrThrow_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var positions = new[] { first, last };

            var expectedBeginHandle = new Vector3(-3, 0, 0);
            var expectedEndHandle = new Vector3(3, 0, 0);

            var uut = UnitySpline.FromHandles(positions).AsOrThrow<CatmullRomSpline<Vector3, Vector3>>();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(expectedBeginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(expectedEndHandle));
        }

        [Test]
        public void ToSplineOrNone_WhenLessThanTwoNodes_ThenReturnsNull() {
            var positions = new[] { Vector3.zero };

            var uut = CatmullRomSpline.FromHandles(positions, UnitySpline.GeometricOperations.Instance);

            Assert.AreEqual(uut, new CatmullRomSpline.NotEnoughHandles(1, 2).Failure());
        }

        [Test]
        public void ToSplineOrNone_WhenTwoNodes_ThenReturnsCorrectSplineWithCorrectHandles() {
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var positions = new[] { first, last };

            var expectedBeginHandle = new Vector3(-3, 0, 0);
            var expectedEndHandle = new Vector3(3, 0, 0);

            var uutO = CatmullRomSpline.FromHandles(positions, UnitySpline.GeometricOperations.Instance);
            Assert.AreEqual(uutO.WasFailure, false);
            var uut = uutO.ResultOrThrow();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(expectedBeginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(expectedEndHandle));
        }

        [Test]
        public void ToSplineWithHandles_WhenLessThanFourNodes_ThenThrowsInsufficientNodePositionsException() {
            var positions = new[] { Vector3.zero, Vector3.one, Vector3.one };

            Assert.That(() => UnitySpline.FromHandlesIncludingMargin(positions), Throws.TypeOf<InsufficientNodePositionsException>());
        }

        [Test]
        public void ToSpline_WhenFourNodes_ThenReturnsCorrectSpline() {
            var beginHandle = new Vector3(-3, 0, 0);
            var first = new Vector3(-1, 0, 0);
            var last = new Vector3(1, 0, 0);
            var endHandle = new Vector3(3, 0, 0);
            var positions = new[] { beginHandle, first, last, endHandle };

            var uut = UnitySpline.FromHandlesIncludingMargin(positions).AsOrThrow<CatmullRomSpline<Vector3, Vector3>>();
            Assert.That(uut.BeginMarginHandle(), Is.EqualTo(beginHandle));
            Assert.That(uut.FirstHandle(), Is.EqualTo(first));
            Assert.That(uut.LastHandle(), Is.EqualTo(last));
            Assert.That(uut.EndMarginHandle(), Is.EqualTo(endHandle));
        }
    }
}
