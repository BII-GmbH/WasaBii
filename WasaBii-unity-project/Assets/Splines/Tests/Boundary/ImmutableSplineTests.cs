﻿using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.Splines.Tests {
    public class ImmutableSplineTests {
        [Test]
        public void Ctor_WhenInitializedCorrectly_ThenCorrectNodePositionsAndValidSpline() {
            var beginMarginHandle = new Vector3(1, 0, 0);
            var firstHandle = new Vector3(2, 0, 0);
            var lastHandle = new Vector3(3, 0, 0);
            var endMarginHandle = new Vector3(4, 0, 0);

            var uut = new[] { beginMarginHandle, firstHandle, lastHandle, endMarginHandle }.ToSplineWithMarginHandlesOrThrow();

            Assert.That(uut.IsValid(), Is.True);
            Assert.That(uut[SplineHandleIndex.At(0)], Is.EqualTo(beginMarginHandle));
            Assert.That(uut[SplineHandleIndex.At(1)], Is.EqualTo(firstHandle));
            Assert.That(uut[SplineHandleIndex.At(2)], Is.EqualTo(lastHandle));
            Assert.That(uut[SplineHandleIndex.At(3)], Is.EqualTo(endMarginHandle));
        }
    }
}