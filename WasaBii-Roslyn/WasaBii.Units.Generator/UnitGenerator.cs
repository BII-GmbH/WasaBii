using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BII.WasaBii.UnitSystem;

[Generator]
public class UnitGenerator : ISourceGenerator {
    
    private static readonly DiagnosticDescriptor UnexpectedUnitGenerationIssue = new(
        id: "WasaBiiUnits",
        title: "Unexpected Unit Generation Issue",
        messageFormat: "Unexpected issue while generating unit source code for assembly {0}: {1}\n-----\n{2}",
        category: "WasaBii",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context) {
        // Ensure proper printing of decimal constants as valid C# code
        var origCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        var isUnityEditor = context.ParseOptions.PreprocessorSymbolNames.Contains("UNITY_EDITOR");

        try {
            var unitDefs = context.AdditionalFiles
                .Where(f => f.Path.EndsWith(".units.json"))
                .Select(f => {
                    // Sometimes, the paths passed to this are not consistent with `Path.PathSeparator`...
                    var fileNameFull = Path.GetFileName(f.Path);
                    var defs = JsonSerializer.Deserialize<UnitDefinitions>(
                        f.GetText()!.ToString(), 
                        new JsonSerializerOptions {PropertyNameCaseInsensitive = true}
                    )!;
                    var filename = fileNameFull.Substring(0, fileNameFull.Length - ".units.json".Length);
                    return (FileName: filename, Defs: defs);
                }).ToList();
            
            foreach (var (fileName, unitDef) in unitDefs) {
                // Step 1: Pre-parse and find all possible conversions
                var conversions = UnitConversions.AllConversionsFor(unitDef);
                // Step 2: Generate the actual code
                var source = GenerateSourceFor(unitDef, conversions);
                context.AddSource(
                    $"{fileName}.g.cs",
                    source
                );
                if(isUnityEditor)
                    context.AddSource(
                        $"{fileName}PropertyDrawer.g.cs",
                        GeneratePropertyDrawerSourceFor(unitDef)
                    );
            }

        }
        catch (Exception e) {
            context.ReportDiagnostic(Diagnostic.Create(UnexpectedUnitGenerationIssue, Location.None, context.Compilation.AssemblyName, e.Message, e.StackTrace));
        }
        finally {
            Thread.CurrentThread.CurrentCulture = origCulture;
        }
    }
    private static SourceText GenerateSourceFor(UnitDefinitions unitDef, UnitConversions conversions) {
        var unitsInclude = unitDef.Namespace.Equals("BII.WasaBii.UnitSystem") ? "" : "using BII.WasaBii.UnitSystem;\n";
        var res = $@"
using System;
using System.Collections.Generic;
#if UNITY_2022_1_OR_NEWER
using UnityEngine;
#endif
{unitsInclude}

{string.Join("\n\n", unitDef.BaseUnits.Select(b => UnitCodeGeneration.GenerateBaseUnit(b, conversions)))}


{string.Join("\n\n", unitDef.MulUnits.Select(m => UnitCodeGeneration.GenerateDerivedUnit(m, conversions, isMul: true)))}


{string.Join("\n\n", unitDef.DivUnits.Select(d => UnitCodeGeneration.GenerateDerivedUnit(d, conversions, isMul: false)))}
";
        
        return SourceText.From(
            @"// Convince the compiler to enable the `init` keyword
namespace System.Runtime.CompilerServices {
    internal static partial class IsExternalInit {}
}

" + InNamespace(res, unitDef.Namespace), 
            Encoding.UTF8
        );
    }

    private static SourceText GeneratePropertyDrawerSourceFor(UnitDefinitions unitDef) {
        var unitsInclude = unitDef.Namespace.Equals("BII.WasaBii.UnitSystem") ? "" : "using BII.WasaBii.UnitSystem;\n";

        string makeDrawerFor(IUnitDef unit) => $@"
[CustomPropertyDrawer(typeof({unit.TypeName}))]
public sealed class {unit.TypeName}Editor : ValueWithUnitEditor<{unit.TypeName}> {{
    protected override IUnitDescription<IUnit<{unit.TypeName}>> description =>
        new {unit.TypeName}.Unit.Description();
}}
";
        
        var res = @$"
using UnityEditor;
{unitsInclude}

{string.Join("", unitDef.BaseUnits.Cast<IUnitDef>().Concat(unitDef.DivUnits).Concat(unitDef.MulUnits).Select(makeDrawerFor))}";
        return SourceText.From(
            InNamespace(res, unitDef.Namespace), 
            Encoding.UTF8
        );
    }


    private static string InNamespace(string code, string nameSpace) => 
$@"namespace {nameSpace} {{
    {string.Join("\n", code.Split('\n').Select(l => "    " + l))}
}}";
}