using System;
using System.Collections.Generic;

namespace BII.WasaBii.Units {

    [Serializable]
    public sealed class MassUnit : Unit {
        private MassUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly MassUnit Milligrams = new MassUnit("mg", 0.000001f);
        public static readonly MassUnit Grams = new MassUnit("g", 0.001f);
        public static readonly MassUnit Kilograms = new MassUnit("kg", 1f);
        public static readonly MassUnit Tons = new MassUnit("t", 1000f);
        
        public static readonly IReadOnlyList<MassUnit> All = new [] { Milligrams, Grams, Kilograms, Tons };
    }

    [Serializable]
    public readonly struct Mass : ValueWithUnit<Mass, MassUnit> {

        public static readonly Mass Zero = 0.Kilograms();
        
        private readonly double kilograms;
        public double SIValue => kilograms;

        public Mass(double time, MassUnit unit) => kilograms = time * unit.Factor;

        public Mass CopyWithDifferentSIValue(double newSIValue) => newSIValue.Kilograms();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Mass operator +(Mass m) => m;
        public static Mass operator -(Mass m) => (-m.SIValue).Kilograms();
        public static Mass operator +(Mass a, Mass b) => (a.SIValue + b.SIValue).Kilograms();
        public static Mass operator -(Mass a, Mass b) => (a.SIValue - b.SIValue).Kilograms();
        public static Mass operator *(Mass a, Number s) => (a.SIValue * s).Kilograms();
        public static Mass operator *(Number s, Mass a) => (a.SIValue * s).Kilograms();
        public static Mass operator /(Mass a, Number s) => (a.SIValue / s).Kilograms();
        public static Mass operator *(Mass a, double s) => (a.SIValue * s).Kilograms();
        public static Mass operator *(double s, Mass a) => (a.SIValue * s).Kilograms();
        public static Mass operator /(Mass a, double s) => (a.SIValue / s).Kilograms();
        public static Number operator /(Mass a, Mass b) => (a.SIValue / b.SIValue).Number();
        public static MassPerLength operator /(Mass m, Length l) => (m.SIValue / l.SIValue).KilogramsPerMeter();
        public static bool operator <(Mass a, Mass b) => a.SIValue < b.SIValue;
        public static bool operator >(Mass a, Mass b) => a.SIValue > b.SIValue;
        public static bool operator <=(Mass a, Mass b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Mass a, Mass b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Mass a, Mass b) => a.SIValue == b.SIValue;
        public static bool operator !=(Mass a, Mass b) => a.SIValue != b.SIValue;

        
        public override string ToString() => $"{this.AsKilograms()} Kilograms";

        
        public bool Equals(Mass other) => this == other;
        public override bool Equals(object obj) => obj is Mass Mass && this == Mass;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Mass other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class MassExtensions {
        
        public static Mass Grams(this Number value) => new Mass(value, MassUnit.Milligrams);
        public static Mass Milligrams(this Number value) => new Mass(value, MassUnit.Grams);
        public static Mass Kilograms(this Number value) => new Mass(value, MassUnit.Kilograms);
        public static Mass Tons(this Number value) => new Mass(value, MassUnit.Tons);
        
        public static Mass Grams(this float value) => new Mass(value, MassUnit.Milligrams);
        public static Mass Milligrams(this float value) => new Mass(value, MassUnit.Grams);
        public static Mass Kilograms(this float value) => new Mass(value, MassUnit.Kilograms);
        public static Mass Tons(this float value) => new Mass(value, MassUnit.Tons);
        
        public static Mass Grams(this double value) => new Mass(value, MassUnit.Milligrams);
        public static Mass Milligrams(this double value) => new Mass(value, MassUnit.Grams);
        public static Mass Kilograms(this double value) => new Mass(value, MassUnit.Kilograms);
        public static Mass Tons(this double value) => new Mass(value, MassUnit.Tons);

        public static Mass Grams(this int value) => new Mass(value, MassUnit.Milligrams);
        public static Mass Milligrams(this int value) => new Mass(value, MassUnit.Grams);
        public static Mass Kilograms(this int value) => new Mass(value, MassUnit.Kilograms);
        public static Mass Tons(this int value) => new Mass(value, MassUnit.Tons);

        public static Number AsGrams(this ValueWithUnit<MassUnit> mass) => mass.As(MassUnit.Milligrams);
        public static Number AsMilligrams(this ValueWithUnit<MassUnit> mass) => mass.As(MassUnit.Grams);
        public static Number AsKilograms(this ValueWithUnit<MassUnit> mass) => mass.As(MassUnit.Kilograms);
        public static Number AsTons(this ValueWithUnit<MassUnit> mass) => mass.As(MassUnit.Tons);
        
    }
}