using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BII.WasaBii.Analyzers; 

using static SyntaxFactory;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnumExceptionSuggestion : DiagnosticAnalyzer {
    
    public const string EnumExceptionDiagnosticId = "WasaBiiEnumException";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: EnumExceptionDiagnosticId,
        title: $"Use UnsupportedEnumValueException instead of {nameof(ArgumentOutOfRangeException)}",
        messageFormat: $"Consider replacing the {nameof(ArgumentOutOfRangeException)} with an UnsupportedEnumValueException",
        category: "WasaBii Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(analysisContext => {
            var expression = (SwitchExpressionSyntax)analysisContext.Node;
            if(analysisContext.SemanticModel.GetTypeInfo(expression.GoverningExpression).Type is {TypeKind: TypeKind.Enum}
                && expression.Arms.FirstOrDefault(IsArgumentExceptionArm) is {} arm
            ) analysisContext.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                arm.GetLocation()
            ));
        }, SyntaxKind.SwitchExpression);
    }

    private static bool IsArgumentExceptionArm(SwitchExpressionArmSyntax arm) => arm is { 
        Pattern: DiscardPatternSyntax, 
        Expression: ThrowExpressionSyntax {
            Expression : ObjectCreationExpressionSyntax {
                Type : IdentifierNameSyntax { Identifier.Text: nameof(ArgumentOutOfRangeException) }
            }
        }
    };

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public sealed class FixProvider : CodeFixProvider {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EnumExceptionDiagnosticId);
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            if (context.Document is {} document && 
                await document.GetSyntaxRootAsync().ConfigureAwait(continueOnCapturedContext: false) is {} root
            ) {
                foreach (var diagnostic in context.Diagnostics) {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with `UnsupportedEnumValueException`",
                            createChangedDocument: _ => {
                                var node = root.FindNode(diagnostic.Location.SourceSpan);
                                var newRoot = root.ReplaceNode(node, SwitchExpressionArm(
                                    VarPattern(SingleVariableDesignation(Identifier("unsupported"))),
                                    ThrowExpression(ObjectCreationExpression(IdentifierName("UnsupportedEnumValueException"))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("unsupported"))))))));
                                return Task.FromResult(document.WithSyntaxRoot(newRoot));
                            },
                            equivalenceKey: "UnsupportedEnumValueException"),
                        diagnostic);
                }
            }
        }

    }
}