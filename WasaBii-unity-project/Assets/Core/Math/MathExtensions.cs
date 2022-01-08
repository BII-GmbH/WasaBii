using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Core {
    public enum RoundingMode {
        DecimalPlaces,
        SignificantDigits
    }
    public static class MathExtensions {
        
        
        /// <summary>
        /// Clamps a value between 0 and 1.
        /// Use <see cref="MathExtensions.Clamp{T}"/> to clamp between arbitrary numbers.
        /// </summary>
        public static double Clamp01(this double value) {
            if (value < 0.0)
                return 0.0f;
            return value > 1.0 ? 1.0d : value;
        }

        public static float Round(this float value, int digits, RoundingMode mode) {
            switch (mode) {
                case RoundingMode.DecimalPlaces: return (float) Math.Round(value, digits);
                case RoundingMode.SignificantDigits:
                    var scale = value.IsNearly(0) 
                        ? 1 
                        : Math.Pow(10, PositionOfFirstSignificantDigit(value) + 1);
                    return (float) (Math.Round(value / scale, digits) * scale);
                default: throw new UnsupportedEnumValueException(mode, $"Rounding mode {mode} not supported");
            }
        }
        
        public static double Round(this double value, int digits, RoundingMode mode) {
            switch (mode) {
                case RoundingMode.DecimalPlaces: return Math.Round(value, digits);
                case RoundingMode.SignificantDigits:
                    var scale = value.IsNearly(0) 
                        ? 1 
                        : Math.Pow(10, PositionOfFirstSignificantDigit(value) + 1);
                    return Math.Round(value / scale, digits) * scale;
                default: throw new UnsupportedEnumValueException(mode, $"Rounding mode {mode} not supported");
            }
        }

        public static float RoundToWholeMultipleOf(this float value, float factor)
            => (float) Math.Round(value / factor) * factor;
        public static double RoundToWholeMultipleOf(this double value, double factor)
            => Math.Round(value / factor) * factor;

        // From https://stackoverflow.com/a/374470
        /// e.g. 2 for 645.65, 0 for 5.6 or -2 for 0.07656.
        /// NaN for 0
        public static int PositionOfFirstSignificantDigit(this float value) =>
            (int) Math.Floor(Math.Log10(Math.Abs(value)));

        // From https://stackoverflow.com/a/374470
        /// e.g. 2 for 645.65, 0 for 5.6 or -2 for 0.07656.
        /// NaN for 0
        public static int PositionOfFirstSignificantDigit(this double value) =>
            (int) Math.Floor(Math.Log10(Math.Abs(value)));

        public static bool IsInsideInterval<T>(this T value, T intervalStart, T intervalEnd) where T : IComparable<T> =>
            value.CompareTo(intervalStart) == 1 && value.CompareTo(intervalEnd) == -1;

        public static bool IsNearly(this float value, float other, float threshold = float.Epsilon)
            => Math.Abs(value - other) <= threshold;

        public static bool IsNearly(this double value, double other, double threshold = double.Epsilon)
            => Math.Abs(value - other) <= threshold;

        /// <summary>
        /// Returns the greater value.
        /// </summary>
        public static T Max<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0 ? b : a;

        /// <summary>
        /// Returns the lesser value.
        /// </summary>
        public static T Min<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0 ? a : b;

        public static double Min(this double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : a < b ? a : b;
        public static double Max(this double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : a > b ? a : b;
        
        public static float Min(this float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : a < b ? a : b;
        public static float Max(this float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : a > b ? a : b;

        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> =>
            value.Max(min).Min(max);

        public static double Clamp(this double value, double min, double max) =>
            value.Max(min).Min(max);

        public static float Clamp(this float value, float min, float max) =>
            value.Max(min).Min(max);

        public static bool IsZero(this float value) => value.Equals(0);
        
        public static bool IsZero(this int value) => value == 0;

        public static bool IsNegative(this float value) => value < 0;

        public static bool IsPositive(this float value) => value > 0;

        public static float Abs(this float value) => Math.Abs(value);
        public static double Abs(this double value) => Math.Abs(value);

        public static int NegateIf(this int value, bool shouldNegate) => shouldNegate ? -value : value;
        public static float NegateIf(this float value, bool shouldNegate) => shouldNegate ? -value : value;
        public static double NegateIf(this double value, bool shouldNegate) => shouldNegate ? -value : value;

        public static IEnumerable<int> Until(this int from, int toExclusive) => Enumerable.Range(from, toExclusive);

        public static int LerpTo(this int fromInclusive, int toExclusive, float progress) {
            Contract.Assert(fromInclusive < toExclusive);
            return ((int) Lerp(fromInclusive, toExclusive, progress)).Min(toExclusive - 1);
        }
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Clamp01(float value)
        {
          if (value < 0.0)
            return 0.0f;
          return value > 1.0 ? 1f : value;
        }
        public static int LerpTo(this int fromInclusive, int toExclusive, double progress) {
            Contract.Assert(fromInclusive < toExclusive);
            return ((int) Mathd.Lerp(fromInclusive, toExclusive, progress)).Min(toExclusive - 1);
        }
        
        public static int Sign(this float value) => value.IsZero() ? 0 : value > 0 ? 1 : -1;
        public static int Sign(this int value) => value.IsZero() ? 0 : value > 0 ? 1 : -1;
    }
}