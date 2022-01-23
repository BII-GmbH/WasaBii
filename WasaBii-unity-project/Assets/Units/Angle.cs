using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {

    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class AngleUnit : Unit {

        [JsonConstructor]
        private AngleUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly AngleUnit Degrees = new AngleUnit("°", Math.PI / 180f);
        public static readonly AngleUnit Radians = new AngleUnit("rad", 1f);

        public static IReadOnlyList<AngleUnit> All = new[]{Degrees, Radians};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Angle : ValueWithUnit<Angle, AngleUnit> {

        public IReadOnlyList<AngleUnit> AllUnits => AngleUnit.All;
        public AngleUnit DisplayUnit => AngleUnit.Degrees;
        public AngleUnit SIUnit => AngleUnit.Radians;

        private readonly double radians;

        public double SIValue => radians;
        
        public static readonly Angle Zero = new();
        public static readonly Angle HalfCircle = Math.PI.Radians();
        public static readonly Angle FullCircle = 2 * HalfCircle;

        public Angle(double time, AngleUnit unit) => radians = time * unit.Factor;

        public Angle CopyWithDifferentSIValue(double newSIValue) => newSIValue.Radians();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        /// The angle between 0° and 360°
        public Angle Normalized360 => mod(radians, FullCircle.radians).Radians();
        [Pure] private static double mod(double x, double m) => (x % m + m) % m;

        /// The angle between -180° and 180°
        public Angle Normalized180 => Normalized360.If(n => n > HalfCircle, n => n - FullCircle);

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
            from = from.Normalized360;
            to = to.Normalized360;
            if ((to - from).Abs() > HalfCircle) {
                from = from.Normalized180;
                to = to.Normalized180;
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
            return UnitUtils.Lerp(from, to, progress, shouldClamp);
        }
        
        public double Cos => Math.Cos(radians);
        public double Sin => Math.Sin(radians);
        public double Tan => Math.Tan(radians);

        public static Angle Acos(Number f) => Math.Acos(f).Radians();
        public static Angle Asin(Number f) => Math.Asin(f).Radians();
        public static Angle Atan(Number f) => Math.Atan(f).Radians();
        public static Angle Atan2(Number opposite, Number adjacent) => Math.Atan2(opposite, adjacent).Radians();

        /// <param name="opposite">German: Ankathete</param>
        public static Angle Asin(Length opposite, Length hypotenuse) => Asin(opposite / hypotenuse);

        /// <param name="adjacent">German: Gegenkathete</param>
        public static Angle Acos(Length adjacent, Length hypotenuse) => Acos(adjacent / hypotenuse);

        /// <param name="opposite">German: Ankathete</param>
        /// <param name="adjacent">German: Gegenkathete</param>
        public static Angle Atan(Length opposite, Length adjacent) => Atan(opposite / adjacent);
        public static Angle Atan2(Length opposite, Length adjacent) => Atan2(opposite.SIValue.Number(), adjacent.SIValue.Number());
        
        public static Angle operator +(Angle a) => a;
        public static Angle operator -(Angle a) => (-a.radians).Radians();
        public static Angle operator +(Angle a, Angle b) => (a.radians + b.radians).Radians();
        public static Angle operator -(Angle a, Angle b) => (a.radians - b.radians).Radians();
        public static Angle operator *(Number s, Angle a) => (a.radians * s).Radians();
        public static Angle operator *(Angle a, Number s) => (a.radians * s).Radians();
        public static Angle operator /(Angle a, Number s) => (a.radians / s).Radians();
        public static Angle operator *(double s, Angle a) => (a.radians * s).Radians();
        public static Angle operator *(Angle a, double s) => (a.radians * s).Radians();
        public static Angle operator /(Angle a, double s) => (a.radians / s).Radians();
        public static Number operator /(Angle a, Angle b) => (a.SIValue / b.SIValue).Number();
        public static AnglePerDuration operator /(Angle a, Duration d) => (a.AsRadians() / d.AsSeconds()).RadiansPerSecond();
        public static bool operator <(Angle a, Angle b) => a.SIValue < b.SIValue;
        public static bool operator >(Angle a, Angle b) => a.SIValue > b.SIValue;
        public static bool operator <=(Angle a, Angle b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Angle a, Angle b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Angle a, Angle b) => a.SIValue == b.SIValue;
        public static bool operator !=(Angle a, Angle b) => a.SIValue != b.SIValue;
        
        // Caution: the System.Numerics.Quaternion functions expect angles to
        // be given in radians while the Unity equivalent expects degrees.
        // Also, it doesn't normalize the axis for you while Unity does.
        [Pure] public System.Numerics.Quaternion WithAxis(System.Numerics.Vector3 axis) => 
            System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.Normalize(axis), (float)this.AsRadians());
        
        public bool IsInsideInterval(Angle min, Angle max) {
            min = min.Normalized360;
            max = max.Normalized360;
            var me = this.Normalized360;
            // Since we are working with angles here, min can be greater than max.
            // In this case, all values OUTSIDE the double interval are actually inside the angle interval.
            return (min < max) 
                ? (me >= min && me <= max)
                : (me >= min || me <= max);
        }
        
        public override string ToString() => $"{this.AsRadians()} Radians ({this.AsDegrees()}°)";

        public bool Equals(Angle other) => this == other;
        public override bool Equals(object obj) => obj is Angle Angle && this == Angle;
        public override int GetHashCode() => SIValue.GetHashCode();

        public static Angle Clamp(Angle value, Angle min, Angle max) {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public static Angle MinimalDifference(Angle from, Angle to) {
            (from, to) = NormalizedWithMinimalDifference(from, to);
            return to - from;
        }
    }

    public static class AngleExtensions {

        public static Angle Radians(this Number value) => new Angle(value, AngleUnit.Radians);
        public static Angle Degrees(this Number value) => new Angle(value, AngleUnit.Degrees);

        public static Angle Radians(this float value) => new Angle(value, AngleUnit.Radians);
        public static Angle Degrees(this float value) => new Angle(value, AngleUnit.Degrees);
        
        public static Angle Radians(this double value) => new Angle(value, AngleUnit.Radians);
        public static Angle Degrees(this double value) => new Angle(value, AngleUnit.Degrees);

        public static Angle Radians(this int value) => new Angle(value, AngleUnit.Radians);
        public static Angle Degrees(this int value) => new Angle(value, AngleUnit.Degrees);

        public static Number AsDegrees(this Angle angle) => angle.As(AngleUnit.Degrees);
        public static Number AsRadians(this Angle angle) => angle.As(AngleUnit.Radians);

        public static Angle Abs(Angle a) => new Angle(Math.Abs(a.SIValue), AngleUnit.Radians);
    }
}