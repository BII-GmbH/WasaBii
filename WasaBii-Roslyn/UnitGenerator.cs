using System.Text;
using BII.WasaBii.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BII.WasaBii.Roslyn;

[Generator]
public class UnitGenerator : ISourceGenerator {
    
    // TODO CR: screw enum format foo, we use some external resource JSONs or something
    
    // var semanticModel = context.Compilation.GetSemanticModel(context.Compilation.SyntaxTrees.First()); // POGCHAMP

    public void Initialize(GeneratorInitializationContext context) {
        context.RegisterForSyntaxNotifications(() => new UnitDefinitionFinder());
    }

    public void Execute(GeneratorExecutionContext context) {
        
        var unitDefinitionFinder = (UnitDefinitionFinder) context.SyntaxReceiver!;
        
        foreach (var unitSystemClass in unitDefinitionFinder.UnitSystems) {
            var semanticModel = context.Compilation.GetSemanticModel(unitSystemClass.SyntaxTree);
            context.AddSource(
                $"{unitSystemClass.Identifier.Text}_Units.g.cs", 
                GenerateSourceFor(unitSystemClass, semanticModel)
            );
        }
    }

    private static SourceText GenerateSourceFor(
        ClassDeclarationSyntax unitSystemClass,
        SemanticModel semanticModel
    ) {
        var baseUnitDefinitions =
            new Dictionary<string, (EnumDeclarationSyntax Declaration, AttributeData AttributeData)>();
        
        var derivedUnitDefinitions =
            new Dictionary<string, (EnumDeclarationSyntax Declaration, AttributeData AttributeData)>();

        foreach (var enumDeclaration in unitSystemClass.ChildNodes().OfType<EnumDeclarationSyntax>()) {
            // TODO CR: is it really worth it? developing a proper extensible system for unit generation?
            // When we could just generate the units via hard-coded foo in here somewhere?
            // Yeah, let's do a generator-only DSL I think. Everything else won't be "perfect enough"
        }
        
        string res;
        return SourceText.From("", Encoding.UTF8);
    }

    private class UnitDefinitionFinder : ISyntaxReceiver {
        
        private readonly List<ClassDeclarationSyntax> _unitSystems = new();
        public IEnumerable<ClassDeclarationSyntax> UnitSystems => _unitSystems;
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if (syntaxNode is ClassDeclarationSyntax cds
                && cds.AttributeLists.Any(a =>
                    a.ChildNodes().Any(c =>
                        c is AttributeSyntax asn && asn.Name.ToString() == nameof(UnitSystem)))
               ) _unitSystems.Add(cds);
        }
    }
}