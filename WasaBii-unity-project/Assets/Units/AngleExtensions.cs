using System;
using JetBrains.Annotations;
using BII.WasaBii.Core;
using UnityEngine;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace BII.WasaBii.UnitSystem {
    public static class Angles {

        static Angles() {
            if (!EnsureGenerationDidRun.DidRun) throw new Exception();
            Debug.Log(EnsureGenerationDidRun.ErrorMessage);
        }
        
        public static readonly Angle HalfCircle = Math.PI.Radians();
        public static readonly Angle FullCircle = 2 * HalfCircle;
        
        [Pure] private static double remainder(double x, double m) => (x % m + m) % m;
        
        /// The angle between 0° and 360°
        public static Angle Normalized360(this Angle angle) => remainder(angle.SiValue, FullCircle.SiValue).Radians();
        

        /// The angle between -180° and 180°
        public static Angle Normalized180(this Angle angle) => 
            angle.Normalized360().If(n => n > HalfCircle, n => n - FullCircle);

        /// <summary>
        /// Brings the two angles into the same "space" (either [0°, 360°) or [-180°, 180°))
        /// where the absolute of the difference between them is minimal.
        /// </summary>
        /// <example>
        /// When a crane turns from one direction to another, you want it to move as
        /// little as possible. E.g.: If <see cref="from"/> is 10° and <see cref="to"/> is 710°
        /// you will not want to lerp between these values. Likewise, if you just <see cref="Normalized360"/>
        /// them, the values will still be 10° and 350°, so the crane will turn 340° instead
        /// of just 20° in the opposite direction.
        /// </example>
        public static (Angle from, Angle to) NormalizedWithMinimalDifference(Angle from, Angle to) {
            from = from.Normalized360();
            to = to.Normalized360();
            if ((to - from).Abs() > HalfCircle) {
                from = from.Normalized180();
                to = to.Normalized180();
            }

            return (from, to);
        }

        /// <summary>
        /// Lerps between the angles after transforming them into a space where their difference is minimal.
        /// </summary>
        /// <example>
        /// When you lerp <see cref="from"/> -122° <see cref="to"/> 130° the normal way, the interpolated
        /// values will go: -122° ... -90° ... 0° ... 90° ... 130° which results in a total 252°
        /// of movement. This method will transform <see cref="from"/> to 238° so that the interpolation
        /// will only traverse 238° ... 180° ... 130° which is a total movement of 108°.
        /// </example>
        public static Angle LerpWithMinimalDifference(Angle from, Angle to, double progress, bool shouldClamp = true) {
            (from, to) = NormalizedWithMinimalDifference(from, to);
            return Units.Lerp(from, to, progress, shouldClamp);
        }
        
        public static double Cos(this Angle angle) => Math.Cos(angle.SiValue);
        public static double Sin(this Angle angle) => Math.Sin(angle.SiValue);
        public static double Tan(this Angle angle) => Math.Tan(angle.SiValue);

        public static Angle Acos(double d) => Math.Acos(d).Radians();
        public static Angle Asin(double d) => Math.Asin(d).Radians();
        public static Angle Atan(double d) => Math.Atan(d).Radians();
        public static Angle Atan2(double opposite, double adjacent) => Math.Atan2(opposite, adjacent).Radians();

        /// <param name="opposite">German: Ankathete</param>
        public static Angle Asin(Length opposite, Length hypotenuse) => Asin(opposite / hypotenuse);

        /// <param name="adjacent">German: Gegenkathete</param>
        public static Angle Acos(Length adjacent, Length hypotenuse) => Acos(adjacent / hypotenuse);

        /// <param name="opposite">German: Ankathete</param>
        /// <param name="adjacent">German: Gegenkathete</param>
        public static Angle Atan(Length opposite, Length adjacent) => Atan(opposite / adjacent);
        public static Angle Atan2(Length opposite, Length adjacent) => Atan2(opposite.SiValue, adjacent.SiValue);
        
        // Caution: the System.Numerics.Quaternion functions expect angles to
        // be given in radians while the Unity equivalent expects degrees.
        // Also, it doesn't normalize the axis for you while Unity does.
        [Pure] public static Quaternion WithAxis(this Angle angle, Vector3 axis) => 
            Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), (float) angle.AsRadians());
        
        public static bool IsInsideInterval(this Angle angle, Angle min, Angle max) {
            min = min.Normalized360();
            max = max.Normalized360();
            var me = angle.Normalized360();
            // Since we are working with angles here, min can be greater than max.
            // In this case, all values OUTSIDE the double interval are actually inside the angle interval.
            return (min < max) 
                ? (me >= min && me <= max)
                : (me >= min || me <= max);
        }
        
        public static Angle MinimalDifference(Angle from, Angle to) {
            (from, to) = NormalizedWithMinimalDifference(from, to);
            return to - from;
        }
    }
}