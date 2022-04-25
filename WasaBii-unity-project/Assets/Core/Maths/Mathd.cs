using System;

namespace BII.WasaBii.Core {
    public static class Mathd {

        /// <summary>
        /// Linear interpolation between <see cref="from"/> and <see cref="to"/>.
        /// </summary>
        /// <param name="t">
        /// Controls how far the result has progressed between the two values.
        /// </param>
        /// <param name="shouldClamp">
        /// Defines whether <see cref="t"/> values less than 0 or greater than 1 should be clamped to that interval.
        /// </param>
        /// <returns>
        /// <see cref="from"/>, if <see cref="t"/> is zero (or less, with <see cref="shouldClamp"/> being true).
        /// <see cref="to"/>, if <see cref="t"/> is one (or greater, with <see cref="shouldClamp"/> being true).
        /// A value between <see cref="from"/> and <see cref="to"/>, if <see cref="t"/> is in (0,1).
        /// A value outside of (<see cref="from"/>,<see cref="to"/>), if <see cref="t"/> is outside of (0,1) and <see cref="shouldClamp"/> is false.
        /// </returns>
        public static double Lerp(this double from, double to, double t, bool shouldClamp = true) =>
            shouldClamp 
                ? from + (to - from) * t.Clamp01()
                : LerpUnclamped(from, to, t);

        /// <see cref="Lerp"/> with shouldClamp == false.
        public static double LerpUnclamped(this double from, double to, double t) => 
            from + (to - from) * t;

        /// <summary>
        /// The inverse operation of <see cref="Lerp"/>.
        /// </summary>
        /// <param name="a">Corresponds to "from" in <see cref="Lerp"/></param>
        /// <param name="b">Corresponds to "to" in <see cref="Lerp"/></param>
        /// <param name="value">Corresponds to the result of <see cref="Lerp"/></param>
        /// <param name="shouldClamp">Corresponds to "shouldClamp" in <see cref="Lerp"/></param>
        /// <returns>
        /// 0, if <see cref="value"/> is <see cref="a"/> (or less*, with <see cref="shouldClamp"/> being true).
        /// 1, if <see cref="value"/> is <see cref="b"/> (or greater*, with <see cref="shouldClamp"/> being true).
        /// A value between 0 and 1, if <see cref="value"/> is in (<see cref="a"/>,<see cref="b"/>).
        /// A value outside of (0,1), if <see cref="value"/> is outside of (<see cref="a"/>,<see cref="b"/>) and <see cref="shouldClamp"/> is false.
        /// *: assuming a is less than b, invert if this is not the case.
        /// </returns>
        public static double InverseLerp(double a, double b, double value, bool shouldClamp = true) => a != b 
            ? ((value - a) / (b - a)).If(shouldClamp, val => val.Clamp01()) 
            : 0.0;

        public static int FloorToInt(double d) => (int) Math.Floor(d);

        public static double Min(double a, double b) => a < b ? a : b;

        public static double Max(double a, double b) => a > b ? a : b;
        
        public static double Min(params double[] values) {
            if (values.Length == 0) {
                return 0; //this is equivalent to the Unity method
            }
            var result = values[0];
            foreach (var value in values) {
                if (value < result) {
                    result = value;
                }
            }
            return result;
        }
        
        public static double Max(params double[] values) {
            if (values.Length == 0) {
                return 0; //this is equivalent to the Unity method
            }
            var result = values[0];
            foreach (var value in values) {
                if (value > result) {
                    result = value;
                }
            }
            return result;
        }
    }
}