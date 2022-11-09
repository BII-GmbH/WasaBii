using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BII.WasaBii.Geometry.Generator;

using static SyntaxFactory;

public static class CodeGenerationUtils {
    
    private static readonly UsingDirectiveSyntax[] defaultIncludes = new string[]{
        "System.Diagnostics.Contracts",
        "System"
    }.Select(str => UsingDirective(ParseName(str)).NormalizeWhitespace().AppendTrivia(LineFeed)).ToArray();

    public static SyntaxNode WrapInParentsOf<T>(this T node, SyntaxNode other) where T : MemberDeclarationSyntax => other.Parent switch {
        CompilationUnitSyntax cus => cus
            .WithMembers(SingletonList<MemberDeclarationSyntax>(node))
            .WithUsings(cus.Usings.AddRange(defaultIncludes.Where(incl => !cus.Usings.Any(u => u.Name.ToString() == incl.Name.ToString())))),
        NamespaceDeclarationSyntax nds => nds.WithMembers(SingletonList<MemberDeclarationSyntax>(node)).WrapInParentsOf(nds),
        TypeDeclarationSyntax tds => tds.ClearTrivia().WithMembers(SingletonList<MemberDeclarationSyntax>(node)).WrapInParentsOf(tds),
        var p => throw new Exception($"Unsupported node type for wrapping: {p?.GetType()}")
    };

    public static T ClearTrivia<T>(this T node) where T : TypeDeclarationSyntax
        => (T) node
            .WithoutTrivia()
            .WithOpenBraceToken(node.OpenBraceToken.WithoutTrivia())
            .WithCloseBraceToken(node.CloseBraceToken.WithLeadingTrivia(LineFeed).WithTrailingTrivia());

    public static NamespaceDeclarationSyntax ClearTrivia(this NamespaceDeclarationSyntax node)
        => node
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

    public static IEnumerable<TSyntax> PrependTrivia<TSyntax>(
        this IEnumerable<TSyntax> nodes,
        params SyntaxTrivia[] trivia
    ) where TSyntax : SyntaxNode {
        using var e = nodes.GetEnumerator();
        if (e.MoveNext()) {
            yield return e.Current!.PrependTrivia(trivia);
            while (e.MoveNext()) yield return e.Current!;
        }
    }
    
    public static IEnumerable<TSyntax> AppendTrivia<TSyntax>(
        this IEnumerable<TSyntax> nodes,
        params SyntaxTrivia[] trivia
    ) where TSyntax : SyntaxNode {
        using var e = nodes.GetEnumerator();
        if (e.MoveNext()) {
            var cur = e.Current!;
            while (e.MoveNext()) {
                yield return cur;
                cur = e.Current!;
            }
            yield return cur.AppendTrivia(trivia);
        }
    }

}
