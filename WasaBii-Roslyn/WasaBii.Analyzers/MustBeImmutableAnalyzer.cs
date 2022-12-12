using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BII.WasaBii.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustBeImmutableAnalyzer : DiagnosticAnalyzer
{
    public enum NotImmutableReason { }

    public record ContextPathPart(string Desc); // TODO CR: 

    public record ImmutablityViolation(
        ImmutableStack<ContextPathPart> Context, 
        NotImmutableReason Reason
    ); 
    
    internal const string DiagnosticId = "RoslynAnalyzerTemplate0001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: "Type name contains lowercase letters",
        messageFormat: "Type name '{0}' contains lowercase letters",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Type names should be all uppercase.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); 
        
        //context.EnableConcurrentExecution(); // intentionally not concurrent;
            // we don't want weird validation race conditions when one type-to-be-validated references another.

        // TODO CR: distinguish between generic type instantiations?
        var allViolations = new Dictionary<TypeInfo, List<ImmutablityViolation>>(); // empty value when checked and fine
        var topLevelToValidate = new HashSet<TypeInfo>();
        
        // Step 1: collect all violations
        
        context.RegisterSyntaxNodeAction(ctx => {
            var model = ctx.SemanticModel;
            if (ctx.Node is TypeDeclarationSyntax tds) {
                var typeInfo = model.GetTypeInfo(tds); 
                // TODO CR PREMERGE: ensure that subtypes appropriately inherit the attribute here
                if (typeInfo.Type!.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(MustBeImmutableAttribute))) {
                    topLevelToValidate.Add(typeInfo);
                    ValidateType(typeInfo, ImmutableStack<ContextPathPart>.Empty);
                }
            } 

            // adds violations to the allViolations dictionary
            void ValidateType(TypeInfo typeInfo, ImmutableStack<ContextPathPart> contextPath) {
                
            }

        }, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration);
        
        // Step 2: report all collected violations
        
        context.RegisterCompilationAction(ctx => {
            foreach (var type in topLevelToValidate) 
            foreach (var location in type.Type!.Locations)
            foreach (var violation in allViolations[type]) {
                // TODO CR PREMERGE: turn violations into proper diagnostics
                ctx.ReportDiagnostic(Diagnostic.Create(null, location, DiagnosticSeverity.Error));
            }
        });
    }
}