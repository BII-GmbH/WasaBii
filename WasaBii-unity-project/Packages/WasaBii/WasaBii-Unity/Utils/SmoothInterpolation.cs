#nullable enable

using System;
using System.Collections;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {

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
        /// Interpolate(Interpolate(10f, 50f, 0.5f, 1.5f), 50f, 0.5f, 3.5f) ==
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
        [Pure] public static float Interpolate(float current, float target, float smoothness, float progress) =>
            Mathf.Lerp(target, current, Mathf.Pow(smoothness, progress));
            // Proof why the property of consecutive calls mentioned in the summary holds true:
            // float c, t, s, a, b;
            // Interpolate(Interpolate(c, t, s, a), t, s, b)
            // == Interpolate(c * s^a + t * (1 - s^a), t, s, b)
            // == (c * s^a + t * (1 - s^a)) * s^b + t * (1 - s^b)
            // == c * s^(a+b) + t * (s^b - s^(a+b)) + t * (1 - s^b)
            // == c * s^(a+b) + t * (1 - s^(a+b))
            // == Interpolate(c, t, s, a+b)

        /// <inheritdoc cref="Interpolate(float,float,float,float)"/>
        [Pure] public static double Interpolate(double current, double target, double smoothness, double progress) => 
            MathD.Lerp(target, current, Math.Pow(smoothness, progress));
        
        /// <inheritdoc cref="Interpolate(float,float,float,float)"/>
        [Pure] public static T Interpolate<T>(T current, T target, double smoothness, double progress) 
            where T : struct, IUnitValue<T> => 
            Units.Lerp(target, current, Math.Pow(smoothness, progress));
    }

    /// <summary>
    /// The base for classes that track a single <see cref="CurrentValue"/> of type <typeparamref name="T"/> and
    /// update it automatically over time such that it tends towards the <see cref="Target"/> as specified in
    /// <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/>. The changing of the value
    /// happens in <c>Update()</c> / in a coroutine managed by the <see cref="Coroutines.CoroutineRunner"/>.
    /// Child classes specify the type-specific (liner) interpolation.
    /// </summary>
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

        public readonly float Smoothness;
        
        private readonly float? _updateDelay;
        private float lastUpdateTime;
        private Coroutine? _coroutine;

        protected Smoothed(T startValue, Func<T> targetGetter, float smoothness, float? updateDelay = null) {
            CurrentValue = startValue;
            TargetGetter = targetGetter;
            Smoothness = smoothness;
            _updateDelay = updateDelay;
        }

        public void Start() => _coroutine = updateValue().Start();
        public void Stop() => _coroutine?.Stop();

        private IEnumerator updateValue() {
            lastUpdateTime = Time.time;
            while(true) {
                yield return _updateDelay == null ? null : new WaitForSeconds(_updateDelay.Value);
                var t = Math.Pow(Smoothness, Time.time - lastUpdateTime);
                CurrentValue = interpolate(CurrentValue, TargetGetter(), t);
                lastUpdateTime = Time.time;
            }
            // ReSharper disable once IteratorNeverReturns
            // Designed to be aborted in `Stop`
        }

        protected abstract T interpolate(T current, T target, double progress);

        public void Dispose() => Stop();
    }
    
    /// <summary>
    /// Tracks a single floating point <see cref="Smoothed{Single}.CurrentValue"/> and updates it automatically
    /// over <see cref="Time.time"/> such that it tends towards the <see cref="Smoothed{Single}.Target"/> as
    /// specified in <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/> via coroutine.
    /// </summary>
    public sealed class SmoothedFloat : Smoothed<float> {
        public SmoothedFloat(float startValue, Func<float> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override float interpolate(float current, float target, double progress)
            => Mathf.Lerp(target, current, (float) progress);
    }

    /// <summary>
    /// Tracks a single double precision floating point <see cref="Smoothed{Single}.CurrentValue"/> and updates
    /// it automatically over <see cref="Time.time"/> such that it tends towards the <see cref="Smoothed{Single}.Target"/>
    /// as specified in <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/> via coroutine.
    /// </summary>
    public sealed class SmoothedDouble : Smoothed<double> {
        public SmoothedDouble(double startValue, Func<double> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override double interpolate(double current, double target, double progress)
            => MathD.Lerp(current, target, progress);
    }

    /// <summary>
    /// Tracks a single <see cref="IUnitValue"/> <see cref="Smoothed{Single}.CurrentValue"/> and updates it automatically
    /// over <see cref="Time.time"/> such that it tends towards the <see cref="Smoothed{Single}.Target"/> as
    /// specified in <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/> via coroutine.
    /// </summary>
    public sealed class SmoothedUnitValue<T> : Smoothed<T> where T : struct, IUnitValue<T> {
        public SmoothedUnitValue(T startValue, Func<T> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override T interpolate(T current, T target, double progress)
            => Units.Lerp(current, target, progress);
    }

    /// <summary>
    /// Tracks a single <see cref="WithLerp{T}"/> <see cref="Smoothed{Single}.CurrentValue"/> and updates it automatically
    /// over <see cref="Time.time"/> such that it tends towards the <see cref="Smoothed{Single}.Target"/> as
    /// specified in <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/> via coroutine.
    /// </summary>
    public sealed class SmoothedLerp<T> : Smoothed<T> where T : struct, WithLerp<T> {
        public SmoothedLerp(T startValue, Func<T> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override T interpolate(T current, T target, double progress)
            => current.LerpTo(target, progress);
    }

    /// <summary>
    /// Tracks a single <see cref="WithSlerp{T}"/> <see cref="Smoothed{Single}.CurrentValue"/> and updates it automatically
    /// over <see cref="Time.time"/> such that it tends towards the <see cref="Smoothed{Single}.Target"/> as
    /// specified in <see cref="SmoothInterpolation.Interpolate(float, float, float, float)"/> via coroutine.
    /// </summary>
    public sealed class SmoothedSlerp<T> : Smoothed<T> where T : struct, WithSlerp<T> {
        public SmoothedSlerp(T startValue, Func<T> targetGetter, float smoothness, float? updateDelay = null) 
            : base(startValue, targetGetter, smoothness, updateDelay) { }

        protected override T interpolate(T current, T target, double progress)
            => current.SlerpTo(target, progress);
    }

}
