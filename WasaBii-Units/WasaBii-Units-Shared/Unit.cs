namespace System.Runtime.CompilerServices {
    public static class IsExternalInit {}
}

namespace BII.WasaBii.Units {

    // TODO CR: equality?

    public interface IUnit {
        string LongName { get; }
        string ShortName { get; }
        double SiFactor { get; }

        public interface Base : IUnit { }

        public interface Derived : IUnit { }

        public interface Mul<out TLeft, out TRight> : Derived
            where TLeft : IUnit where TRight : IUnit { }

        public class DerivedMul<TLeft, TRight> : Mul<TLeft, TRight>
            where TLeft : IUnit where TRight : IUnit {
            public readonly TLeft LeftUnit;
            public readonly TRight RightUnit;

            public DerivedMul(TLeft leftUnit, TRight rightUnit) {
                LeftUnit = leftUnit;
                RightUnit = rightUnit;
            }

            public string LongName => $"{LeftUnit.LongName} {RightUnit.LongName}s";
            public string ShortName => $"{LeftUnit.ShortName}{RightUnit.ShortName}"; // e.g. "Nm"
            public double SiFactor => LeftUnit.SiFactor * RightUnit.SiFactor;
        }

        public interface Div<out TNumerator, out TDenominator> : Derived
            where TNumerator : IUnit where TDenominator : IUnit { }

        public class DerivedDiv<TNumerator, TDenominator> : Div<TNumerator, TDenominator>
            where TNumerator : IUnit where TDenominator : IUnit {
            public readonly TNumerator NumeratorUnit;
            public readonly TDenominator DenominatorUnit;

            public DerivedDiv(TNumerator numeratorUnit, TDenominator denominatorUnit) {
                NumeratorUnit = numeratorUnit;
                DenominatorUnit = denominatorUnit;
            }

            public string LongName => $"{NumeratorUnit.LongName}s per {DenominatorUnit.LongName}";
            public string ShortName => $"{NumeratorUnit.ShortName}/{DenominatorUnit.ShortName}"; // e.g. "km/h"
            public double SiFactor => NumeratorUnit.SiFactor / DenominatorUnit.SiFactor;
        }
    }

    // TODO CR: ensure every unit has this and that the description type fits somehow
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnitMetadataAttribute : Attribute {
        public readonly Type UnitDescriptionType;
        public UnitMetadataAttribute(Type unitDescriptionType) => UnitDescriptionType = unitDescriptionType;
    }

    public interface IUnitDescription<out TUnit> where TUnit : IUnit {
        TUnit SiUnit { get; }
    }

    public static class UnitExtensions {
        public static IUnit.Mul<TLeft, TRight> Mul<TLeft, TRight>(
            this TLeft leftUnit, TRight rightUnit
        ) where TLeft : IUnit where TRight : IUnit =>
            new IUnit.DerivedMul<TLeft, TRight>(leftUnit, rightUnit);

        public static IUnit.Div<TNumerator, TDenominator> Div<TNumerator, TDenominator>(
            this TNumerator numeratorUnit, TDenominator denominatorUnit
        ) where TNumerator : IUnit where TDenominator : IUnit =>
            new IUnit.DerivedDiv<TNumerator, TDenominator>(numeratorUnit, denominatorUnit);
    }

}





