using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.Unity.Tests {
    
    public class SmoothInterpolationTests {

        [Test]
        public void WhenInterpolating_ThenResultIsInRange() {
            var from = 10f;
            var to = -20f;
            var smoothness = 0.5f;
            var time = 2f;
            var result = SmoothInterpolation.Interpolate(from, to, smoothness, time);
            Assert.Greater(result, Mathf.Min(from, to));
            Assert.Less(result, Mathf.Max(from, to));
        }
        
        [Test]
        public void WhenHigherSmoothness_ThenCloserToStart() {
            var from = 10f;
            var to = -20f;
            var smoothness1 = 0.25f;
            var smoothness2 = 0.75f;
            var time = 2f;
            var result1 = SmoothInterpolation.Interpolate(from, to, smoothness1, time);
            var result2 = SmoothInterpolation.Interpolate(from, to, smoothness2, time);
            Assert.Less(Mathf.Abs(from - result2), Mathf.Abs(from - result1));
        }
        
        [Test]
        public void WhenConsecutiveCalls_ThenEqualToSingleCall() {
            var from = 10d;
            var to = -20d;
            var smoothness = 0.5d;
            var time1 = 2d;
            var time2 = 5d;
            var med = SmoothInterpolation.Interpolate(from, to, smoothness, time1);
            var consecutively = SmoothInterpolation.Interpolate(med, to, smoothness, time2);
            var singleCall = SmoothInterpolation.Interpolate(from, to, smoothness, time1 + time2);
            Assert.AreEqual(consecutively, singleCall, delta: 1E-10d);
        }
        
        [Test]
        public void WhenSmoothnessZero_ThenTarget() {
            var from = 10f;
            var to = -20f;
            var smoothness = 0f;
            var time = 2f;
            var result = SmoothInterpolation.Interpolate(from, to, smoothness, time);
            Assert.AreEqual(result, to, delta: 1E-10f);
        }
        
        [Test]
        public void WhenSmoothnessOne_ThenStart() {
            var from = 10f;
            var to = -20f;
            var smoothness = 1f;
            var time = 2f;
            var result = SmoothInterpolation.Interpolate(from, to, smoothness, time);
            Assert.AreEqual(result, from, delta: 1E-10f);
        }
        
    }
    
}
