using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {

    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class LengthUnit : Unit {

        [JsonConstructor]
        private LengthUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly LengthUnit Millimeter = new LengthUnit("mm", 0.001f);
        public static readonly LengthUnit Meter = new LengthUnit("m", 1f);
        public static readonly LengthUnit Kilometer = new LengthUnit("km", 1000f);
        
        public static readonly IReadOnlyList<LengthUnit> All = new []{Millimeter, Meter, Kilometer};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Length : ValueWithUnit<Length, LengthUnit> {

        public IReadOnlyList<LengthUnit> AllUnits => LengthUnit.All;
        public LengthUnit DisplayUnit => LengthUnit.Meter;
        public LengthUnit SIUnit => LengthUnit.Meter;

        public static readonly Length Zero = new(0, LengthUnit.Meter);
        public static readonly Length MaxValue = new(double.MaxValue, LengthUnit.Meter);
        public static readonly Length Epsilon = 1.Millimeters();
        
        private readonly double meter;

        public double SIValue => meter;

        public Length(double meter, LengthUnit unit) => this.meter = meter * unit.Factor;

        public Length CopyWithDifferentSIValue(double newSIValue) => newSIValue.Meters();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Length operator +(Length l) => l;
        public static Length operator -(Length l) => (-l.SIValue).Meters();
        public static Length operator +(Length a, Length b) => (a.SIValue + b.SIValue).Meters();
        public static Length operator -(Length a, Length b) => (a.SIValue - b.SIValue).Meters();
        public static Length operator *(double s, Length a) => (a.SIValue * s).Meters();
        public static Length operator *(Length a, double s) => (a.SIValue * s).Meters();
        public static Area operator *(Length a, Length b) => (a.SIValue * b.SIValue).SquareMeters();
        public static Length operator /(Length a, double s) => (a.SIValue / s).Meters();
        public static Length operator %(Length a, Length b) => (a.SIValue % b.SIValue).Meters();
        
        public static Number operator /(Length a, Length b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Length a, Length b) => a.SIValue < b.SIValue;
        public static bool operator >(Length a, Length b) => a.SIValue > b.SIValue;
        public static bool operator <=(Length a, Length b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Length a, Length b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Length a, Length b) => a.SIValue == b.SIValue;
        public static bool operator !=(Length a, Length b) => a.SIValue != b.SIValue;

        public static Velocity operator /(Length l, Duration d) => (l.AsMeters() / d.AsSeconds()).MetersPerSecond();
        
        public override string ToString() => $"{this.AsMeters()} Meters";

        public bool Equals(Length other) => this == other;
        public override bool Equals(object obj) => obj is Length Length && this == Length;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Length other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class LengthExtensions {
        public static Length Millimeters(this Number value) => new Length(value, LengthUnit.Millimeter);
        public static Length Meters(this Number value) => new Length(value, LengthUnit.Meter);
        public static Length Kilometers(this Number value) => new Length(value, LengthUnit.Kilometer);

        public static Length Millimeters(this float value) => new Length(value, LengthUnit.Millimeter);
        public static Length Meters(this float value) => new Length(value, LengthUnit.Meter);
        public static Length Kilometers(this float value) => new Length(value, LengthUnit.Kilometer);

        public static Length Millimeters(this double value) => new Length(value, LengthUnit.Millimeter);
        public static Length Meters(this double value) => new Length(value, LengthUnit.Meter);
        public static Length Kilometers(this double value) => new Length(value, LengthUnit.Kilometer);

        public static Length Millimeters(this int value) => new Length(value, LengthUnit.Millimeter);
        public static Length Meters(this int value) => new Length(value, LengthUnit.Meter);
        public static Length Kilometers(this int value) => new Length(value, LengthUnit.Kilometer);

        public static Number AsMillimeters(this ValueWithUnit<LengthUnit> length) => length.As(LengthUnit.Millimeter);
        public static Number AsMeters(this ValueWithUnit<LengthUnit> length) => length.As(LengthUnit.Meter);
        public static Number AsKilometers(this ValueWithUnit<LengthUnit> length) => length.As(LengthUnit.Kilometer);
    }
}