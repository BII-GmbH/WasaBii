using System;

namespace BII.WasaBii.Core {
    public static class Mathd {

        public static double Lerp(this double from, double to, double t, bool shouldClamp = true) {
            return shouldClamp 
                ? from + (to - from) * t.Clamp01()
                : LerpUnclamped(from, to, t);
        }
        
        public static double LerpUnclamped(this double from, double to, double t) {
            return from + (to - from) * t;
        }
        
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