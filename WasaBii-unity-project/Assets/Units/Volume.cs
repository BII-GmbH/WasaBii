using System;
using System.Collections.Generic;
using UnityEngine;

namespace BII.Units {
    
    [Serializable]
    public sealed class VolumeUnit : Unit {
        private VolumeUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly VolumeUnit Liter = new VolumeUnit("l", 0.001f);
        public static readonly VolumeUnit CubicMeter = new VolumeUnit("m³", 1f);

        public static readonly IReadOnlyList<VolumeUnit> All = new []{CubicMeter, Liter};
    }

    [Serializable]
    public readonly struct Volume : ValueWithUnit<Volume, VolumeUnit> {

        public static readonly Volume Zero = new Volume(0, VolumeUnit.CubicMeter);

        [SerializeField]
        private double _cubicMeter;

        public double SIValue => _cubicMeter;

        public Volume(double time, VolumeUnit unit) => _cubicMeter = time * unit.Factor;

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
        public static bool operator <(Volume a, Volume b) => a.SIValue < b.SIValue;
        public static bool operator >(Volume a, Volume b) => a.SIValue > b.SIValue;
        public static bool operator <=(Volume a, Volume b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Volume a, Volume b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Volume a, Volume b) => a.SIValue == b.SIValue;
        public static bool operator !=(Volume a, Volume b) => a.SIValue != b.SIValue;

        public static Volume Lerp(Volume a, Volume b, double t)
            => Mathd.Lerp(a._cubicMeter, b._cubicMeter, t).CubicMeter();

        public static Volume Max(Volume a, Volume b) => Math.Max(a._cubicMeter, b._cubicMeter).CubicMeter();
        
        public static Volume Min(Volume a, Volume b) => Math.Min(a._cubicMeter, b._cubicMeter).CubicMeter();

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

        public static Number AsLiter(this Volume volume) => volume.As(VolumeUnit.Liter);
        public static Number AsCubicMeter(this Volume volume) => volume.As(VolumeUnit.CubicMeter);
    }
}