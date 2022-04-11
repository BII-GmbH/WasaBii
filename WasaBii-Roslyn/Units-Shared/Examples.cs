namespace BII.WasaBii.Units; 

// TODO: Implement Joule = Nm/s²
    
// TODO: generate operators for all unit combinations defined in same file
// => we cannot use operators generically, but if we have a C : DivDef(A,B)
//    then we can add appropriate operators on A, B and C.
// This project must ship as a DLL, so it needs to be solid. But we ship the main WasaBII Project as Unity project
// => the user can edit sources to add new units where needed.
    
// But I will keep the mul/div abstraction => abstraction layer for generic programming if needed

[UnitSystem, Serializable]
public static class UnitSystem {
    
    [BaseUnitDefinition(
        unitName: "Length", 
        siUnit: Meters, 
        GenerateExtensions = true
    )]
    public enum LengthDef {
        [Unit("m", 1)] Meters,
        [Unit("km", 1000)] Kilometers,
        [Unit("cm", 0.01)] Centimeters,
    }

    [BaseUnitDefinition(
        unitName: "Duration", 
        siUnit: Seconds, 
        GenerateExtensions = true
    )]
    public enum DurationDef {
        [Unit("ms", 0.001)] Milliseconds,
        [Unit("s", 1)] Seconds,
        [Unit("min", 60)] Minutes,
        [Unit("h", 3600)] Hours,
        [Unit("d", 3600*24)] Days,
        [Unit("w", 3600*24*7)] Weeks
    }

    [DerivedUnitDefinition(
        DerivedUnitKind.Div, "Velocity", 
        typeof(LengthDef), 
        typeof(DurationDef), 
        GenerateExtensions = true
    )]
    public enum VelocityDef {
        [Unit("knots", 0.514444)] Knots
    }
}

