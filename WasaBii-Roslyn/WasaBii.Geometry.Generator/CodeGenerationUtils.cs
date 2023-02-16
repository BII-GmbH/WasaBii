using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BII.WasaBii.Geometry.Generator;

using static SyntaxFactory;

public static class CodeGenerationUtils {

    private static readonly UsingDirectiveSyntax[] defaultIncludes = new[]{
        "BII.WasaBii.Core",
        "System",
        "System.Collections.Generic",
        "System.Linq",
    }.Select(str => UsingDirective(ParseName(str)).NormalizeWhitespace().AppendTrivia(LineFeed)).ToArray();

    public static SyntaxNode WrapInParentsOf<T>(this T node, SyntaxNode other) where T : MemberDeclarationSyntax =>
        SingletonList<MemberDeclarationSyntax>(node).WrapInParentsOf(other);

    public static SyntaxNode WrapInParentsOf(this SyntaxList<MemberDeclarationSyntax> nodes, SyntaxNode other) => other.Parent switch {
        CompilationUnitSyntax cus => cus
            .WithMembers(nodes)
            .WithUsings(cus.Usings.AddRange(defaultIncludes.Where(incl => !cus.Usings.Any(u => u.Name.ToString() == incl.Name.ToString())))),
        NamespaceDeclarationSyntax nds => nds.WithMembers(nodes).WrapInParentsOf(nds),
        TypeDeclarationSyntax tds => tds.ClearTrivia().WithMembers(nodes).WrapInParentsOf(tds),
        var p => throw new Exception($"Unsupported node type for wrapping: {p?.GetType()}")
    };

    public static IEnumerable<T> MapFirst<T>(this IEnumerable<T> enumerable, Func<T, T> f) {
        using var enumerator = enumerable.GetEnumerator();
        if (enumerator.MoveNext()) {
            yield return f(enumerator.Current);
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }
    }

    public static T ClearTrivia<T>(this T node) where T : TypeDeclarationSyntax
        => (T) node
            .WithoutTrivia()
            .WithOpenBraceToken(node.OpenBraceToken.WithoutTrivia())
            .WithCloseBraceToken(node.CloseBraceToken.WithLeadingTrivia(LineFeed).WithTrailingTrivia());

    public static TSyntax WithTrailingTrivia<TSyntax>(
        this TSyntax node,
        Func<IEnumerable<SyntaxTrivia>, IEnumerable<SyntaxTrivia>> triviaMutator
    ) where TSyntax : SyntaxNode
        => node.WithTrailingTrivia(triviaMutator(node.GetTrailingTrivia()));
    
    public static TSyntax WithLeadingTrivia<TSyntax>(
        this TSyntax node,
        Func<IEnumerable<SyntaxTrivia>, IEnumerable<SyntaxTrivia>> triviaMutator
    ) where TSyntax : SyntaxNode
        => node.WithLeadingTrivia(triviaMutator(node.GetLeadingTrivia()));

    public static TSyntax AppendTrivia<TSyntax>(
        this TSyntax node,
        params SyntaxTrivia[] trivia
    ) where TSyntax : SyntaxNode
        => node.WithTrailingTrivia(trailing => trailing.Concat(trivia));
    
    public static TSyntax PrependTrivia<TSyntax>(
        this TSyntax node,
        params SyntaxTrivia[] trivia
    ) where TSyntax : SyntaxNode
        => node.WithLeadingTrivia(trivia.Concat);

}
