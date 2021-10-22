using System;
using UnityEngine;

namespace BII.WasaBii.Extra {

    public static class SmoothInterpolation {
        
        // TODO DS: Make variants for ValueWithUnit when it is integrated.

        /// <summary>
        /// Interpolates between two values such that the interpolation can go on indefinitely,
        /// approaching the target asymptotically.
        /// The idea is that instead of setting a variable to discrete values at certain events
        /// with a noticeable delay, the transition between them is done smoothly by only setting
        /// the target value and interpolating towards that constantly. This makes the resulting
        /// value continuous. Check figure `SmoothInterpolation.pdf` for an illustration.
        /// The formula is designed such that the result of two consecutive calls with the same
        /// smoothness value is equal to one call with added progress:
        /// <example><code>
        /// 10f.SmoothInterpolateTo(50f, 0.5f, 1.5f).SmoothInterpolateTo(50f, 0.5f, 3.5f) ==
        /// 10f.SmoothInterpolateTo(50f, 0.5f, 5f)
        /// </code></example>
        /// </summary>
        /// <param name="current">The starting value from which the interpolation happens.
        /// In most cases except initialization, this will be the result of the last call.</param>
        /// <param name="target">The target value to which the interpolation happens.</param>
        /// <param name="smoothness">Defines how fast the value changes, i.e. how strongly the result
        /// differs from <see cref="current"/>. Must be between 0 (not smoothed, always returns
        /// <see cref="target"/>) and 1 (infinitely smooth, always returns <see cref="current"/>.</param>
        /// <param name="progress">Defines how far to interpolate. This will usually be the time that
        /// has passed since the last call (which returned <see cref="current"/>), e.g. <see cref="Time.deltaTime"/>.
        /// Must be greater than or equal to 0.</param>
        public static float SmoothInterpolateTo(this float current, float target, float smoothness, float progress) =>
            Mathf.Lerp(target, current, Mathf.Pow(smoothness, progress));

        /// <inheritdoc cref="SmoothInterpolateTo(float,float,float,float)"/>
        public static double SmoothInterpolateTo(this double current, double target, double smoothness, double progress) {
            // Note DS: There is no `Lerp` for doubles. Do we have one in our codebase?. If not:
            // TODO DS for maintainer: Add `Lerp` for doubles in an appropriate place.
            var t = Math.Pow(smoothness, progress);
            return current * t + target * (1 - t);
            // Proof why the property of consecutive calls mentioned in the summary holds true:
            // float c, t, s, a, b;
            // c.SmoothInterpolateTo(t, s, a).SmoothInterpolateTo(t, s, b)
            // == (c * s^a + t * (1 - s^a)).SmoothInterpolateTo(t, s, b)
            // == (c * s^a + t * (1 - s^a)) * s^b + t * (1 - s^b)
            // == c * s^(a+b) + t * (s^b - s^(a+b)) + t * (1 - s^b)
            // == c * s^(a+b) + t * (1 - s^(a+b))
            // == c.SmoothInterpolateTo(t, s, a+b)
        }
        
    }

    // TODO DS: Implement a Behaviour / Coroutines / Pool thing once the main code has been added.
    
}
