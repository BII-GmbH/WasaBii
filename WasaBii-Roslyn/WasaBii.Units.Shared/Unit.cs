namespace System.Runtime.CompilerServices {
    public static partial class IsExternalInit {}
}

namespace BII.WasaBii.UnitSystem {

    public interface IUnit {
        string LongName { get; }
        string ShortName { get; }
        double SiFactor { get; }
        
        /// <summary>
        /// The multiplicative inverse of <see cref="SiFactor"/>. Precalculated once such that utility methods can
        /// multiply si values by this instead of dividing by <see cref="SiFactor"/> for improved performance.
        /// </summary>
        double InverseSiFactor { get; }

        public interface Base : IUnit { }

        public interface Derived : IUnit { }

        public interface Mul<out TLeft, out TRight> : Derived
            where TLeft : IUnit where TRight : IUnit { }

        public interface Div<out TNumerator, out TDenominator> : Derived
            where TNumerator : IUnit where TDenominator : IUnit { }
    }

    // TODO CR: ensure every unit has this and that the description type fits somehow
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnitMetadataAttribute : Attribute {
        public readonly Type UnitDescriptionType;
        public UnitMetadataAttribute(Type unitDescriptionType) => UnitDescriptionType = unitDescriptionType;
    }

    public interface IUnitDescription<out TUnit> where TUnit : IUnit {
        TUnit SiUnit { get; }
        IReadOnlyList<TUnit> AllUnits { get; }
    }

    public static class UnitMulDivExtensions {
        
        /// <summary>
        /// Multiplies the two passed UnitValues.
        /// Can be used to get values of units that have not been generated explicitly.
        /// This can be useful in rare cases, when you do not want to introduce a whole
        ///  new hard-coded unit for a single use-case, or for edge cases in generic code.
        /// </summary>
        public static UnitValueOf<IUnit.Mul<TLeft, TRight>> Mul<TLeft, TRight>(
            IUnitValueOf<TLeft> left, IUnitValueOf<TRight> right
        ) where TLeft : IUnit where TRight : IUnit => 
            new UnitValueOf<IUnit.Mul<TLeft, TRight>> { SiValue = left.SiValue * right.SiValue };
        
        /// <summary>
        /// Divides the first passed UnitValue by the second.
        /// Can be used to get values of units that have not been generated explicitly.
        /// This can be useful in rare cases, when you do not want to introduce a whole
        ///  new hard-coded unit for a single use-case, or for edge cases in generic code.
        /// </summary>
        public static UnitValueOf<IUnit.Div<TLeft, TRight>> Div<TLeft, TRight>(
            IUnitValueOf<TLeft> left, IUnitValueOf<TRight> right
        ) where TLeft : IUnit where TRight : IUnit => 
            new UnitValueOf<IUnit.Div<TLeft, TRight>> { SiValue = left.SiValue / right.SiValue };
    }

}





