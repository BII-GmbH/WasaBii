using System;
using System.Collections.Generic;

namespace BII.Units {

    [Serializable]
    public sealed class NumberUnit : Unit {
        private NumberUnit(string displayName, double factor) : base(displayName, factor) { }

        public static readonly NumberUnit NoneUnit = new NumberUnit("", 1.0);
        public static readonly IReadOnlyList<NumberUnit> All = new []{NoneUnit};
    }

    [Serializable]
    public readonly struct Number : ValueWithUnit<Number, NumberUnit>, IFormattable{
        
        public static readonly Number Zero = new Number(number: 0.0, NumberUnit.NoneUnit);
        
        private readonly double number;
        public double SIValue => number;

        public Number(double number, NumberUnit unit) => this.number = number * unit.Factor;

        public Number CopyWithDifferentSIValue(double newSIValue) => newSIValue.Number();
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Number operator +(Number v) => v;
        public static Number operator -(Number v) => (-v.SIValue).Number();
        public static Number operator +(Number a, Number b) => (a.SIValue + b.SIValue).Number();
        public static Number operator +(double a, Number b) => (a + b.SIValue).Number();
        public static Number operator +(Number a, double b) => (a.SIValue + b).Number();
        public static Number operator -(Number a, Number b) => (a.SIValue - b.SIValue).Number();
        public static Number operator -(double a, Number b) => (a - b.SIValue).Number();
        public static Number operator -(Number a, double b) => (a.SIValue - b).Number();
        public static Number operator *(Number a, Number s) => (a.SIValue * s.SIValue).Number();
        public static Number operator /(Number a, Number s) => (a.SIValue / s.SIValue).Number();
        public static Number operator *(Number a, double s) => (a.SIValue * s).Number();
        public static Number operator *(double s, Number a) => (a.SIValue * s).Number();
        public static Number operator /(Number a, double s) => (a.SIValue / s).Number();
        public static Number operator /(double a, Number s) => (a / s.SIValue).Number();
        public static bool operator <(Number a, Number b) => a.SIValue < b.SIValue;
        public static bool operator >(Number a, Number b) => a.SIValue > b.SIValue;
        public static bool operator <=(Number a, Number b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Number a, Number b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Number a, Number b) => a.SIValue == b.SIValue;
        public static bool operator !=(Number a, Number b) => a.SIValue != b.SIValue;
        public static bool operator <(Number a, double b) => a.SIValue < b;
        public static bool operator >(Number a, double b) => a.SIValue > b;
        public static bool operator <=(Number a, double b) => a.SIValue <= b;
        public static bool operator >=(Number a, double b) => a.SIValue >= b;
        public static bool operator ==(Number a, double b) => a.SIValue == b;
        public static bool operator !=(Number a, double b) => a.SIValue != b;
        
        
        public static implicit operator double(Number number) => number.SIValue;

        public override string ToString() => $"{(double)this}";
        public string ToString(IFormatProvider formatProvider) => ((double)this).ToString(formatProvider);
        public string ToString(String str, IFormatProvider formatProvider) => ((double)this).ToString(str, formatProvider);
        
        public bool Equals(Number other) => this == other;
        public override bool Equals(object obj) => obj is Number Number && this == Number;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Number other) => (this > other) ? 1 : ((this < other) ? -1 : 0);
    }

    public static class NumberExtensions {
        
        public static Number Number(this float value) => new Number(value, NumberUnit.NoneUnit);
        public static Number Number(this double value) => new Number(value, NumberUnit.NoneUnit);
        
    }
}