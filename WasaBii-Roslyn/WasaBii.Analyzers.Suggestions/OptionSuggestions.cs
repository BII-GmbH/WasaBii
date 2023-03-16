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
using static SyntaxTreeUtils;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OptionSuggestions : DiagnosticAnalyzer {
    
    public const string OptionIfDiagnosticId = "WasaBiiOptionIf";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: OptionIfDiagnosticId,
        title: "Use Option.If instead of ternary operator",
        messageFormat: "Consider replacing '{0}' with 'Option.If({1}, {2})'",
        category: "WasaBii Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(analysisContext => {
            var conditional = (ConditionalExpressionSyntax)analysisContext.Node;

            if(!DeclaresOrAssignsVariables(conditional.Condition))
                if (IsSomeAndNone(conditional.WhenTrue, conditional.WhenFalse, out var someExpression))
                    ReportSuggestion(conditional, conditional.Condition.ToString(), someExpression);
                else if(IsSomeAndNone(conditional.WhenFalse, conditional.WhenTrue, out someExpression))
                    ReportSuggestion(conditional, $"!{conditional.Condition}", someExpression);

            void ReportSuggestion(ConditionalExpressionSyntax conditional, string condition, ExpressionSyntax someExpression) {
                var someExpressionString = someExpression is SimpleNameSyntax or LiteralExpressionSyntax ? someExpression.ToString() : $"() => {someExpression}";
                analysisContext.ReportDiagnostic(Diagnostic.Create(
                    Descriptor,
                    conditional.GetLocation(),
                    properties: ImmutableDictionary.CreateRange(new [] {
                        KeyValuePair.Create<string, string?>("SomeExpression", someExpressionString),
                        KeyValuePair.Create<string, string?>("Condition", condition)
                    }),
                    conditional.ToFullString(),
                    condition,
                    someExpressionString
                ));
            }
        }, SyntaxKind.ConditionalExpression);
    }

    private static bool IsSomeAndNone(
        ExpressionSyntax branchA, 
        ExpressionSyntax branchB,
        out ExpressionSyntax someExpression
    ) {
        someExpression = null!;
        return branchA is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 or 1 } trueInvocation &&
            IsOptionSomeMethod(trueInvocation, out someExpression) &&
            branchB is MemberAccessExpressionSyntax {
                Expression: SimpleNameSyntax { Identifier.Text: "Option" }, Name.Identifier.Text: "None"
            };
    }

    private static bool IsOptionSomeMethod(InvocationExpressionSyntax invocation, out ExpressionSyntax method)
    {
        if (invocation is {Expression: MemberAccessExpressionSyntax{Name.Identifier.Text:"Some"} memberAccess})
        {
            if (invocation.ArgumentList.Arguments.Count == 1 && memberAccess.Expression is SimpleNameSyntax{Identifier.Text:"Option"})
            {
                method = invocation.ArgumentList.Arguments[0].Expression;
                return true;
            }
            else if (invocation.ArgumentList.Arguments.Count == 0)
            {
                method = memberAccess.Expression;
                return true;
            }
        }

        method = null!;
        return false;
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public sealed class FixProvider : CodeFixProvider {
        
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(OptionIfDiagnosticId);
        public override FixAllProvider? GetFixAllProvider() => new FixAll();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            if (context.Document is {} document && 
                await document.GetSyntaxRootAsync().ConfigureAwait(continueOnCapturedContext: false) is {} root) {
                foreach (var diagnostic in context.Diagnostics) {
                    if (extractDataFrom(diagnostic) is var (_, condition, someMethod))
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Replace with `Option.If`",
                                createChangedDocument: _ =>
                                    Task.FromResult(ApplyFix(document, root, diagnostic, condition, someMethod)),
                                equivalenceKey: "OptionIf"),
                            diagnostic);
                }
            }
        }
        
        private static Document ApplyFix(Document document, SyntaxNode root, Diagnostic diagnostic, string condition, string someMethod)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var newRoot = root.ReplaceNode(node, InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Option"), IdentifierName("If")),
                ArgumentList(SeparatedList(new[] {
                    Argument(ParseExpression(condition)),
                    Argument(ParseExpression(someMethod)),
                }))
            ));
            return document.WithSyntaxRoot(newRoot);
        }

        private static (Diagnostic Diagnostic, string Condition, string SomeMethod)? extractDataFrom(Diagnostic diagnostic) =>
            diagnostic.Properties.TryGetValue("SomeExpression", out var someMethod) && someMethod != null
            && diagnostic.Properties.TryGetValue("Condition", out var condition) && condition != null
                ? (diagnostic, condition, someMethod)
                : null;

        private sealed class FixAll : FixAllProvider {
            public override async Task<CodeAction?> GetFixAsync(FixAllContext context) {
                var allFixes = (await context.GetAllDiagnosticsAsync(context.Project))
                    .Select(extractDataFrom)
                    .Where(tuple => tuple != null)
                    .Select(tuple => tuple!.Value)
                    .ToList();
                return allFixes.Count > 0 && context.Document is {} document && 
                    await document.GetSyntaxRootAsync().ConfigureAwait(continueOnCapturedContext: false) is {} root
                ? CodeAction.Create(
                    title: "Replace with `Option.If` where applicable",
                    createChangedDocument: _ => {
                        foreach (var (diagnostic, condition, someMethod) in allFixes) {
                            if(ApplyFix(document, root, diagnostic, condition, someMethod) is {} newDocument)
                                document = newDocument;
                        }
                        return Task.FromResult(document);
                    },
                    equivalenceKey: "OptionIfAll")
                : null;
            }
        }
    }
}