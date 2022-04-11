namespace BII.WasaBii.Units;

// TODO CR: equality?

public interface Unit {
    string DisplayName { get; }
    double SiFactor { get; }

    public interface Base : Unit { }
    public interface Derived : Unit { }
    
    public interface Mul<out TLeft, out TRight> : Derived
    where TLeft : Unit where TRight : Unit {

        public new class Derived : Mul<TLeft, TRight> {
            public readonly TLeft LeftUnit;
            public readonly TRight RightUnit;

            public Derived(TLeft leftUnit, TRight rightUnit) {
                LeftUnit = leftUnit;
                RightUnit = rightUnit;
            }

            public string DisplayName => $"{LeftUnit.DisplayName}{RightUnit.DisplayName}"; // e.g. "Nm"
            public double SiFactor => LeftUnit.SiFactor * RightUnit.SiFactor;
        }
    }

    public interface Div<out TNumerator, out TDenominator> : Derived
    where TNumerator : Unit where TDenominator : Unit {

        public new class Derived : Div<TNumerator, TDenominator> {
            public readonly TNumerator NumeratorUnit;
            public readonly TDenominator DenominatorUnit;

            public Derived(TNumerator numeratorUnit, TDenominator denominatorUnit) {
                NumeratorUnit = numeratorUnit;
                DenominatorUnit = denominatorUnit;
            }

            public string DisplayName => $"{NumeratorUnit.DisplayName}/{DenominatorUnit.DisplayName}"; // e.g. "km/h"
            public double SiFactor => NumeratorUnit.SiFactor / DenominatorUnit.SiFactor;
        }
    }
}

// TODO CR: ensure every unit has this and that the description type fits somehow
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnitMetadataAttribute : Attribute {
    public readonly Type UnitDescriptionType;
    public UnitMetadataAttribute(Type unitDescriptionType) => UnitDescriptionType = unitDescriptionType;
}

public interface UnitDescription<out TUnit> where TUnit : Unit {
    TUnit SiUnit { get; }
}

public static class UnitExtensions {
    public static Unit.Mul<TLeft, TRight>.Derived Mul<TLeft, TRight>(this TLeft leftUnit, TRight rightUnit)
        where TLeft : Unit where TRight : Unit => new(leftUnit, rightUnit);
    
    public static Unit.Div<TNumerator, TDenominator>.Derived Div<TNumerator, TDenominator>(
        this TNumerator numeratorUnit, TDenominator denominatorUnit
    ) where TNumerator : Unit where TDenominator : Unit => new(numeratorUnit, denominatorUnit);
}





