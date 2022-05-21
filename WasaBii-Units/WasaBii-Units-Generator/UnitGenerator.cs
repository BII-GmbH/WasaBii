using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace BII.WasaBii.UnitSystem;

[Generator]
public class UnitGenerator : ISourceGenerator {
    
    private static readonly DiagnosticDescriptor UnexpectedUnitGenerationIssue = new(
        id: "WasaBiiUnits",
        title: "Unexpected Unit Generation Issue",
        messageFormat: "Unexpected issue while generating unit source code:\n{0}",
        category: "WasaBii",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context) {
        
        // Ensure proper printing of decimal constants as valid C# code
        var origCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        try {

            var unitDefs = context.AdditionalFiles
                .Where(f => f.Path.EndsWith(".units.json"))
                .Select(f => {
                    // Sometimes, the paths passed to this are not consistent with `Path.PathSeparator`...
                    var fileNameFull = Path.GetFileName(f.Path);
                    return (
                        FileName: fileNameFull.Substring(0, fileNameFull.Count() - ".units.json".Count()),
                        Defs: JsonConvert.DeserializeObject<UnitDefinitions>(f.GetText()!.ToString())!
                    );
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
            }

        }
        catch (Exception e) {
            context.ReportDiagnostic(Diagnostic.Create(UnexpectedUnitGenerationIssue, Location.None, e.Message));
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
{unitsInclude}

{string.Join("\n\n", unitDef.BaseUnits.Select(b => UnitCodeGeneration.GenerateBaseUnit(b, conversions)))}


{string.Join("\n\n", unitDef.MulUnits.Select(m => UnitCodeGeneration.GenerateDerivedUnit(m, conversions, isMul: true)))}


{string.Join("\n\n", unitDef.DivUnits.Select(d => UnitCodeGeneration.GenerateDerivedUnit(d, conversions, isMul: false)))}
";
        
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