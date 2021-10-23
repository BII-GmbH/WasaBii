using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BII.Units {
    
    [Serializable]
    public sealed class VelocityUnit : Unit {
        private VelocityUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly VelocityUnit MetersPerSecond = new VelocityUnit("m/s", 1f);
        public static readonly VelocityUnit KilometersPerHour = new VelocityUnit("km/h", 1000f / 3600f);

        public static readonly IReadOnlyList<VelocityUnit> All = new []{KilometersPerHour, MetersPerSecond};
    }

    [Serializable]
    public readonly struct Velocity : ValueWithUnit<Velocity, VelocityUnit> {
        
        public static readonly Velocity Zero = new Velocity(velocity: 0f, VelocityUnit.MetersPerSecond);

        [FormerlySerializedAs("_MetersPerSecond")] [SerializeField]
        private double _metersPerSecond;

        public double SIValue => _metersPerSecond;

        public Velocity(double velocity, VelocityUnit unit) => _metersPerSecond = velocity * unit.Factor;

        public Velocity CopyWithDifferentSIValue(double newSIValue) => newSIValue.MetersPerSecond();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Velocity operator +(Velocity v) => v;
        public static Velocity operator -(Velocity v) => (-v.SIValue).MetersPerSecond();
        public static Velocity operator +(Velocity a, Velocity b) => (a.SIValue + b.SIValue).MetersPerSecond();
        public static Velocity operator -(Velocity a, Velocity b) => (a.SIValue - b.SIValue).MetersPerSecond();
        public static Velocity operator *(Velocity a, double s) => (a.SIValue * s).MetersPerSecond();
        public static Velocity operator *(double s, Velocity a) => (a.SIValue * s).MetersPerSecond();
        public static Velocity operator /(Velocity a, double s) => (a.SIValue / s).MetersPerSecond();
        public static Number operator /(Velocity a, Velocity b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Velocity a, Velocity b) => a.SIValue < b.SIValue;
        public static bool operator >(Velocity a, Velocity b) => a.SIValue > b.SIValue;
        public static bool operator <=(Velocity a, Velocity b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Velocity a, Velocity b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Velocity a, Velocity b) => a.SIValue == b.SIValue;
        public static bool operator !=(Velocity a, Velocity b) => a.SIValue != b.SIValue;

        public static Duration operator /(Length l, Velocity v) => (l.AsMeters() / v.AsMetersPerSecond()).Seconds();
        public static Length operator *(Velocity v, Duration d) => (v.AsMetersPerSecond() * d.AsSeconds()).Meters();
        
        public override string ToString() => $"{this.AsMetersPerSecond()} Meters per Second";
        
        public bool Equals(Velocity other) => this == other;
        public override bool Equals(object obj) => obj is Velocity Velocity && this == Velocity;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Velocity other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class VelocityExtensions {

        public static Velocity MetersPerSecond(this Number value) =>
            new Velocity(value, VelocityUnit.MetersPerSecond);
        
        public static Velocity KilometersPerHour(this Number value) =>
            new Velocity(value, VelocityUnit.KilometersPerHour);
        
        public static Velocity MetersPerSecond(this float value) => new Velocity(value, VelocityUnit.MetersPerSecond);
        public static Velocity KilometersPerHour(this float value) => new Velocity(value, VelocityUnit.KilometersPerHour);
        
        public static Velocity MetersPerSecond(this double value) => new Velocity(value, VelocityUnit.MetersPerSecond);
        public static Velocity KilometersPerHour(this double value) => new Velocity(value, VelocityUnit.KilometersPerHour);

        public static Velocity MetersPerSecond(this int value) => new Velocity(value, VelocityUnit.MetersPerSecond);
        public static Velocity KilometersPerHour(this int value) => new Velocity(value, VelocityUnit.KilometersPerHour);

        public static Number AsMetersPerSecond(this ValueWithUnit<VelocityUnit> velocity) => velocity.As(VelocityUnit.MetersPerSecond);

        public static Number AsKilometersPerHour(this ValueWithUnit<VelocityUnit> velocity) => velocity.As(VelocityUnit.KilometersPerHour);

    }
}