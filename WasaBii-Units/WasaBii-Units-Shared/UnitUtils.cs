using System.Diagnostics.Contracts;
using System.Reflection;

namespace BII.WasaBii.Units;

// TODO CR: generate when done



[UnitMetadata(typeof(NumberUnitDesc))]
public sealed class NumberUnit : IUnit.Base {
    public string LongName => "Thing";
    public string ShortName => "";
    public double SiFactor => 1;

    public sealed class NumberUnitDesc : IUnitDescription<NumberUnit> {
        public NumberUnit SiUnit => Instance;
    }

    private NumberUnit() {}
    public static readonly NumberUnit Instance = new();
}

public readonly struct Number : IUnitValue<Number, NumberUnit> {
    public double SiValue { init; get; }
    public Type UnitType => typeof(NumberUnit);

    public Number(double value) => SiValue = value;

    public int CompareTo(Number other) => this.SiValue.CompareTo(other.SiValue);
    public bool Equals(Number other) => this.SiValue.Equals(other.SiValue);

    public override bool Equals(object? obj) {
        return obj is Number other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(SiValue, typeof(Number));
    }
}


// TODO CR: this is WasaBii code that uses code generation

public static class Units {
    public static TValue FromSiValue<TValue>(double value) where TValue : IUnitValue, new() => new TValue {SiValue = value};

    public static double As<TValue, TUnit>(this TValue value, TUnit unit)
        where TValue : IUnitValue<TUnit> where TUnit : IUnit => value.SiValue * unit.SiFactor;

    public static double As(this IUnitValue value, IUnit unit) {
        Contract.Assert(value.UnitType.IsInstanceOfType(unit));
        return value.SiValue * unit.SiFactor;
    }
    
    // Conditions validated in Unit base constructor

    public static TUnit SiUnitOf<TUnit>() where TUnit : IUnit =>
        ((IUnitDescription<TUnit>) Activator.CreateInstance(typeof(TUnit)
            .GetCustomAttribute<UnitMetadataAttribute>().UnitDescriptionType)).SiUnit;

    // TODO: the other unit utilities from dProB...
}