using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BII.WasaBii.UnitSystem; 

public static class CodeGenerationUtils {
    
    public static (string prefix, int indentation, string postfix) MkContext(SyntaxNode? part, bool includeSelf = false)
        => (includeSelf ? part : part?.Parent) switch {
            TypeDeclarationSyntax tds when MkContext(includeSelf ? tds.Parent : tds) is {prefix:var pre, indentation:var i, postfix:var post}
                => ($"{pre}{Indent(i)}{tds} {{\n", i + 1, $"}}\n{Indent(i)}{post}"),
            NamespaceDeclarationSyntax nds => ($"{nds}{{\n", 1, "}"),
            _ => ("", 0, "")
        };

    public static string Indent(int depth) => string.Join("", Enumerable.Repeat("    ", depth));

}
