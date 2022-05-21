namespace BII.WasaBii.UnitSystem; 

public record SiUnitDef(string Name, string Short);

public record UnitDef(string Name, string Short, double Factor);

public interface IUnitDef {
    string TypeName { get; }
    SiUnitDef SiUnit { get; }
    UnitDef[] AdditionalUnits { get; }
    bool GenerateExtensions { get; }
}

public record BaseUnitDef(string TypeName, SiUnitDef SiUnit, UnitDef[] AdditionalUnits, bool GenerateExtensions = false) : IUnitDef;

public record DerivedUnitDef(string TypeName, 
    string Primary, 
    string Secondary, 
    SiUnitDef SiUnit,
    UnitDef[] AdditionalUnits, 
    bool GenerateExtensions = false
) : IUnitDef;

public record UnitDefinitions(string Namespace, BaseUnitDef[] BaseUnits, DerivedUnitDef[] MulUnits, DerivedUnitDef[] DivUnits);