namespace System.Runtime.CompilerServices {
    public class IsExternalInit {}
}

namespace BII.WasaBii.Units {

    // [BaseUnitDefinition(
    //     unitName: "Length", 
    //     siUnit: Meters, 
    //     displayUnit: Meters, 
    //     GenerateExtensions = true
    // )]
    // public enum LengthDef {
    //     [Unit("m", 1)] Meters,
    //     [Unit("km", 1000)] Kilometers,
    //     [Unit("cm", 0.01)] Centimeters,
    // }

    // [DivUnitDefinition("Velocity", typeof(Length), typeof(Time), GenerateExtensions = true)]
    // public enum VelocityDef {
    //     [Unit("knots", 0.514444)] Knots
    // }

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class BaseUnitDefinitionAttribute : Attribute {
        public readonly string UnitName;
        public readonly Enum SiUnit;
        public readonly Enum DisplayUnit;
        public bool GenerateExtensions { init; get; } = true;

        public BaseUnitDefinitionAttribute(string unitName, object siUnit, object displayUnit) {
            UnitName = unitName;
            SiUnit = (Enum) siUnit;
            DisplayUnit = (Enum) displayUnit;
        }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class DivUnitDefinitionAttribute : Attribute {
        public readonly string UnitName;
        public readonly Type LeftUnit;
        public readonly Type RightUnit;
        public bool GenerateExtensions { get; set; } = true;
        public bool IncludeDerivedUnits { get; set; } = false;
        public DivUnitDefinitionAttribute(string unitName, Type leftUnit, Type rightUnit) {
            UnitName = unitName;
            LeftUnit = leftUnit;
            RightUnit = rightUnit;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnitAttribute : Attribute {
        public readonly string DisplayName;
        public readonly double Factor;
        public UnitAttribute(string displayName, double factor) => 
            (DisplayName, Factor) = (displayName, factor);
    }

}