using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable]
    public sealed class VolumePerDurationUnit : Unit {
        [JsonConstructor]
        private VolumePerDurationUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly VolumePerDurationUnit CubicMetersPerSecond =
            new VolumePerDurationUnit("m³/s", factor: 1f);

        public static readonly IReadOnlyList<VolumePerDurationUnit> All = new[] {CubicMetersPerSecond};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct VolumePerDuration : ValueWithUnit<VolumePerDuration, VolumePerDurationUnit> {
        
        public IReadOnlyList<VolumePerDurationUnit> AllUnits => VolumePerDurationUnit.All;
        public VolumePerDurationUnit DisplayUnit => VolumePerDurationUnit.CubicMetersPerSecond;
        public VolumePerDurationUnit SIUnit => VolumePerDurationUnit.CubicMetersPerSecond;

        public static readonly VolumePerDuration Zero = new(
            volumePerDuration: 0,
            VolumePerDurationUnit.CubicMetersPerSecond
        );

        private readonly double cubicMetersPerSecond;

        public double SIValue => cubicMetersPerSecond;

        public VolumePerDuration(double volumePerDuration, VolumePerDurationUnit unit) =>
            cubicMetersPerSecond = volumePerDuration * unit.Factor;

        public VolumePerDuration CopyWithDifferentSIValue(double newSIValue) => newSIValue.CubicMetersPerSecond();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static VolumePerDuration operator +(VolumePerDuration vpd) => vpd;
        public static VolumePerDuration operator -(VolumePerDuration vpd) => (-vpd.SIValue).CubicMetersPerSecond();

        public static VolumePerDuration operator +(VolumePerDuration a, VolumePerDuration b) =>
            (a.SIValue + b.SIValue).CubicMetersPerSecond();

        public static VolumePerDuration operator -(VolumePerDuration a, VolumePerDuration b) =>
            (a.SIValue - b.SIValue).CubicMetersPerSecond();

        public static VolumePerDuration operator *(VolumePerDuration a, double s) =>
            (a.SIValue * s).CubicMetersPerSecond();

        public static VolumePerDuration operator *(double s, VolumePerDuration a) => a * s;

        public static VolumePerDuration operator /(VolumePerDuration a, double s) =>
            (a.SIValue / s).CubicMetersPerSecond();

        public static Number operator /(VolumePerDuration a, VolumePerDuration b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(VolumePerDuration a, VolumePerDuration b) => a.SIValue < b.SIValue;
        public static bool operator >(VolumePerDuration a, VolumePerDuration b) => a.SIValue > b.SIValue;
        public static bool operator <=(VolumePerDuration a, VolumePerDuration b) => a.SIValue <= b.SIValue;
        public static bool operator >=(VolumePerDuration a, VolumePerDuration b) => a.SIValue >= b.SIValue;
        public static bool operator ==(VolumePerDuration a, VolumePerDuration b) => a.SIValue.Equals(b.SIValue);
        public static bool operator !=(VolumePerDuration a, VolumePerDuration b) => !a.Equals(b);

        public static Duration operator /(Volume v, VolumePerDuration vpd) =>
            (v.AsCubicMeter() / vpd.AsCubicMetersPerSecond()).Seconds();

        public static Volume operator *(Duration d, VolumePerDuration vpd) =>
            (d.AsSeconds() * vpd.AsCubicMetersPerSecond()).CubicMeter();

        public static Volume operator *(VolumePerDuration mpl, Duration d) => d * mpl;

        public static VolumePerDuration Lerp(VolumePerDuration a, VolumePerDuration b, double t)
            => Mathd.Lerp(a.cubicMetersPerSecond, b.cubicMetersPerSecond, t).CubicMetersPerSecond();

        public static VolumePerDuration Max(VolumePerDuration a, VolumePerDuration b) =>
            Math.Max(a.cubicMetersPerSecond, b.cubicMetersPerSecond).CubicMetersPerSecond();

        public static VolumePerDuration Min(VolumePerDuration a, VolumePerDuration b) =>
            Math.Min(a.cubicMetersPerSecond, b.cubicMetersPerSecond).CubicMetersPerSecond();

        public override string ToString() => $"{this.AsCubicMetersPerSecond()} Cubic Meters per Second";

        public bool Equals(VolumePerDuration other) => this == other;

        public override bool Equals(object obj) =>
            obj is VolumePerDuration VolumePerDuration && this == VolumePerDuration;

        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(VolumePerDuration other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class VolumePerDurationExtensions {
        public static VolumePerDuration CubicMetersPerSecond(this float value) =>
            new VolumePerDuration(value, VolumePerDurationUnit.CubicMetersPerSecond);
        public static VolumePerDuration CubicMetersPerSecond(this Number value) =>
            new VolumePerDuration(value, VolumePerDurationUnit.CubicMetersPerSecond);
        
        public static VolumePerDuration CubicMetersPerSecond(this double value) =>
            new VolumePerDuration(value, VolumePerDurationUnit.CubicMetersPerSecond);

        public static VolumePerDuration CubicMetersPerSecond(this int value) =>
            new VolumePerDuration(value, VolumePerDurationUnit.CubicMetersPerSecond);

        public static Number AsCubicMetersPerSecond(this ValueWithUnit<VolumePerDurationUnit> volumePerDuration) =>
            volumePerDuration.As(VolumePerDurationUnit.CubicMetersPerSecond);
    }
}