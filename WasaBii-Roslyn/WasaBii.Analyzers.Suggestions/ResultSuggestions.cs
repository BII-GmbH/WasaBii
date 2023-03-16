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
public class ResultSuggestions : DiagnosticAnalyzer {
    
    public const string ResultIfDiagnosticId = "WasaBiiResultIf";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: ResultIfDiagnosticId,
        title: "Use Result.If instead of ternary operator",
        messageFormat: "Consider replacing '{0}' with 'Result.If({1}, {2}, {3})'",
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
                if (IsSuccessAndFailure(conditional.WhenTrue, conditional.WhenFalse, out var successExpression, out var failureExpression))
                    ReportSuggestion(conditional, conditional.Condition.ToString(), successExpression, failureExpression);
                else if(IsSuccessAndFailure(conditional.WhenFalse, conditional.WhenTrue, out successExpression, out failureExpression))
                    ReportSuggestion(conditional, $"!{conditional.Condition}", successExpression, failureExpression);

            void ReportSuggestion(ConditionalExpressionSyntax conditional, string condition, ExpressionSyntax successExpression, ExpressionSyntax failureExpression) {
                var successExpressionString = successExpression is SimpleNameSyntax or LiteralExpressionSyntax ? successExpression.ToString() : $"() => {successExpression}";
                var failureExpressionString = failureExpression is SimpleNameSyntax or LiteralExpressionSyntax ? failureExpression.ToString() : $"() => {failureExpression}";
                analysisContext.ReportDiagnostic(Diagnostic.Create(
                    Descriptor,
                    conditional.GetLocation(),
                    properties: ImmutableDictionary.CreateRange(new [] {
                        KeyValuePair.Create<string, string?>("SuccessExpression", successExpressionString),
                        KeyValuePair.Create<string, string?>("FailureExpression", failureExpressionString),
                        KeyValuePair.Create<string, string?>("Condition", condition)
                    }),
                    conditional.ToFullString(),
                    condition,
                    successExpressionString,
                    failureExpressionString
                ));
            }
        }, SyntaxKind.ConditionalExpression);
    }

    private static bool IsSuccessAndFailure(
        ExpressionSyntax branchA, 
        ExpressionSyntax branchB,
        out ExpressionSyntax successExpression,
        out ExpressionSyntax failureExpression
    ) {
        successExpression = null!;
        failureExpression = null!;
        return branchA is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 or 1 } trueInvocation &&
            IsSuccess(trueInvocation, out successExpression) &&
            branchB is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 or 1 } falseInvocation &&
            IsFailure(falseInvocation, out failureExpression);
    }

    private static bool IsSuccess(InvocationExpressionSyntax invocation, out ExpressionSyntax method)
    {
        if (invocation is {Expression: MemberAccessExpressionSyntax{Name.Identifier.Text:"Success"} memberAccess})
        {
            if (invocation.ArgumentList.Arguments.Count == 1 && memberAccess.Expression is SimpleNameSyntax{Identifier.Text:"Result"})
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

    private static bool IsFailure(InvocationExpressionSyntax invocation, out ExpressionSyntax method)
    {
        if (invocation is {Expression: MemberAccessExpressionSyntax{Name.Identifier.Text:"Failure"} memberAccess})
        {
            if (invocation.ArgumentList.Arguments.Count == 1 && memberAccess.Expression is SimpleNameSyntax{Identifier.Text:"Result"})
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
        
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ResultIfDiagnosticId);
        public override FixAllProvider? GetFixAllProvider() => new FixAll();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            if (context.Document is {} document && 
                await document.GetSyntaxRootAsync().ConfigureAwait(continueOnCapturedContext: false) is {} root) {
                foreach (var diagnostic in context.Diagnostics) {
                    if (extractDataFrom(diagnostic) is var (_, condition, successExpression, failureExpression))
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Replace with `Result.If`",
                                createChangedDocument: _ =>
                                    Task.FromResult(ApplyFix(document, root, diagnostic, condition, successExpression, failureExpression)),
                                equivalenceKey: "ResultIf"),
                            diagnostic);
                }
            }
        }
        
        private static Document ApplyFix(Document document, SyntaxNode root, Diagnostic diagnostic, string condition, string successExpression, string failureExpression)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var newRoot = root.ReplaceNode(node, InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Result"), IdentifierName("If")),
                ArgumentList(SeparatedList(new[] {
                    Argument(ParseExpression(condition)),
                    Argument(ParseExpression(successExpression)),
                    Argument(ParseExpression(failureExpression)),
                }))
            ));
            return document.WithSyntaxRoot(newRoot);
        }

        private static (Diagnostic Diagnostic, string Condition, string SuccessExpression, string FailureExpression)? extractDataFrom(Diagnostic diagnostic) =>
            diagnostic.Properties.TryGetValue("SuccessExpression", out var successExpression) && successExpression != null &&
            diagnostic.Properties.TryGetValue("FailureExpression", out var failureExpression) && failureExpression != null &&
            diagnostic.Properties.TryGetValue("Condition", out var condition) && condition != null
                ? (diagnostic, condition, successExpression, failureExpression)
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
                    title: "Replace with `Result.If` where applicable",
                    createChangedDocument: _ => {
                        foreach (var (diagnostic, condition, successExpression, failureExpression) in allFixes) {
                            if(ApplyFix(document, root, diagnostic, condition, successExpression, failureExpression) is {} newDocument)
                                document = newDocument;
                        }
                        return Task.FromResult(document);
                    },
                    equivalenceKey: "ResultIfAll")
                : null;
            }
        }
    }
}