using System;
using System.Collections;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using BII.WasaBii.Unity;
using BII.WasaBii.Unity.Geometry;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Extra {

    public static class SmoothInterpolation {
        
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
        [Pure] public static float SmoothInterpolateTo(this float current, float target, float smoothness, float progress) =>
            Mathf.Lerp(target, current, Mathf.Pow(smoothness, progress));
            // Proof why the property of consecutive calls mentioned in the summary holds true:
            // float c, t, s, a, b;
            // c.SmoothInterpolateTo(t, s, a).SmoothInterpolateTo(t, s, b)
            // == (c * s^a + t * (1 - s^a)).SmoothInterpolateTo(t, s, b)
            // == (c * s^a + t * (1 - s^a)) * s^b + t * (1 - s^b)
            // == c * s^(a+b) + t * (s^b - s^(a+b)) + t * (1 - s^b)
            // == c * s^(a+b) + t * (1 - s^(a+b))
            // == c.SmoothInterpolateTo(t, s, a+b)

        /// <inheritdoc cref="SmoothInterpolateTo(float,float,float,float)"/>
        [Pure] public static double SmoothInterpolateTo(this double current, double target, double smoothness, double progress) => 
            Mathd.Lerp(target, current, Math.Pow(smoothness, progress));
    }

    public static class UnitSmoothInterpolation {
        /// <inheritdoc cref="SmoothInterpolation.SmoothInterpolateTo(float,float,float,float)"/>
        [Pure] public static T SmoothInterpolateTo<T>(this T current, T target, double smoothness, double progress) 
            where T : struct, IUnitValue<T> => 
            UnitUtils.Lerp(target, current, Math.Pow(smoothness, progress));
    }

    public static class TransformHelperSmoothInterpolation {
        /// <inheritdoc cref="SmoothInterpolation.SmoothInterpolateTo(float,float,float,float)"/>
        [Pure] public static T SmoothInterpolateTo<T>(this T current, T target, double smoothness, double progress) 
            where T : struct, GeometryHelper<T> => 
            target.LerpTo(current, Math.Pow(smoothness, progress));
    }

    // TODO DS: Document.
    public abstract class Smoothed<T> : IDisposable 
    where T : struct {
        
        private T _currentValue;
        public T CurrentValue {
            get => _currentValue;
            set {
                _currentValue = value;
                lastUpdateTime = Time.time;
            }
        }

        public Func<T> TargetGetter;
        public T Target {
            set => TargetGetter = () => value;
        }

        public readonly T Smoothness;
        
        private readonly float? _updateDelay;
        private float lastUpdateTime;
        private Coroutine _coroutine;

        protected Smoothed(T startValue, Func<T> targetGetter, T smoothness, float? updateDelay = null) {
            CurrentValue = startValue;
            TargetGetter = targetGetter;
            Smoothness = smoothness;
            _updateDelay = updateDelay;
        }

        public void Start() => _coroutine = updateValue().Start();
        public void Stop() => _coroutine.Stop();

        private IEnumerator updateValue() {
            lastUpdateTime = Time.time;
            while(true) {
                // TODO DS: replace with _updateDelay?.Let(new WaitForSeconds) once `Let` is integrated
                yield return _updateDelay.HasValue ? new WaitForSeconds(_updateDelay.Value) : null;
                CurrentValue = interpolate(CurrentValue, TargetGetter(), Smoothness, Time.time - lastUpdateTime);
                lastUpdateTime = Time.time;
            }
            // ReSharper disable once IteratorNeverReturns
            // Designed to be aborted in `Stop`
        }

        protected abstract T interpolate(T current, T target, T smoothness, float progress);

        public void Dispose() => Stop();
    }

    public sealed class SmoothedFloat : Smoothed<float> {
        public SmoothedFloat(float startValue, Func<float> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override float interpolate(float current, float target, float smoothness, float progress)
            => current.SmoothInterpolateTo(target, smoothness, progress);
    }

    public sealed class SmoothedDouble : Smoothed<double> {
        public SmoothedDouble(double startValue, Func<double> targetGetter, double smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override double interpolate(double current, double target, double smoothness, float progress)
            => current.SmoothInterpolateTo(target, smoothness, progress);
    }

}
