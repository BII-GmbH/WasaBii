using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {

    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable] 
    public sealed class AreaUnit : Unit {

        [JsonConstructor]
        private AreaUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly AreaUnit SquareMeter = new AreaUnit("m²", 1f);
        public static readonly AreaUnit Hectare = new AreaUnit("ha", 10000f);
        
        public static readonly IReadOnlyList<AreaUnit> All = new []{SquareMeter, Hectare};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Area : ValueWithUnit<Area, AreaUnit> {
        
        public IReadOnlyList<AreaUnit> AllUnits => AreaUnit.All;
        public AreaUnit DisplayUnit => AreaUnit.SquareMeter;
        public AreaUnit SIUnit => AreaUnit.SquareMeter;

        public static readonly Area Zero = new(0, AreaUnit.SquareMeter);
        public static readonly Area MaxValue = new(double.MaxValue, AreaUnit.SquareMeter);
        public static readonly Area Epsilon = Length.Epsilon * Length.Epsilon;
        
        private readonly double sqrMeter;

        public double SIValue => sqrMeter;

        public Area(double sqrMeter, AreaUnit unit) => this.sqrMeter = sqrMeter * unit.Factor;

        public Area CopyWithDifferentSIValue(double newSIValue) => newSIValue.SquareMeters();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Area operator +(Area a) => a;
        public static Area operator -(Area a) => (-a.SIValue).SquareMeters();
        public static Area operator +(Area a, Area b) => (a.SIValue + b.SIValue).SquareMeters();
        public static Area operator -(Area a, Area b) => (a.SIValue - b.SIValue).SquareMeters();
        public static Area operator *(double s, Area a) => (a.SIValue * s).SquareMeters();
        public static Area operator *(Area a, double s) => (a.SIValue * s).SquareMeters();
        public static Area operator /(Area a, double s) => (a.SIValue / s).SquareMeters();
        public static Area operator %(Area a, Area b) => (a.SIValue % b.SIValue).SquareMeters();
        
        public static Number operator /(Area a, Area b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Area a, Area b) => a.SIValue < b.SIValue;
        public static bool operator >(Area a, Area b) => a.SIValue > b.SIValue;
        public static bool operator <=(Area a, Area b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Area a, Area b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Area a, Area b) => a.SIValue == b.SIValue;
        public static bool operator !=(Area a, Area b) => a.SIValue != b.SIValue;

        public static Length operator /(Area a, Length l) => (a.AsSquareMeters() / l.AsMeters()).Meters();
        public static Volume operator *(Area a, Length l) => (a.AsSquareMeters() * l.AsMeters()).CubicMeter();
        public static Volume operator *(Length l, Area a) => a * l;
        
        public override string ToString() => $"{this.AsSquareMeters()} m²";

        public bool Equals(Area other) => this == other;
        public override bool Equals(object obj) => obj is Area area && this == area;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Area other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }
    
    public static class AreaExtensions {
        public static Area SquareMeters(this Number value) => new Area(value, AreaUnit.SquareMeter);
        public static Area Hectares(this Number value) => new Area(value, AreaUnit.Hectare);

        public static Area SquareMeters(this float value) => new Area(value, AreaUnit.SquareMeter);
        public static Area Hectares(this float value) => new Area(value, AreaUnit.Hectare);

        public static Area SquareMeters(this double value) => new Area(value, AreaUnit.SquareMeter);
        public static Area Hectares(this double value) => new Area(value, AreaUnit.Hectare);

        public static Area SquareMeters(this int value) => new Area(value, AreaUnit.SquareMeter);
        public static Area Hectares(this int value) => new Area(value, AreaUnit.Hectare);

        public static Number AsSquareMeters(this ValueWithUnit<AreaUnit> area) => area.As(AreaUnit.SquareMeter);
        public static Number AsHectares(this ValueWithUnit<AreaUnit> area) => area.As(AreaUnit.Hectare);
    }
}
