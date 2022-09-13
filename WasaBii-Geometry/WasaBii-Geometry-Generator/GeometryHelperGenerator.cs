using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using WasaBii_Geometry_Shared;

namespace BII.WasaBii.UnitSystem;

[Generator]
public sealed class GeometryHelperGenerator : ISourceGenerator {
    
    private static readonly DiagnosticDescriptor UnexpectedGenerationIssue = new(
        id: "WasaBiiGeometryHelpers",
        title: "Unexpected Geometry Helper Generation Issue",
        messageFormat: "Unexpected issue while generating geometry utility source code:\n{0}",
        category: "WasaBii",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(GeneratorInitializationContext context) =>
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

    public void Execute(GeneratorExecutionContext context) {
        
        // Ensure proper printing of decimal constants as valid C# code
        var origCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        try {
            foreach (var typeDecl in ((SyntaxReceiver)context.SyntaxReceiver!).GeometryHelpers) {
                var fields = typeDecl.Members.OfType<FieldDeclarationSyntax>().ToArray();
                var semanticModel = context.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
                var hasMagnitude = fields.All(f =>
                    TypeSymbolFor(f.Declaration.Type, semanticModel) is INamedTypeSymbol {
                        Name: "Length", ContainingAssembly.Name: "BII.WasaBii.UnitSystem"
                    });
                // if fields are independent, make map and with
                // if has magnitude, make scale?, Length?, Min, Max
                // isNearly, Lerp, Slerp
                var sourceText = SourceText.From(InContext(
                    "",
                    typeDecl,
                    includeSelf: true
                ));
            }
        }
        catch (Exception e) {
            context.ReportDiagnostic(Diagnostic.Create(UnexpectedGenerationIssue, Location.None, e.Message));
        }
        finally {
            Thread.CurrentThread.CurrentCulture = origCulture;
        }
    }

    public sealed class SyntaxReceiver : ISyntaxReceiver {

        public readonly HashSet<TypeDeclarationSyntax> GeometryHelpers = new();


        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if (syntaxNode is TypeDeclarationSyntax tds &&
                tds.ChildTokens().Any(t => t.IsKind(SyntaxKind.PartialKeyword)) &&
                tds.AttributeLists.SelectMany(a => a.Attributes)
                    .Any(a => a.Name.ToString() == nameof(GeometryHelper))) 
                GeometryHelpers.Add(tds);
        }
    }
    
    private static ITypeSymbol TypeSymbolFor(SyntaxNode node, SemanticModel semanticModel) => 
        semanticModel.GetTypeInfo(node).ConvertedType!;

    private static string InContext(string code, TypeDeclarationSyntax typeDecl, bool includeSelf = false) {
        var (prefix, indent, postfix) = CodeGenerationUtils.MkContext(typeDecl, includeSelf);
        return prefix +
               string.Join("\n", code.Split('\n').Select(l => CodeGenerationUtils.Indent(indent) + l)) +
               postfix;
    }
}