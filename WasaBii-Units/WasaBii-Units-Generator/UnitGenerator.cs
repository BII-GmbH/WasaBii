using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace BII.WasaBii.Units;

/* TODO: operators with this and other compatible units */

#region Json Unit Definition Records

record SiUnitDef(string Name, string Short);

record UnitDef(string Name, string Short, double Factor);

interface IUnit {
    string TypeName { get; }
    SiUnitDef SiUnit { get; }
    UnitDef[] AdditionalUnits { get; }
    bool GenerateExtensions { get; }
}

record BaseUnit(string TypeName, SiUnitDef SiUnit, UnitDef[] AdditionalUnits, bool GenerateExtensions = false) : IUnit;

record DerivedUnit(string TypeName, 
    string Primary, 
    string Secondary, 
    SiUnitDef SiUnit,
    UnitDef[] AdditionalUnits, 
    bool GenerateExtensions = false
) : IUnit;

record UnitDefinitions(string Namespace, BaseUnit[] BaseUnits, DerivedUnit[] MulUnits, DerivedUnit[] DivUnits);

#endregion


[Generator]
public class UnitGenerator : ISourceGenerator {

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context) {
        
        // try {

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
            
        // }
        // catch (Exception e) {
        //     context.AddSource(
        //         $"EnsureGenerationWorks.g.cs",
        //         SourceText.From(
        //             "namespace BII.WasaBii.Units { public static class EnsureGenerationDidRun { " +
        //             "public const bool DidRun = true; " +
        //             $"\npublic const string ErrorMessage = @\"{e.Message.Replace("\"", "\"\"")}\";" +
        //             "} }",
        //             Encoding.UTF8
        //         )
        //     );
        // }

        // Note CR: this seems to allow the "init" keyword to compile somehow
        context.AddSource("EnsureIsExternalInitHack.g.cs", SourceText.From(@"
namespace System.Runtime.CompilerServices {
    public static class IsExternalInit {}
}", Encoding.UTF8));
    }

    private static SourceText GenerateSourceFor(
        UnitDefinitions unitDef
    ) {
        var unitsInclude = unitDef.Namespace.Equals("BII.WasaBii.Units") ? "" : "using BII.WasaBii.Units;\n";
        var res = $@"
using System;
{unitsInclude}

{string.Join("\n\n", unitDef.BaseUnits.Select(GenerateBaseUnit))}


{string.Join("\n\n", unitDef.MulUnits.Select(m => GenerateDerivedUnit(m, isMul: true)))}


{string.Join("\n\n", unitDef.DivUnits.Select(d => GenerateDerivedUnit(d, isMul: false)))}


{GenerateConversions(unitDef)}
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

public readonly partial struct {name} : IUnitValue<{name}, {name}.Unit> {{
    public double SiValue {{ init; get; }}
    public Type UnitType => typeof(Unit);
    
    public {name}(double value, Unit unit) => SiValue = value * unit.SiFactor;

    public static {name} Zero => new(0, SiUnit);

    public static Unit SiUnit => Unit.{unit.SiUnit.Name}.Instance;

    [UnitMetadata(typeof(Description))]
    public abstract class Unit : IUnit.Base {{
        public abstract string LongName {{ get; }}
        public abstract string ShortName {{ get; }}
        public abstract double SiFactor {{ get; }}

        private Unit() {{}}

        public sealed class Description : IUnitDescription<Unit> {{
            public Unit SiUnit => {name}.SiUnit;
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

public readonly partial struct {name} : IUnitValue<{name}, {name}.Unit> {{
    public double SiValue {{ init; get; }}
    public Type UnitType => typeof(Unit);
    
    public {name}(double value, Unit unit) => SiValue = value * unit.SiFactor;

    public static {name} Zero => new(0, SiUnit);

    public static Unit SiUnit => Unit.{unit.SiUnit.Name}.Instance;

    [UnitMetadata(typeof(Description))]
    public abstract class Unit : IUnit.{typeStr}<{unit.Primary}.Unit, {unit.Secondary}.Unit> {{
        public abstract string LongName {{ get; }}
        public abstract string ShortName {{ get; }}
        public abstract double SiFactor {{ get; }}

        private Unit() {{}}

        public sealed class Description : IUnitDescription<Unit> {{
            public Unit SiUnit => {name}.SiUnit;
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

#region Conversion Operators

    // Plus and Minus on the same units
    // Mul and Div on composed units
    
    enum UnitDefType { Base, Mul, Div }

    private static string GenerateConversions(UnitDefinitions unitDef) {
        var baseNameToUnit = unitDef.BaseUnits.ToDictionary(u => u.TypeName, u => u);
        var mulNameToUnit = unitDef.MulUnits.ToDictionary(u => u.TypeName, u => u);
        var divNameToUnit = unitDef.DivUnits.ToDictionary(u => u.TypeName, u => u);
        
        // For ease of use, just make a lot of partial classes

        string Header(IUnit unit) => $"public readonly partial struct {unit.TypeName}";

        var res = new StringBuilder();
        
        // Plus and Minus
        
        string GeneratePlusMinus(IUnit unit) {
            var name = unit.TypeName;
            return $@"    public static {name} operator +({name} first, {name} second) => new(first.SiValue + second.SiValue, {name}.SiUnit);
    public static {name} operator -({name} first, {name} second) => new(first.SiValue - second.SiValue, {name}.SiUnit);";
        }
        
        foreach (var b in unitDef.BaseUnits) {
            res.Append($@"
{Header(b)} {{
{GeneratePlusMinus(b)}  
}}");
        }
        
        foreach (var m in unitDef.MulUnits) {
            res.Append($@"
{Header(m)} {{
{GeneratePlusMinus(m)}  
}}");
        }
        
        foreach (var d in unitDef.DivUnits) {
            res.Append($@"
{Header(d)} {{
{GeneratePlusMinus(d)}  
}}");
        }
        
        // Multiplication and Division

        (UnitDefType, IUnit) MatchUnit(string unit) {
            if (baseNameToUnit!.TryGetValue(unit, out var b)) {
                return (UnitDefType.Base, b);
            } else if (mulNameToUnit!.TryGetValue(unit, out var m)) {
                return (UnitDefType.Mul, m);
            } else if (divNameToUnit!.TryGetValue(unit, out var d)) {
                return (UnitDefType.Div, d);
            } else throw new Exception($"Unit not declared: '{unit}'");
        }

        // first and second are symmetric
        (IUnit First, IUnit Second, IUnit Third) OperationForDerived(DerivedUnit unit, bool isMul) {
            var (firstType, first) = MatchUnit(unit.Primary);
            var (secondType, second) = MatchUnit(unit.Secondary);
            if (isMul) {
                return  (first, second, unit);
                // TODO: find other units that can be combined, recursively
            } else { // is div: (first / second = unit) <=> (unit * second = first)
                return (unit, second, first);
                // TODO: find other unit that can be combined, recursively
            }
        } 
        
        string GenerateMul(IUnit a, IUnit b, IUnit c) => $@"
{Header(a)} {{
    public static {c.TypeName} operator*({a.TypeName} a, {b.TypeName} b) => 
        new(a.SiValue * b.SiValue, {c.TypeName}.SiUnit);
}}";

        string GenerateDiv(IUnit a, IUnit b, IUnit c) => 
            a.Equals(b) ? $@"
{Header(c)} {{
    public static {b.TypeName} operator/({c.TypeName} c, {a.TypeName} a) => 
        new(c.SiValue / a.SiValue, {b.TypeName}.SiUnit);
}}
" : $@"
{Header(c)} {{
    public static {b.TypeName} operator/({c.TypeName} c, {a.TypeName} a) => 
        new(c.SiValue / a.SiValue, {b.TypeName}.SiUnit);
    public static {a.TypeName} operator/({c.TypeName} c, {b.TypeName} b) => 
        new(c.SiValue / b.SiValue, {a.TypeName}.SiUnit);
}}
";

        foreach (var (m, isMul) in unitDef.MulUnits
            .Select(m => (m, true))
            .Concat(unitDef.DivUnits.Select(d => (d, false)))
        ) {
            var (a, b, c) = OperationForDerived(m, isMul);
            res.Append(GenerateMul(a, b, c));
            if (!a.Equals(b)) res.Append(GenerateMul(b, a, c));
            res.Append(GenerateDiv(a, b, c));
        }

        return res.ToString();
    }

#endregion


    private static string InNamespace(string code, string nameSpace) => 
$@"namespace {nameSpace} {{
    {string.Join("\n", code.Split('\n').Select(l => "    " + l))}
}}";
}