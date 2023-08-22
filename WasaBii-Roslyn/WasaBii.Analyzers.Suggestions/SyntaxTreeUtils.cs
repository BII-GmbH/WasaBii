using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BII.WasaBii.Analyzers;

internal static class SyntaxTreeUtils {

    public static bool DeclaresOrAssignsVariables(ExpressionSyntax condition) => condition switch {
        // `bool = calcBool()` and `var foo`
        AssignmentExpressionSyntax or DeclarationExpressionSyntax => true,
        // `foo is {...}`
        IsPatternExpressionSyntax ip => IsPatternWithDeclaration(ip.Pattern),
        // `foo(...)`
        InvocationExpressionSyntax invocation => DeclaresOrAssignsVariables(invocation.Expression) || 
            invocation.ArgumentList.Arguments.Any(argument => 
                // stuff nested in the arguments
                DeclaresOrAssignsVariables(argument.Expression) || 
                // `foo(out bar)`
                argument.RefOrOutKeyword != default && argument.Expression is not IdentifierNameSyntax{Identifier.Text:"_"}),
        // e.g. `foo && bar`
        BinaryExpressionSyntax binaryExpression => DeclaresOrAssignsVariables(binaryExpression.Left) || 
            DeclaresOrAssignsVariables(binaryExpression.Right),
        // e.g. `!foo`
        PrefixUnaryExpressionSyntax unary => DeclaresOrAssignsVariables(unary.Operand),
        //`(foo)`
        ParenthesizedExpressionSyntax parenthesized => DeclaresOrAssignsVariables(parenthesized.Expression),
        // `foo switch {...}`
        // DOES NOT CATCH ASSIGNMENTS IN THE PATTERNS, e.g. 
        // foo switch { var f1 when calcBool(out b) => ... }
        SwitchExpressionSyntax switchExp => DeclaresOrAssignsVariables(switchExp.GoverningExpression) || 
            switchExp.Arms.Any(arm => DeclaresOrAssignsVariables(arm.Expression)),
        _ => false
    };

    public static bool DeclaresVariables(ExpressionSyntax condition) => condition switch {
        // `var foo`
        DeclarationExpressionSyntax => true,
        // `foo is {...}`
        IsPatternExpressionSyntax ip => IsPatternWithDeclaration(ip.Pattern),
        // `foo(...)`
        InvocationExpressionSyntax invocation => DeclaresVariables(invocation.Expression) || 
            invocation.ArgumentList.Arguments.Any(argument => 
                // stuff nested in the arguments or declaration arguments, e.g.
                // foo(out var bar)
                DeclaresVariables(argument.Expression)),
        // e.g. `foo && bar`
        BinaryExpressionSyntax binaryExpression => DeclaresVariables(binaryExpression.Left) || 
            DeclaresVariables(binaryExpression.Right),
        // e.g. `!foo`
        PrefixUnaryExpressionSyntax unary => DeclaresVariables(unary.Operand),
        //`(foo)`
        ParenthesizedExpressionSyntax parenthesized => DeclaresVariables(parenthesized.Expression),
        _ => false
    };

    private static bool IsPatternWithDeclaration(PatternSyntax pattern) => pattern switch {
        // `foo is {...} f`
        DeclarationPatternSyntax declarationPattern => declarationPattern.Designation is not DiscardDesignationSyntax,
        // `foo is {Bar: var bar}`
        VarPatternSyntax varPattern => varPattern.Designation is not DiscardDesignationSyntax,
        // various nesting patterns
        UnaryPatternSyntax unaryPattern => IsPatternWithDeclaration(unaryPattern.Pattern),
        RecursivePatternSyntax recursivePattern => 
            recursivePattern.Designation is not null or DiscardDesignationSyntax ||
            (recursivePattern.PositionalPatternClause is {} positionalPattern && 
                positionalPattern.Subpatterns.Any(subPattern => IsPatternWithDeclaration(subPattern.Pattern))) ||
            recursivePattern.PropertyPatternClause is {} propertyPattern && 
                propertyPattern.Subpatterns.Any(subPattern => IsPatternWithDeclaration(subPattern.Pattern)),
        BinaryPatternSyntax binaryPattern => IsPatternWithDeclaration(binaryPattern.Left) || IsPatternWithDeclaration(binaryPattern.Right),
        ParenthesizedPatternSyntax parenthesizedPattern => IsPatternWithDeclaration(parenthesizedPattern.Pattern),
        _ => false
    };
    
}