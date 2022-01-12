using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {

    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class VolumeUnit : Unit {

        [JsonConstructor]
        private VolumeUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly VolumeUnit Liter = new VolumeUnit("l", 0.001f);
        public static readonly VolumeUnit CubicMeter = new VolumeUnit("m³", 1f);

        public static readonly IReadOnlyList<VolumeUnit> All = new []{CubicMeter, Liter};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Volume : ValueWithUnit<Volume, VolumeUnit> {

        public IReadOnlyList<VolumeUnit> AllUnits => VolumeUnit.All;
        public VolumeUnit DisplayUnit => VolumeUnit.CubicMeter;
        public VolumeUnit SIUnit => VolumeUnit.CubicMeter;

        public static readonly Volume Zero = new(0, VolumeUnit.CubicMeter);

        private readonly double cubicMeter;

        public double SIValue => cubicMeter;

        public Volume(double time, VolumeUnit unit) => cubicMeter = time * unit.Factor;

        public Volume CopyWithDifferentSIValue(double newSIValue) => newSIValue.CubicMeter();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Volume operator +(Volume v) => v;
        public static Volume operator -(Volume v) => (-v.SIValue).CubicMeter();
        public static Volume operator +(Volume a, Volume b) => (a.SIValue + b.SIValue).CubicMeter();
        public static Volume operator -(Volume a, Volume b) => (a.SIValue - b.SIValue).CubicMeter();
        public static Volume operator *(Volume a, double s) => (a.SIValue * s).CubicMeter();
        public static Volume operator *(double s, Volume a) => (a.SIValue * s).CubicMeter();
        public static Volume operator /(Volume a, double s) => (a.SIValue / s).CubicMeter();
        public static Number operator /(Volume a, Volume b) => (a.SIValue / b.SIValue).Number();
        
        public static VolumePerDuration operator /(Volume v, Duration d) => 
            (v.AsCubicMeter() / d.AsSeconds()).CubicMetersPerSecond();
        public static Area operator /(Volume v, Length l) => (v.AsCubicMeter() / l.AsMeters()).SquareMeters();
        public static Length operator /(Volume v, Area a) => (v.AsCubicMeter() / a.AsSquareMeters()).Meters();
        public static bool operator <(Volume a, Volume b) => a.SIValue < b.SIValue;
        public static bool operator >(Volume a, Volume b) => a.SIValue > b.SIValue;
        public static bool operator <=(Volume a, Volume b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Volume a, Volume b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Volume a, Volume b) => a.SIValue == b.SIValue;
        public static bool operator !=(Volume a, Volume b) => a.SIValue != b.SIValue;

        public static Volume Lerp(Volume a, Volume b, double t)
            => Mathd.Lerp(a.cubicMeter, b.cubicMeter, t).CubicMeter();

        public static Volume Max(Volume a, Volume b) => Math.Max(a.cubicMeter, b.cubicMeter).CubicMeter();
        
        public static Volume Min(Volume a, Volume b) => Math.Min(a.cubicMeter, b.cubicMeter).CubicMeter();

        public override string ToString() => $"{this.AsCubicMeter()} Cubic Meters";
        
        public bool Equals(Volume other) => this == other;
        public override bool Equals(object obj) => obj is Volume Volume && this == Volume;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Volume other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class VolumeExtensions {
        
        public static Volume Liter(this Number value) => new Volume(value, VolumeUnit.Liter);
        public static Volume CubicMeter(this Number value) => new Volume(value, VolumeUnit.CubicMeter);
        
        public static Volume Liter(this float value) => new Volume(value, VolumeUnit.Liter);
        public static Volume CubicMeter(this float value) => new Volume(value, VolumeUnit.CubicMeter);
        
        public static Volume Liter(this double value) => new Volume(value, VolumeUnit.Liter);
        public static Volume CubicMeter(this double value) => new Volume(value, VolumeUnit.CubicMeter);

        public static Volume Liter(this int value) => new Volume(value, VolumeUnit.Liter);
        public static Volume CubicMeter(this int value) => new Volume(value, VolumeUnit.CubicMeter);

        public static Number AsLiter(this ValueWithUnit<VolumeUnit> volume) => volume.As(VolumeUnit.Liter);
        public static Number AsCubicMeter(this ValueWithUnit<VolumeUnit> volume) => volume.As(VolumeUnit.CubicMeter);
    }
}