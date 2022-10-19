using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WasaBii_Geometry_Generator; 

using static SyntaxFactory;

public static class SyntaxFactoryUtils {

    public static AttributeSyntax Pure => Attribute(IdentifierName("Pure"));
    
    public static SyntaxList<AttributeListSyntax> AttributeList(IEnumerable<AttributeSyntax> attributes) =>
        SingletonList(SyntaxFactory.AttributeList(SeparatedList(attributes)));

    public static SyntaxList<AttributeListSyntax> AttributeList(params AttributeSyntax[] attributes) =>
        AttributeList((IEnumerable<AttributeSyntax>) attributes);

    public static SyntaxList<AttributeListSyntax> NoAttributes => List<AttributeListSyntax>();

    public static ParameterListSyntax ParameterList(IEnumerable<ParameterSyntax> parameters) =>
        SyntaxFactory.ParameterList(SeparatedList(parameters));

    public static ParameterListSyntax ParameterList(params ParameterSyntax[] parameters) =>
        ParameterList((IEnumerable<ParameterSyntax>) parameters);

    public static ParameterListSyntax NoParameters => ParameterList();

    public static ArgumentListSyntax ArgumentList(IEnumerable<ArgumentSyntax> arguments) =>
        SyntaxFactory.ArgumentList(SeparatedList(arguments));

    public static ArgumentListSyntax ArgumentList(params ArgumentSyntax[] arguments) =>
        ArgumentList((IEnumerable<ArgumentSyntax>) arguments);

    public static ArgumentListSyntax NoArguments => ArgumentList();

    public static TypeArgumentListSyntax TypeArgumentList(IEnumerable<TypeSyntax> typeArguments) =>
        SyntaxFactory.TypeArgumentList(SeparatedList(typeArguments));

    public static TypeArgumentListSyntax TypeArgumentList(params TypeSyntax[] typeArguments) =>
        TypeArgumentList((IEnumerable<TypeSyntax>) typeArguments);

}