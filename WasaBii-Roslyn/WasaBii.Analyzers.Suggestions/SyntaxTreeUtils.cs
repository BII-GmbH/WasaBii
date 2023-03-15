using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BII.WasaBii.Analyzers;

internal static class SyntaxTreeUtils {

    public static bool DeclaresOrAssignsVariables(ExpressionSyntax condition) => condition switch {
        IsPatternExpressionSyntax ip => IsPatternWithDeclaration(ip.Pattern),
        InvocationExpressionSyntax invocation => DeclaresOrAssignsVariables(invocation.Expression) || 
            invocation.ArgumentList.Arguments.Any(argument => 
                DeclaresOrAssignsVariables(argument.Expression) || 
                argument.RefOrOutKeyword != default && argument.Expression is not IdentifierNameSyntax{Identifier.Text:"_"}),
        BinaryExpressionSyntax binaryExpression => DeclaresOrAssignsVariables(binaryExpression.Left) || 
            DeclaresOrAssignsVariables(binaryExpression.Right),
        _ => false
    };

    public static bool IsPatternWithDeclaration(PatternSyntax pattern) => pattern switch {
        UnaryPatternSyntax unaryPattern => IsPatternWithDeclaration(unaryPattern.Pattern),
        DeclarationPatternSyntax declarationPattern => declarationPattern.Designation is not DiscardDesignationSyntax,
        VarPatternSyntax varPattern => varPattern.Designation is not DiscardDesignationSyntax,
        RecursivePatternSyntax recursivePattern => 
            recursivePattern.Designation is {} and not DiscardDesignationSyntax ||
            (recursivePattern.PositionalPatternClause is {} positionalPattern && 
                positionalPattern.Subpatterns.Any(subPattern => IsPatternWithDeclaration(subPattern.Pattern))) ||
            recursivePattern.PropertyPatternClause is {} propertyPattern && 
                propertyPattern.Subpatterns.Any(subPattern => IsPatternWithDeclaration(subPattern.Pattern)),
        BinaryPatternSyntax binaryPattern => IsPatternWithDeclaration(binaryPattern.Left) || IsPatternWithDeclaration(binaryPattern.Right),
        ParenthesizedPatternSyntax parenthesizedPattern => IsPatternWithDeclaration(parenthesizedPattern.Pattern),
        _ => false
    };
}