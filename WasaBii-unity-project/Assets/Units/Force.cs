using System;
using System.Collections.Generic;

namespace BII.Units {
    
    [Serializable]
    public sealed class ForceUnit : Unit {
        private ForceUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly ForceUnit Newton = new ForceUnit("N", factor: 1f);

        public static readonly IReadOnlyList<ForceUnit> All = new[] {Newton};
    }

    [Serializable]
    public readonly struct Force : ValueWithUnit<Force, ForceUnit> {
        public static readonly Force Zero = new Force(force: 0, ForceUnit.Newton);

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

        // public static Force Lerp(Force a, Force b, double t) => Mathd.Lerp(a._newton, b._newton, t).Newton();
        //
        // public static Force Max(Force a, Force b) => Mathd.Max(a._newton, b._newton).Newton();
        //
        // public static Force Min(Force a, Force b) => Mathd.Min(a._newton, b._newton).Newton();

        public override string ToString() => $"{this.AsNewton()} Newton";

        public bool Equals(Force other) => this == other;
        public override bool Equals(object obj) => obj is Force Force && this == Force;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Force other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class ForceExtensions {
        public static Force Newton(this Number value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this float value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this double value) => new Force(value, ForceUnit.Newton);
        public static Force Newton(this int value) => new Force(value, ForceUnit.Newton);
        public static Number AsNewton(this ValueWithUnit<ForceUnit> force) => force.As(ForceUnit.Newton);
    }
}