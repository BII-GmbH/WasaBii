using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace BII.WasaBii.Units;

#region Json Unit Definition Records

record SiUnitDef(string Name, string Short);

record UnitDef(string Name, string Short, double Factor);

record BaseUnit(string TypeName, SiUnitDef SiUnit, UnitDef[] AdditionalUnits, bool GenerateExtensions = false);

record DerivedUnit(string TypeName, 
    string PrimaryUnit, 
    string SecondaryUnit, 
    SiUnitDef SiUnit,
    UnitDef[] AdditionalUnits, 
    bool GenerateExtensions = false
);

record UnitDefinitions(string Namespace, BaseUnit[] BaseUnits, DerivedUnit[] MulUnits, DerivedUnit[] DivUnits);

#endregion


[Generator]
public class UnitGenerator : ISourceGenerator {

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context) {
        
        try {

            var unitDefs = context.AdditionalFiles
                .Where(f => f.Path.EndsWith(".units.json"))
                .Select(f => {
                    // Sometimes, the paths passed to this are not consistent with `Path.PathSeparator`...
                    var fileNameFull = f.Path.Split('/').SelectMany(s => s.Split('\\')).Last();
                    return (
                        FileName: fileNameFull.Substring(0, fileNameFull.Count() - ".units.json".Count()),
                        Defs: JsonConvert.DeserializeObject<UnitDefinitions>(f.GetText()!.ToString())!
                    );
                }).ToList();

            foreach (var (fileName, unitDef) in unitDefs) {
                var source = GenerateSourceFor(unitDef);
                context.AddSource(
                    $"{fileName}.g.cs",
                    source
                );
            }
            
            context.AddSource(
                $"EnsureGenerationWorks.g.cs",
                SourceText.From(
                    "namespace BII.WasaBii.Units { public static class EnsureGenerationDidRun { " +
                        "public const bool DidRun = true; " +
                    $"    public const string ErrorMessage = \"All good. {string.Join(",", unitDefs.Select(d => d.FileName))}\";" +
                    "} }",
                    Encoding.UTF8
                )
            );
            
        }
        catch (Exception e) {
            context.AddSource(
                $"EnsureGenerationWorks.g.cs",
                SourceText.From(
                    "namespace BII.WasaBii.Units { public static class EnsureGenerationDidRun { " +
                    "public const bool DidRun = true; " +
                    $"\npublic const string ErrorMessage = @\"{e.Message.Replace("\"", "\"\"")}\";" +
                    "} }",
                    Encoding.UTF8
                )
            );
        }

        // Note CR: this seems to allow the "init" keyword to compile somehow
        context.AddSource("EnsureIsExternalInitHack.g.cs", SourceText.From(@"
namespace System.Runtime.CompilerServices {
    public static class IsExternalInit {}
}", Encoding.UTF8));
    }

    private static SourceText GenerateSourceFor(
        UnitDefinitions unitDef
    ) {
        var res = $@"
using System;
using BII.WasaBii.Units;

{string.Join("\n\n", unitDef.BaseUnits.Select(GenerateBaseUnit))}


{string.Join("\n\n", unitDef.MulUnits.Select(m => GenerateDerivedUnit(m, isMul: true)))}


{string.Join("\n\n", unitDef.DivUnits.Select(d => GenerateDerivedUnit(d, isMul: false)))}
";
        
        return SourceText.From(
            InNamespace(res, unitDef.Namespace), 
            Encoding.UTF8
        );
    }
    
#region Base Units

    private static string GenerateBaseUnit(BaseUnit unit) {
        var name = unit.TypeName;
        return $@"#region {name}

public readonly struct {name} : IUnitValue<{name}, {name}.Unit> {{
    public double SiValue {{ init; get; }}
    public Type UnitType => typeof(Unit);
    
    public {name}(double value, Unit unit) => SiValue = value * unit.SiFactor;

    public static {name} Zero => new(0, Unit.SiUnit);

    [UnitMetadata(typeof(Description))]
    public abstract class Unit : IUnit.Base {{
        public abstract string LongName {{ get; }}
        public abstract string ShortName {{ get; }}
        public abstract double SiFactor {{ get; }}

        private Unit() {{}}

        public static Unit SiUnit => {unit.SiUnit.Name}.Instance;

        public sealed class Description : IUnitDescription<Unit> {{
            public Unit SiUnit => Unit.SiUnit;
        }}

        public sealed class {unit.SiUnit.Name} : Unit {{
            public override string LongName => ""{unit.SiUnit.Name}"";
            public override string ShortName => ""{unit.SiUnit.Short}"";
            public override double SiFactor => 1d;
            
            private {unit.SiUnit.Name}() : base() {{}}

            public static readonly {unit.SiUnit.Name} Instance = new();
        }}

{string.Join("\n\n", unit.AdditionalUnits.Select(au => $@"
        public sealed class {au.Name} : Unit {{
            public override string LongName => ""{au.Name}"";
            public override string ShortName => ""{au.Short}"";
            public override double SiFactor => {au.Factor};
            
            private {au.Name}() : base() {{}}

            public static readonly {au.Name} Instance = new();
        }}
"))}

    }}

    public int CompareTo({name} other) => this.SiValue.CompareTo(other.SiValue);
    public bool Equals({name} other) => this.SiValue.Equals(other.SiValue);

    public static bool operator ==({name} left, {name} right) => Equals(left, right);
    public static bool operator !=({name} left, {name} right) => !Equals(left, right);

    public override bool Equals(object obj) => obj is {name} other && Equals(other);

    // We include this type in case values of different units are hashed in the same collection
    public override int GetHashCode() => HashCode.Combine(this.SiValue, typeof({name}));

    {/* TODO: operators with this and other compatible units */ ""}
    
}}{GenerateToDoubleExtensions(unit)}{GenerateConstructionExtensions(unit)}

#endregion
";
    }

    private static string GenerateToDoubleExtensions(BaseUnit unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

public static class {name}ToDoubleExtensions {{
    public static double As{unit.SiUnit.Name}(this IUnitValue<{name}.Unit> value) => value.SiValue;
{string.Join("\n", unit.AdditionalUnits.Select(au =>
    $@"    public static double As{au.Name}(this IUnitValue<{name}.Unit> value) => value.SiValue / {au.Factor};"))}  
}}
";
    }

    private static string GenerateConstructionExtensions(BaseUnit unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

public static class {name}ConstructionExtensions {{
    public static {name} {unit.SiUnit.Name}(this double value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
{string.Join("\n", unit.AdditionalUnits.Select(au => 
    $@"    public static {name} {au.Name}(this double value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
}}
";
    }
    
#endregion

#region Derived Units

    private static string GenerateDerivedUnit(DerivedUnit unit, bool isMul) {
        var name = unit.TypeName;
        var typeStr = isMul ? "Mul" : "Div";
        return $@"#region {name}

public readonly struct {name} : IUnitValue<{name}, {name}.Unit> {{
    public double SiValue {{ init; get; }}
    public Type UnitType => typeof(Unit);
    
    public {name}(double value, Unit unit) => SiValue = value * unit.SiFactor;

    public static {name} Zero => new(0, Unit.SiUnit);

    [UnitMetadata(typeof(Description))]
    public abstract class Unit : IUnit.{typeStr}<{unit.PrimaryUnit}.Unit, {unit.SecondaryUnit}.Unit> {{
        public abstract string LongName {{ get; }}
        public abstract string ShortName {{ get; }}
        public abstract double SiFactor {{ get; }}

        private Unit() {{}}

        public static Unit SiUnit => {unit.SiUnit.Name}.Instance;

        public sealed class Description : IUnitDescription<Unit> {{
            public Unit SiUnit => Unit.SiUnit;
        }}

        public sealed class {unit.SiUnit.Name} : Unit {{
            public override string LongName => ""{unit.SiUnit.Name}"";
            public override string ShortName => ""{unit.SiUnit.Short}"";
            public override double SiFactor => 1d;
            
            private {unit.SiUnit.Name}() : base() {{}}

            public static readonly {unit.SiUnit.Name} Instance = new();
        }}

{string.Join("\n\n", unit.AdditionalUnits.Select(au => $@"
        public sealed class {au.Name} : Unit {{
            public override string LongName => ""{au.Name}"";
            public override string ShortName => ""{au.Short}"";
            public override double SiFactor => {au.Factor};
            
            private {au.Name}() : base() {{}}

            public static readonly {au.Name} Instance = new();
        }}
"))}

    }}

    public int CompareTo({name} other) => this.SiValue.CompareTo(other.SiValue);
    public bool Equals({name} other) => this.SiValue.Equals(other.SiValue);

    public static bool operator ==({name} left, {name} right) => Equals(left, right);
    public static bool operator !=({name} left, {name} right) => !Equals(left, right);

    public override bool Equals(object obj) => obj is {name} other && Equals(other);

    // We include this type in case values of different units are hashed in the same collection
    public override int GetHashCode() => HashCode.Combine(this.SiValue, typeof({name}));

    {/* TODO: operators with this and other compatible units */ ""}
    
}}{GenerateDerivedToDoubleExtensions(unit)}{GenerateDerivedConstructionExtensions(unit)}

#endregion
";
    }

    private static string GenerateDerivedToDoubleExtensions(DerivedUnit unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

    public static class {name}ToDoubleExtensions {{
        public static double As{unit.SiUnit.Name}(this IUnitValue<{name}.Unit> value) => value.SiValue;
    {string.Join("\n", unit.AdditionalUnits.Select(au =>
        $@"    public static double As{au.Name}(this IUnitValue<{name}.Unit> value) => value.SiValue / {au.Factor};"))}  
    }}
    ";
    }

    private static string GenerateDerivedConstructionExtensions(DerivedUnit unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

    public static class {name}ConstructionExtensions {{
        public static {name} {unit.SiUnit.Name}(this double value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
    {string.Join("\n", unit.AdditionalUnits.Select(au => 
        $@"    public static {name} {au.Name}(this double value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
    }}
    ";
    }
    
#endregion

    private static string InNamespace(string code, string nameSpace) => 
$@"namespace {nameSpace} {{
    {string.Join("\n", code.Split('\n').Select(l => "    " + l))}
}}";
}