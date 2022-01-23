using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {

    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class AnglePerDurationUnit : Unit {

        [JsonConstructor]
        private AnglePerDurationUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly AnglePerDurationUnit DegreesPerSecond = new AnglePerDurationUnit("°/s", AngleUnit.Degrees.Factor / TimeUnit.Seconds.Factor);
        public static readonly AnglePerDurationUnit DegreesPerMinute = new AnglePerDurationUnit("°/min", AngleUnit.Degrees.Factor / TimeUnit.Minutes.Factor);
        public static readonly AnglePerDurationUnit RadiansPerSecond = new AnglePerDurationUnit("rad/s", AngleUnit.Radians.Factor / TimeUnit.Seconds.Factor);

        public static IReadOnlyList<AnglePerDurationUnit> All = new[]{DegreesPerSecond, DegreesPerMinute, RadiansPerSecond};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct AnglePerDuration : ValueWithUnit<AnglePerDuration, AnglePerDurationUnit> {

        public IReadOnlyList<AnglePerDurationUnit> AllUnits => AnglePerDurationUnit.All;
        public AnglePerDurationUnit DisplayUnit => AnglePerDurationUnit.DegreesPerSecond;
        public AnglePerDurationUnit SIUnit => AnglePerDurationUnit.RadiansPerSecond;

        private readonly double radiansPerSecond;

        public double SIValue => radiansPerSecond;
        
        public AnglePerDuration(double speed, AnglePerDurationUnit unit) => radiansPerSecond = speed * unit.Factor;

        public AnglePerDuration CopyWithDifferentSIValue(double newSIValue) => newSIValue.RadiansPerSecond();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static AnglePerDuration operator +(AnglePerDuration a) => a;
        public static AnglePerDuration operator -(AnglePerDuration a) => (-a.radiansPerSecond).RadiansPerSecond();
        public static AnglePerDuration operator +(AnglePerDuration a, AnglePerDuration b) => (a.radiansPerSecond + b.radiansPerSecond).RadiansPerSecond();
        public static AnglePerDuration operator -(AnglePerDuration a, AnglePerDuration b) => (a.radiansPerSecond - b.radiansPerSecond).RadiansPerSecond();
        public static AnglePerDuration operator *(Number s, AnglePerDuration a) => (a.radiansPerSecond * s).RadiansPerSecond();
        public static AnglePerDuration operator *(AnglePerDuration a, Number s) => (a.radiansPerSecond * s).RadiansPerSecond();
        public static AnglePerDuration operator /(AnglePerDuration a, Number s) => (a.radiansPerSecond / s).RadiansPerSecond();
        public static AnglePerDuration operator *(double s, AnglePerDuration a) => (a.radiansPerSecond * s).RadiansPerSecond();
        public static AnglePerDuration operator *(AnglePerDuration a, double s) => (a.radiansPerSecond * s).RadiansPerSecond();
        public static AnglePerDuration operator /(AnglePerDuration a, double s) => (a.radiansPerSecond / s).RadiansPerSecond();
        public static Angle operator *(AnglePerDuration a, Duration d) => (a.AsRadiansPerSecond() * d.AsSeconds()).Radians();
        public static Duration operator /(Angle a, AnglePerDuration s) => (a.AsRadians() / s.AsRadiansPerSecond()).Seconds();
        public static Number operator /(AnglePerDuration a, AnglePerDuration b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(AnglePerDuration a, AnglePerDuration b) => a.SIValue < b.SIValue;
        public static bool operator >(AnglePerDuration a, AnglePerDuration b) => a.SIValue > b.SIValue;
        public static bool operator <=(AnglePerDuration a, AnglePerDuration b) => a.SIValue <= b.SIValue;
        public static bool operator >=(AnglePerDuration a, AnglePerDuration b) => a.SIValue >= b.SIValue;
        public static bool operator ==(AnglePerDuration a, AnglePerDuration b) => a.SIValue == b.SIValue;
        public static bool operator !=(AnglePerDuration a, AnglePerDuration b) => a.SIValue != b.SIValue;
        
        public override string ToString() => $"{this.AsRadiansPerSecond()} Radians per Second ({this.AsDegreesPerSecond()}°/s)";

        public bool Equals(AnglePerDuration other) => this == other;
        public override bool Equals(object obj) => obj is AnglePerDuration other && this == other;
        public override int GetHashCode() => SIValue.GetHashCode();

    }

    public static class AnglePerDurationExtensions {

        public static AnglePerDuration RadiansPerSecond(this Number value) => new AnglePerDuration(value, AnglePerDurationUnit.RadiansPerSecond);
        public static AnglePerDuration DegreesPerSecond(this Number value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerSecond);
        public static AnglePerDuration DegreesPerMinute(this Number value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerMinute);

        public static AnglePerDuration RadiansPerSecond(this float value) => new AnglePerDuration(value, AnglePerDurationUnit.RadiansPerSecond);
        public static AnglePerDuration DegreesPerSecond(this float value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerSecond);
        public static AnglePerDuration DegreesPerMinute(this float value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerMinute);
        
        public static AnglePerDuration RadiansPerSecond(this double value) => new AnglePerDuration(value, AnglePerDurationUnit.RadiansPerSecond);
        public static AnglePerDuration DegreesPerSecond(this double value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerSecond);
        public static AnglePerDuration DegreesPerMinute(this double value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerMinute);

        public static AnglePerDuration RadiansPerSecond(this int value) => new AnglePerDuration(value, AnglePerDurationUnit.RadiansPerSecond);
        public static AnglePerDuration DegreesPerSecond(this int value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerSecond);
        public static AnglePerDuration DegreesPerMinute(this int value) => new AnglePerDuration(value, AnglePerDurationUnit.DegreesPerMinute);

        public static Number AsRadiansPerSecond(this AnglePerDuration speed) => speed.As(AnglePerDurationUnit.RadiansPerSecond);
        public static Number AsDegreesPerSecond(this AnglePerDuration speed) => speed.As(AnglePerDurationUnit.DegreesPerSecond);
        public static Number AsDegreesPerMinute(this AnglePerDuration speed) => speed.As(AnglePerDurationUnit.DegreesPerMinute);
    }
}