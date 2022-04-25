using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using Newtonsoft.Json;

namespace BII.WasaBii.Units {
    
    [JsonObject(IsReference = false)] // Treat as value type for serialization
    [MustBeSerializable]
    public sealed class ForceUnit : Unit {
        [JsonConstructor] private ForceUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly ForceUnit Newton = new ForceUnit("N", factor: 1f);

        public static readonly IReadOnlyList<ForceUnit> All = new[] {Newton};
    }

    [Serializable]
    [MustBeSerializable]
    public readonly struct Force : ValueWithUnit<Force, ForceUnit> {
        
        public IReadOnlyList<ForceUnit> AllUnits => ForceUnit.All;
        public ForceUnit DisplayUnit => ForceUnit.Newton;
        public ForceUnit SIUnit => ForceUnit.Newton;

        public static readonly Force Zero = new(force: 0, ForceUnit.Newton);

        private readonly double newton;

        public double SIValue => newton;

        public Force(double force, ForceUnit unit) => newton = force * unit.Factor;

        public Force CopyWithDifferentSIValue(double newSIValue) => newSIValue.Newton();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Force operator +(Force v) => v;
        public static Force operator -(Force v) => (-v.SIValue).Newton();
        public static Force operator +(Force a, Force b) => (a.SIValue + b.SIValue).Newton();
        public static Force operator -(Force a, Force b) => (a.SIValue - b.SIValue).Newton();
        public static Force operator *(Force a, double s) => (a.SIValue * s).Newton();
        public static Force operator *(double s, Force a) => (a.SIValue * s).Newton();
        public static Force operator /(Force a, double s) => (a.SIValue / s).Newton();
        public static Number operator /(Force a, Force b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Force a, Force b) => a.SIValue < b.SIValue;
        public static bool operator >(Force a, Force b) => a.SIValue > b.SIValue;
        public static bool operator <=(Force a, Force b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Force a, Force b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Force a, Force b) => a.SIValue.Equals(b.SIValue);
        public static bool operator !=(Force a, Force b) => !a.Equals(b);

        public static Force Lerp(Force a, Force b, double t) => Mathd.Lerp(a.newton, b.newton, t).Newton();

        public static Force Max(Force a, Force b) => Mathd.Max(a.newton, b.newton).Newton();

        public static Force Min(Force a, Force b) => Mathd.Min(a.newton, b.newton).Newton();

        public override string ToString() => $"{this.AsNewton()} Newton";

        public bool Equals(Force other) => this == other;
        public override bool Equals(object obj) => obj is Force Force && this == Force;
        public override int GetHashCode() => SIValue.GetHashCode();
        
        public int CompareTo(Force other) => SIValue.CompareTo(other.SIValue);
    }

    public static class ForceExtensions {
        public static Force Newton(this Number value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this float value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this double value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this int value) => new Force(value, ForceUnit.Newton);
        public static Number AsNewton(this Force force) => force.As(ForceUnit.Newton);
    }
}