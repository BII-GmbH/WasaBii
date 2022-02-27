using System.Diagnostics.Contracts;
using System.Reflection;

namespace BII.WasaBii.Units;

// TODO CR: generate when done

[UnitMetadata(typeof(NumberUnitDesc))]
public sealed class NumberUnit : Unit.Base {
    public override string DisplayName => "";
    public override double SiFactor => 1;

    public sealed class NumberUnitDesc : UnitDescription<NumberUnit> {
        public NumberUnit SiUnit => Instance;
        public NumberUnit DisplayUnit => Instance;
    }

    private NumberUnit() {}
    public static NumberUnit Instance = new();
}

public readonly struct Number : UnitValue<Number, NumberUnit> {
    public double SiValue { init; get; }
    public Type UnitType => typeof(NumberUnit);

    public Number(double value) => SiValue = value;

    public int CompareTo(Number other) => other.SiValue.CompareTo(this.SiValue);
    public bool Equals(Number other) => other.SiValue.Equals(this.SiValue);
}


// TODO CR: this is WasaBii code that uses code generation

public static class Units {
    public static TValue FromSiValue<TValue>(double value) where TValue : UnitValue, new() => new TValue {SiValue = 5};

    public static Number As<TValue, TUnit>(this TValue value, TUnit unit)
        where TValue : UnitValue<TUnit> where TUnit : Unit => new(value.SiValue * unit.SiFactor);

    public static Number As(this UnitValue value, Unit unit) {
        Contract.Assert(value.UnitType.IsInstanceOfType(unit));
        return new(value.SiValue * unit.SiFactor);
    }
    
    // Conditions validated in Unit base constructor

    public static TUnit SiUnitOf<TUnit>() where TUnit : Unit =>
        ((UnitDescription<TUnit>) Activator.CreateInstance(typeof(TUnit)
            .GetCustomAttribute<UnitMetadataAttribute>().UnitDescriptionType)).SiUnit;

    public static TUnit DisplayUnitOf<TUnit>() where TUnit : Unit =>
        ((UnitDescription<TUnit>) Activator.CreateInstance(typeof(TUnit)
            .GetCustomAttribute<UnitMetadataAttribute>().UnitDescriptionType)).DisplayUnit;

    // TODO: the other unit utilities from dProB...
}