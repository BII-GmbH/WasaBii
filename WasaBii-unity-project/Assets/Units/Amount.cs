using System;
using System.Collections.Generic;
using UnityEngine;

namespace BII.Units {
    
    [Serializable]
    public sealed class AmountUnit : Unit {
        public AmountUnit(string displayName, double factor) : base(displayName, factor) { }
        
        public static readonly AmountUnit Amount = new AmountUnit("Amount", factor: 1f);
        
        public static readonly IReadOnlyList<AmountUnit> All = new []{Amount};
    }

    [Serializable]
    public readonly struct Amount : ValueWithUnit<Amount, AmountUnit> {
        
        public static readonly Amount Zero = new Amount(amount: 0, AmountUnit.Amount);
        
        private readonly int amount;
        public double SIValue => amount;

        public Amount(int amount, AmountUnit unit) => this.amount = (int) (amount * unit.Factor);
        public Amount(double amount, AmountUnit unit) => this.amount = (int) (amount * unit.Factor);

        public Amount CopyWithDifferentSIValue(double newSIValue) => newSIValue;
        CopyableValueWithUnit CopyableValueWithUnit.CopyWithDifferentSIValue(double newSIValue) => 
            CopyWithDifferentSIValue(newSIValue);

        public static Amount operator +(Amount q) => q;
        public static Amount operator -(Amount q) => -q.SIValue;
        public static Amount operator +(Amount a, Amount b) => a.SIValue + b.SIValue;
        public static Amount operator -(Amount a, Amount b) => a.SIValue - b.SIValue;
        public static Amount operator *(Amount a, double s) => a.SIValue * s;
        public static Amount operator *(double s, Amount a) => a.SIValue * s;
        public static Amount operator /(Amount a, double s) => a.SIValue / s;
        public static Number operator /(Amount a, Amount b) => (a.SIValue / b.SIValue).Number();
        public static bool operator <(Amount a, Amount b) => a.SIValue < b.SIValue;
        public static bool operator >(Amount a, Amount b) => a.SIValue > b.SIValue;
        public static bool operator <=(Amount a, Amount b) => a.SIValue <= b.SIValue;
        public static bool operator >=(Amount a, Amount b) => a.SIValue >= b.SIValue;
        public static bool operator ==(Amount a, Amount b) => Math.Abs(a.SIValue - b.SIValue) < double.Epsilon;
        public static bool operator !=(Amount a, Amount b) => Math.Abs(a.SIValue - b.SIValue) > double.Epsilon;
        
        public override string ToString() => $"Amount: {(int) this}";
        
        public bool Equals(Amount other) => this == other;
        public override bool Equals(object obj) => obj is Amount amount && this == amount;
        public override int GetHashCode() => SIValue.GetHashCode();
        public int CompareTo(Amount other) => this > other ? 1 : this < other ? -1 : 0;

        public static implicit operator int(Amount a) => a.amount;
        public static implicit operator Amount(int i) => new Amount(i, AmountUnit.Amount);
        public static implicit operator Amount(double f) => new Amount(f, AmountUnit.Amount);
    }

    public static class AmountExtensions {
        public static Amount Amount(this Number value) => new Amount((int)value, AmountUnit.Amount);
        public static Amount Amount(this int value) => new Amount(value, AmountUnit.Amount);
    }
}