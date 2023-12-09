using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Core {

    public enum RoundingMode {
        /// <summary>
        /// Rounds to the given number of digits after the point, independent of the value.
        /// </summary>
        /// <example>
        /// 123.456f.Round(digits: 2, DecimalPlaces) == 123.46f
        /// 12345.6f.Round(digits: 2, DecimalPlaces) == 12345.60f
        /// 1.23456f.Round(digits: 2, DecimalPlaces) == 1.23f
        /// </example>
        DecimalPlaces,
        /// <summary>
        /// Rounds to the given number of digits, counted from the leftmost non-zero digit.
        /// </summary>
        /// <example>
        /// 123.456f.Round(digits: 2, SignificantDigits) == 120f
        /// 12345.6f.Round(digits: 2, SignificantDigits) == 12000f
        /// 1.23456f.Round(digits: 2, SignificantDigits) == 1.2f
        /// </example>
        SignificantDigits
    }
    
    public static class MathExtensions {
    
        /// <summary>
        /// Clamps a value between 0 and 1.
        /// Use <see cref="MathExtensions.Clamp{T}"/> to clamp between arbitrary numbers.
        /// </summary>
        [Pure] public static double Clamp01(this double value) {
            if (value < 0.0)
                return 0.0f;
            return value > 1.0 ? 1.0d : value;
        }

        [Pure] public static float Round(this float value, int digits, RoundingMode mode) {
            switch (mode) {
                case RoundingMode.DecimalPlaces: return (float) Math.Round(value, digits);
                case RoundingMode.SignificantDigits:
                    var scale = value.IsNearly(0) 
                        ? 1 
                        : Math.Pow(10, PositionOfFirstSignificantDigit(value) + 1);
                    return (float) (Math.Round(value / scale, digits) * scale);
                default: throw new UnsupportedEnumValueException(mode);
            }
        }
        
        [Pure] public static double Round(this double value, int digits, RoundingMode mode) {
            switch (mode) {
                case RoundingMode.DecimalPlaces: return Math.Round(value, digits);
                case RoundingMode.SignificantDigits:
                    var scale = value.IsNearly(0) 
                        ? 1 
                        : Math.Pow(10, PositionOfFirstSignificantDigit(value) + 1);
                    return Math.Round(value / scale, digits) * scale;
                default: throw new UnsupportedEnumValueException(mode);
            }
        }

        [Pure] public static float RoundToWholeMultipleOf(this float value, float factor)
            => (float) Math.Round(value / factor) * factor;
        [Pure] public static double RoundToWholeMultipleOf(this double value, double factor)
            => Math.Round(value / factor) * factor;

        // From https://stackoverflow.com/a/374470
        // e.g. 2 for 645.65, 0 for 5.6 or -2 for 0.07656.
        // NaN for 0
        [Pure] public static int PositionOfFirstSignificantDigit(this float value) =>
            (int) Math.Floor(Math.Log10(Math.Abs(value)));

        // From https://stackoverflow.com/a/374470
        // e.g. 2 for 645.65, 0 for 5.6 or -2 for 0.07656.
        // NaN for 0
        [Pure] public static int PositionOfFirstSignificantDigit(this double value) =>
            (int) Math.Floor(Math.Log10(Math.Abs(value)));

        public static bool IsInsideInterval<T>(this T value, T intervalStart, T intervalEnd, bool inclusive = false) where T : IComparable<T> {
            var cmp1 = value.CompareTo(intervalStart);
            var cmp2 = value.CompareTo(intervalEnd);
            return inclusive
                ? cmp1 != -1 && cmp2 != 1
                : cmp1 == 1 && cmp2 == -1;
        }

        [Pure] public static bool IsInsideInterval(this float value, float min, float max, float threshold = float.Epsilon)
            => value >= min - threshold && value <= max + threshold;

        [Pure] public static bool IsInsideInterval(this double value, double min, double max, double threshold = double.Epsilon)
            => value >= min - threshold && value <= max + threshold;

        [Pure] public static bool IsNearly(this float value, float other, float threshold = float.Epsilon)
            => Math.Abs(value - other) <= threshold;

        [Pure] public static bool IsNearly(this double value, double other, double threshold = double.Epsilon)
            => Math.Abs(value - other) <= threshold;

        /// Returns the greater value.
        [Pure] public static T Max<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0 ? b : a;

        /// Returns the lesser value.
        [Pure] public static T Min<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0 ? a : b;

        [Pure] public static double Min(this double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : a < b ? a : b;
        [Pure] public static double Max(this double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : a > b ? a : b;
        
        [Pure] public static float Min(this float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : a < b ? a : b;
        [Pure] public static float Max(this float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : a > b ? a : b;

        [Pure] public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> =>
            value.Max(min).Min(max);

        [Pure] public static double Clamp(this double value, double min, double max) =>
            value.Max(min).Min(max);

        [Pure] public static float Clamp(this float value, float min, float max) =>
            value.Max(min).Min(max);

        [Pure] public static bool IsZero(this int value) => value == 0;
        [Pure] public static bool IsZero(this float value) => value.IsNearly(0);
        [Pure] public static bool IsZero(this double value) => value.IsNearly(0);

        [Pure] public static bool IsNegative(this float value) => value < 0;
        [Pure] public static bool IsPositive(this float value) => value > 0;

        [Pure] public static int Abs(this int value) => Math.Abs(value);
        [Pure] public static float Abs(this float value) => Math.Abs(value);
        [Pure] public static double Abs(this double value) => Math.Abs(value);

        [Pure] public static int NegateIf(this int value, bool shouldNegate) => shouldNegate ? -value : value;
        [Pure] public static float NegateIf(this float value, bool shouldNegate) => shouldNegate ? -value : value;
        [Pure] public static double NegateIf(this double value, bool shouldNegate) => shouldNegate ? -value : value;

        [Pure] public static IEnumerable<int> Until(this int from, int toExclusive) => Enumerable.Range(from, toExclusive);

        [Pure] public static int LerpTo(this int fromInclusive, int toExclusive, float progress) {
            if (fromInclusive >= toExclusive) throw new ArgumentOutOfRangeException();
            return ((int) Lerp(fromInclusive, toExclusive, progress)).Min(toExclusive - 1);
        }
        [Pure] public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        
        [Pure] public static float Clamp01(float value) {
          if (value < 0.0)
            return 0.0f;
          return value > 1.0 ? 1f : value;
        }
        
        [Pure] public static int LerpTo(this int fromInclusive, int toExclusive, double progress) {
            if (fromInclusive >= toExclusive) throw new ArgumentOutOfRangeException();
            return ((int) MathD.Lerp(fromInclusive, toExclusive, progress)).Min(toExclusive - 1);
        }
        
        [Pure] public static int Sign(this int value) => value.IsZero() ? 0 : value > 0 ? 1 : -1;
        [Pure] public static int Sign(this float value) => value.IsZero() ? 0 : value > 0 ? 1 : -1;
        [Pure] public static int Sign(this double value) => value.IsZero() ? 0 : value > 0 ? 1 : -1;

        [Pure] public static int Sqr(this int value) => value * value;
        [Pure] public static float Sqr(this float value) => value * value;
        [Pure] public static double Sqr(this double value) => value * value;
        
        /// <summary>
        /// Remainder of dividing <paramref name="x"/> by <paramref name="m"/>.
        /// Equal to the % operator for positive arguments,
        ///  but will always yield a positive result for negative arguments.
        /// Always positive for any x as long as m > 0.
        /// </summary>
        [Pure] public static int Rem(int x, int m) => (x % m + m) % m;
        
        /// <inheritdoc cref="Rem(int,int)"/>
        [Pure] public static float Rem(float x, float m) => (x % m + m) % m;
        
        /// <inheritdoc cref="Rem(int,int)"/>
        [Pure] public static double Rem(double x, double m) => (x % m + m) % m;

    }
}