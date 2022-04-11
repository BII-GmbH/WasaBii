namespace System.Runtime.CompilerServices {
    public class IsExternalInit {}
}

namespace BII.WasaBii.Units {

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class BaseUnitDefinitionAttribute : Attribute {
        public readonly string UnitName;
        public readonly Enum SiUnit;
        public bool GenerateExtensions { init; get; } = false;

        public BaseUnitDefinitionAttribute(string unitName, object siUnit) {
            UnitName = unitName;
            SiUnit = (Enum) siUnit;
        }
    }
    
    public enum DerivedUnitKind { Div, Mul }

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class DerivedUnitDefinitionAttribute : Attribute {
        public readonly DerivedUnitKind UnitKind;
        public readonly string UnitName;
        public readonly Type LeftUnitDefinition;
        public readonly Type RightUnitDefinition;
        public bool GenerateExtensions { get; set; } = false;
        public bool IncludeDerivedUnits { get; set; } = true;
        
        public DerivedUnitDefinitionAttribute(
            DerivedUnitKind unitKind, 
            string unitName, 
            Type leftUnitDef, 
            Type rightUnitDef
        ) {
            UnitKind = unitKind;
            UnitName = unitName;
            LeftUnitDefinition = leftUnitDef;
            RightUnitDefinition = rightUnitDef;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnitAttribute : Attribute {
        public readonly string DisplayName;
        public readonly double Factor;
        public UnitAttribute(string displayName, double factor) => 
            (DisplayName, Factor) = (displayName, factor);
    }

    /// <summary>
    /// If added to a class, then all enums with <see cref="BaseUnitDefinitionAttribute"/>
    ///  or <see cref="DerivedUnitDefinitionAttribute"/> are collected together.
    /// Appropriate operator overloads for all relevant combinations will be generated in the same namespace.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)] // TODO CR: might be able to do this in assembly scope
    public sealed class UnitSystemAttribute : Attribute { }

}