using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable]
    public sealed class MassPerLengthUnit : Unit {
        [JsonConstructor] private MassPerLengthUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly MassPerLengthUnit KilogramsPerMeter = new MassPerLengthUnit("kg/m", factor: 1f);

        public static readonly IReadOnlyList<MassPerLengthUnit> All = new[] {KilogramsPerMeter};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct MassPerLength : ValueWithUnit<MassPerLength, MassPerLengthUnit> {
        
        public IReadOnlyList<MassPerLengthUnit> AllUnits => MassPerLengthUnit.All;
        public MassPerLengthUnit DisplayUnit => MassPerLengthUnit.KilogramsPerMeter;
        public MassPerLengthUnit SIUnit => MassPerLengthUnit.KilogramsPerMeter;

        public static readonly MassPerLength Zero = new(
            massPerLength: 0,
            MassPerLengthUnit.KilogramsPerMeter
        );

        private readonly double kilogramsPerMeter;

        public double SIValue => kilogramsPerMeter;

        public MassPerLength(double massPerLength, MassPerLengthUnit unit) =>
            kilogramsPerMeter = massPerLength * unit.Factor;

        public MassPerLength CopyWithDifferentSIValue(double newSIValue) => newSIValue.KilogramsPerMeter();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static MassPerLength operator +(MassPerLength mpl) => mpl;
        public static MassPerLength operator -(MassPerLength mpl) => (-mpl.SIValue).KilogramsPerMeter();

        public static MassPerLength operator +(MassPerLength a, MassPerLength b) =>
            (a.SIValue + b.SIValue).KilogramsPerMeter();

        public static MassPerLength operator -(MassPerLength a, MassPerLength b) =>
            (a.SIValue - b.SIValue).KilogramsPerMeter();

        public static MassPerLength operator *(MassPerLength a, double s) => (a.SIValue * s).KilogramsPerMeter();
        public static MassPerLength operator *(double s, MassPerLength a) => a * s;
        public static MassPerLength operator /(MassPerLength a, double s) => (a.SIValue / s).KilogramsPerMeter();

        public static Number operator /(MassPerLength a, MassPerLength b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(MassPerLength a, MassPerLength b) => a.SIValue < b.SIValue;
        public static bool operator >(MassPerLength a, MassPerLength b) => a.SIValue > b.SIValue;
        public static bool operator <=(MassPerLength a, MassPerLength b) => a.SIValue <= b.SIValue;
        public static bool operator >=(MassPerLength a, MassPerLength b) => a.SIValue >= b.SIValue;
        public static bool operator ==(MassPerLength a, MassPerLength b) => a.SIValue.Equals(b.SIValue);
        public static bool operator !=(MassPerLength a, MassPerLength b) => !a.Equals(b);

        public static Length operator /(Mass m, MassPerLength mpl) =>
            (m.AsKilograms() / mpl.AsKilogramsPerMeter()).Meters();

        public static Mass operator *(Length l, MassPerLength mpl) =>
            (l.AsMeters() * mpl.AsKilogramsPerMeter()).Kilograms();

        public static Mass operator *(MassPerLength mpl, Length l) => l * mpl;

        public static MassPerLength Lerp(MassPerLength a, MassPerLength b, double t)
            => Mathd.Lerp(a.kilogramsPerMeter, b.kilogramsPerMeter, t).KilogramsPerMeter();

        public static MassPerLength Max(MassPerLength a, MassPerLength b) =>
            Math.Max(a.kilogramsPerMeter, b.kilogramsPerMeter).KilogramsPerMeter();

        public static MassPerLength Min(MassPerLength a, MassPerLength b) =>
            Math.Min(a.kilogramsPerMeter, b.kilogramsPerMeter).KilogramsPerMeter();

        public override string ToString() => $"{this.AsKilogramsPerMeter()} Kilograms per Meter";

        public bool Equals(MassPerLength other) => this == other;
        public override bool Equals(object obj) => obj is MassPerLength MassPerLength && this == MassPerLength;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(MassPerLength other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class MassPerLengthExtensions {
        public static MassPerLength KilogramsPerMeter(this Number value) =>
            new MassPerLength(value, MassPerLengthUnit.KilogramsPerMeter);
        public static MassPerLength KilogramsPerMeter(this float value) =>
            new MassPerLength(value, MassPerLengthUnit.KilogramsPerMeter);
        
        public static MassPerLength KilogramsPerMeter(this double value) =>
            new MassPerLength(value, MassPerLengthUnit.KilogramsPerMeter);

        public static MassPerLength KilogramsPerMeter(this int value) =>
            new MassPerLength(value, MassPerLengthUnit.KilogramsPerMeter);

        public static Number AsKilogramsPerMeter(this ValueWithUnit<MassPerLengthUnit> massPerLength) =>
            massPerLength.As(MassPerLengthUnit.KilogramsPerMeter);
        
    }
}