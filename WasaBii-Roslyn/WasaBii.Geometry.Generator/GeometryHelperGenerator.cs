using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using BII.WasaBii.Geometry.Shared;

namespace BII.WasaBii.Geometry.Generator;

using static SyntaxFactory;
using static SyntaxFactoryUtils;

[Generator]
public class GeometryHelperGenerator : ISourceGenerator {
    
    private static readonly DiagnosticDescriptor UnexpectedGenerationIssue = new(
        id: "WasaBiiGeometryHelpers",
        title: "Unexpected Geometry Helper Generation Issue",
        messageFormat: "Unexpected issue while generating geometry utility source code:\n{0}",
        category: "WasaBii",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(GeneratorInitializationContext context) =>
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

    private enum WrappedType {
        Vector, Quaternion, Other
    }

    public void Execute(GeneratorExecutionContext context) {
        // Ensure proper printing of decimal constants as valid C# code
        var origCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        
        foreach (var (typeDecl, attribute) in ((SyntaxReceiver)context.SyntaxReceiver!).GeometryHelpers) {
            try {
                var wrappedFieldDecls = typeDecl.Members.OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword))
                    .Select(f => f.Declaration)
                    .SelectMany(decl => decl.Variables.Select(variable => (type: decl.Type, identifier: variable.Identifier)))
                    .Concat(typeDecl.Members.OfType<PropertyDeclarationSyntax>()
                        .Where(p => p.Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword)
                            && (p.AccessorList?.Accessors.Any(a => a.Kind() 
                                is SyntaxKind.GetAccessorDeclaration && a.Body == null && a.ExpressionBody == null) ?? false))
                        .Select(p => (type: p.Type, identifier: p.Identifier))
                    ).ToList();

                var semanticModel = context.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
                var attributeData = semanticModel.GetDeclaredSymbol(typeDecl)!.GetAttributes()
                    .First(a => a.AttributeClass!.Name == nameof(GeometryHelper));
                var attributeArguments = attributeData.GetArgumentValues().ToArray();
                
                var areFieldsIndependent = (bool) attributeArguments.First(a => a.Name == "areFieldsIndependent").Value;
                var hasMagnitude = (bool) attributeArguments.First(a => a.Name == "hasMagnitude").Value;
                var hasOrientation = (bool) attributeArguments.First(a => a.Name == "hasOrientation").Value;

                var isVector = wrappedFieldDecls.Count == 1 && TypeSymbolFor(wrappedFieldDecls[0].type, semanticModel) 
                    is INamedTypeSymbol { Name: "Vector3" };
                
                var isQuaternion = wrappedFieldDecls.Count == 1 && TypeSymbolFor(wrappedFieldDecls[0].type, semanticModel) 
                    is INamedTypeSymbol { Name: "Quaternion" };

                var wrappedType = isVector 
                    ? WrappedType.Vector 
                    : isQuaternion ? WrappedType.Quaternion : WrappedType.Other;
                
                var allMembers = new List<MemberDeclarationSyntax>();

                if(wrappedFieldDecls.Count == 1)
                    allMembers.AddRange(mkAccessorProperties(wrappedType, wrappedFieldDecls[0].identifier, hasMagnitude));
                
                allMembers.AddRange(mkMapAndScale(typeDecl, areFieldsIndependent, wrappedFieldDecls, hasMagnitude && hasOrientation));
                if (areFieldsIndependent) {
                    if(isVector) {
                        allMembers.AddRange(mkWithFieldMethods(typeDecl, wrappedFieldDecls[0].identifier));
                        allMembers.AddRange(mkMinMax(typeDecl, wrappedFieldDecls[0].identifier));
                    }
                    // TODO DS: Lerp as static utility
                    // allMembers.Add(mkLerp(typeDecl, allFields));
                }

                if (isVector || isQuaternion) {
                    if (areFieldsIndependent) allMembers.AddRange(mkLerp(typeDecl, wrappedFieldDecls[0]));
                    if (hasOrientation) allMembers.AddRange(mkSlerp(typeDecl, wrappedFieldDecls[0]));
                }
                
                // if(fieldType is not FieldType.Other){
                //     allMembers.Add(mkDotProduct(typeDecl, allFields.Select(f => f.identifier), fieldType));
                //
                //     if (hasMagnitude) {
                //         allMembers.Add(mkSqrMagnitude(allFields.Select(f => f.identifier)));
                //         allMembers.Add(mkMagnitude());
                //     }
                
                if (isVector && hasOrientation) {
                    allMembers.Add(mkAngleTo(typeDecl, hasMagnitude));
                }
                // }
                // allMembers.Add(mkSlerp(typeDecl, allFields, isBasic));
                // allMembers.Add(mkIsNearly(typeDecl, allFields, semanticModel, fieldType));
                
                // TODO DS: Equzality
                // TODO DS: Average extension method
                
                // if fields are independent, make map and with
                // if has magnitude, make scale?, Length?, Min, Max
                // isNearly, Lerp, Slerp
                var result = typeDecl
                    .WithBaseList(null)
                    .WithAttributeLists(List(Enumerable.Empty<AttributeListSyntax>()))
                    .ClearTrivia()
                    .WithMembers(List(allMembers));
                
                var sourceText = SourceText.From(result.WrapInParentsOf(typeDecl).NormalizeWhitespace().ToFullString(), Encoding.UTF8);
                
                context.AddSource(
                    $"{typeDecl.Identifier.Text}.g.cs",
                    sourceText
                );
            }
            catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(UnexpectedGenerationIssue, Location.None, e.Message));
            }
        }
        Thread.CurrentThread.CurrentCulture = origCulture;
    }

    private IEnumerable<MemberDeclarationSyntax> mkAccessorProperties(WrappedType wrappedType, SyntaxToken wrappedIdentifier, bool hasMagnitude) {
        MemberDeclarationSyntax mk(string field) {
            var fieldMember = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(wrappedIdentifier),
                IdentifierName(field));
            return PropertyDeclaration(
                    type: hasMagnitude ? IdentifierName("Length") : PredefinedType(Token(SyntaxKind.FloatKeyword)),
                    identifier: field
                ).WithExpressionBody(ArrowExpressionClause(
                    hasMagnitude ? InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, fieldMember, IdentifierName("Meters"))) : fieldMember
                ))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        return wrappedType switch {
            WrappedType.Vector => new[]{"X", "Y", "Z"}.Select(mk),
            WrappedType.Quaternion => new[]{"X", "Y", "Z", "W"}.Select(mk),
            _ => Enumerable.Empty<MemberDeclarationSyntax>()
        };
    }

    private MethodDeclarationSyntax mkCopyMethod(TypeDeclarationSyntax typeDecl, SyntaxToken identifier, ParameterListSyntax parameters, ArgumentListSyntax arg) => 
        MethodDeclaration(
            attributeLists: AttributeList(Pure),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
            returnType: IdentifierName(typeDecl.Identifier),
            explicitInterfaceSpecifier:null,
            identifier,
            typeParameterList:null,
            parameters,
            constraintClauses: List<TypeParameterConstraintClauseSyntax>(),
            body: null,
            expressionBody: ArrowExpressionClause(makeCopy(arg))
        ).WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    private ExpressionSyntax makeCopy(ArgumentListSyntax arg) => 
        ImplicitObjectCreationExpression().WithArgumentList(arg);

    private sealed class CompareTypeSyntaxByNane : IEqualityComparer<TypeSyntax> {
        public bool Equals(TypeSyntax x, TypeSyntax y) => x.ToFullString().Equals(y.ToFullString());

        public int GetHashCode(TypeSyntax obj) => obj.ToFullString().GetHashCode();
    }

    private IEnumerable<MemberDeclarationSyntax> mkMapAndScale(
        TypeDeclarationSyntax typeDecl, bool areFieldsIndependent, 
        IReadOnlyList<(TypeSyntax type, SyntaxToken id)> fields, bool hasMagnitudeAndDirection
    ) {
        if (fields.Count > 1) {
            for (var i = 0; i < fields.Count; i++) {
                yield return mkCopyMethod( // e.g. pose.MapPosition(GlobalPosition => GlobalPosition)
                    typeDecl,
                    Identifier($"Map{fields[i].id}"),
                    ParameterList(Parameter(
                        List(Enumerable.Empty<AttributeListSyntax>()),
                        TokenList(),
                        GenericName(Identifier("Func"), TypeArgumentList(fields[i].type, fields[i].type)),
                        Identifier("mapping"),
                        null
                    )),
                    ArgumentList(fields.Take(i).Select(f => Argument(IdentifierName(f.id))).Append(
                        Argument(InvocationExpression(
                            IdentifierName("mapping"),
                            ArgumentList(Argument(IdentifierName(fields[i].id))))
                        )).Concat(fields.Skip(i + 2).Select(f => Argument(IdentifierName(f.id)))))
                );
            }
        } else {
            var (fieldType, fieldId) = fields[0];
            yield return mkCopyMethod( // e.g. pos.Map(Vector3 => Vector3)
                typeDecl,
                Identifier("Map"),
                ParameterList(Parameter(
                    List(Enumerable.Empty<AttributeListSyntax>()),
                    TokenList(),
                    GenericName(Identifier("Func"), TypeArgumentList(fieldType, fieldType)),
                    Identifier("mapping"),
                    null
                )),
                ArgumentList(Argument(InvocationExpression(
                    IdentifierName("mapping"),
                    ArgumentList(Argument(IdentifierName(fieldId)))
                )))
            );
            if(areFieldsIndependent) {
                yield return mkCopyMethod( // e.g. pos.Map(Length => Length)
                    typeDecl,
                    Identifier("Map"),
                    ParameterList(Parameter(
                        List(Enumerable.Empty<AttributeListSyntax>()),
                        TokenList(),
                        GenericName(Identifier("Func"), TypeArgumentList(IdentifierName("Length"), IdentifierName("Length"))),
                        Identifier("mapping"),
                        null
                    )),
                    ArgumentList(
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(IdentifierName("X"))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(IdentifierName("Y"))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(IdentifierName("Z")))))
                    )
                );
                yield return mkCopyMethod( // e.g. pos.Map(float => float)
                    typeDecl,
                    Identifier("Map"),
                    ParameterList(Parameter(
                        List(Enumerable.Empty<AttributeListSyntax>()),
                        TokenList(),
                        GenericName(Identifier("Func"), TypeArgumentList(PredefinedType(Token(SyntaxKind.FloatKeyword)), PredefinedType(Token(SyntaxKind.FloatKeyword)))),
                        Identifier("mapping"),
                        null
                    )),
                    ArgumentList(
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName("X")))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName("Y")))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName("Z"))))))
                    )
                );
            }
            if(hasMagnitudeAndDirection) {
                var result = IdentifierName(typeDecl.Identifier);
                var valueParameter = Parameter(Identifier("value")).WithType(IdentifierName(typeDecl.Identifier));
                var factorParameter = Parameter(Identifier("factor"))
                    .WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword)));
                var divisorParameter = Parameter(Identifier("divisor"))
                    .WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword)));
                var map = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("value"),
                    IdentifierName("Map"));

                ExpressionSyntax mkMapping(ExpressionSyntax inner) => InvocationExpression(
                    map,
                    ArgumentList(Argument(
                        SimpleLambdaExpression(Parameter(Identifier("l")))
                            .WithExpressionBody(inner))));
                foreach (var op in mkSymmetricBinaryOperator(
                     result, token: Token(SyntaxKind.AsteriskToken),
                     parameter1: factorParameter,
                     parameter2: valueParameter,
                     expression: mkMapping(BinaryExpression(
                         SyntaxKind.MultiplyExpression,
                         IdentifierName("factor"),
                         IdentifierName("l"))))
                ) yield return op;
                yield return mkBinaryOperator(
                    result, Token(SyntaxKind.SlashToken),
                    valueParameter,
                    divisorParameter,
                    expression: mkMapping(BinaryExpression(
                        SyntaxKind.DivideExpression,
                        IdentifierName("l"),
                        IdentifierName("divisor"))));
            }
        }
    }

    private static OperatorDeclarationSyntax mkBinaryOperator(TypeSyntax result, SyntaxToken token, ParameterSyntax parameter1, ParameterSyntax parameter2, ExpressionSyntax expression) =>
        OperatorDeclaration(result, token)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(parameter1, parameter2))
            .WithExpressionBody(ArrowExpressionClause(expression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static IEnumerable<OperatorDeclarationSyntax> mkSymmetricBinaryOperator(TypeSyntax result, SyntaxToken token, ParameterSyntax parameter1, ParameterSyntax parameter2, ExpressionSyntax expression) {
        yield return mkBinaryOperator(result, token, parameter1, parameter2, expression);
        yield return mkBinaryOperator(result, token, parameter2, parameter1, expression);
    }

    private IEnumerable<MethodDeclarationSyntax> mkWithFieldMethods(TypeDeclarationSyntax typeDecl, SyntaxToken fieldId) {
        var fields = new[] { "X", "Y", "Z" };
        for(var i = 0; i < fields.Length; i++)
            foreach (var isLength in new[]{ true, false }) {
                TypeSyntax type = isLength ? IdentifierName("Length") : PredefinedType(Token(SyntaxKind.FloatKeyword));

                IEnumerable<ExpressionSyntax> asArguments(IEnumerable<string> args)
                    => args.Select(a => isLength
                        ? (ExpressionSyntax) IdentifierName(a)
                        : MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(fieldId),
                            IdentifierName(a)));
                MethodDeclarationSyntax mkMethod(ParameterSyntax parameter, ExpressionSyntax constructorArgument) => 
                    mkCopyMethod(
                        typeDecl,
                        identifier: Identifier($"With{fields[i]}"),
                        parameters: ParameterList(parameter),
                        ArgumentList(
                            asArguments(fields.Take(i))
                                .Append(constructorArgument)
                                .Concat(asArguments(fields.Skip(i + 1)))
                                .Select(Argument)
                        )
                        
                    );

                yield return mkMethod(
                    Parameter(
                        List(Enumerable.Empty<AttributeListSyntax>()),
                        TokenList(),
                        type,
                        Identifier(fields[i].ToLower()),
                        null
                    ), 
                    IdentifierName(fields[i].ToLower()));
                yield return mkMethod(
                    Parameter(
                        List(Enumerable.Empty<AttributeListSyntax>()),
                        TokenList(),
                        GenericName(Identifier("Func"), TypeArgumentList(type, type)),
                        Identifier("mapping"),
                        null
                    ), 
                    InvocationExpression(
                        IdentifierName("mapping"),
                        ArgumentList(asArguments(new []{fields[i]}).Select(Argument)))
                    );
            }
    }

    /// Assumes all fields have a `.Min` and `.Max` (extension) method
    private IEnumerable<MethodDeclarationSyntax> mkMinMax(TypeDeclarationSyntax typeDecl, SyntaxToken field) {
        MethodDeclarationSyntax make(string operation) => mkCopyMethod(
            typeDecl,
            Identifier(operation),
            ParameterList(Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier))),
            ArgumentList(Argument(InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("other"),
                        IdentifierName(field)
                    ),
                    IdentifierName(operation)
                ),
                ArgumentList(Argument(IdentifierName(field)))
            )))
        );

        yield return make("Min");
        yield return make("Max");
    }

    /// Assumes all fields are either `Length` or have a `.IsNearly(other, double)`
    // private MethodDeclarationSyntax mkIsNearly(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields, SemanticModel semanticModel, FieldType fieldType) =>
    //     MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier("IsNearly"))
    //         .WithAttributeLists(AttributeList(Pure))
    //         .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
    //         .WithParameterList(ParameterList(
    //             Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
    //             Parameter(Identifier("threshold"))
    //                 .WithType(PredefinedType(Token(SyntaxKind.FloatKeyword)))
    //                 .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1E-6f))))
    //         ))
    //         .WithExpressionBody(ArrowExpressionClause(fields
    //             .Select(f => (ExpressionSyntax) InvocationExpression(
    //                 MemberAccessExpression(
    //                     SyntaxKind.SimpleMemberAccessExpression,
    //                     IdentifierName(f.identifier),
    //                     IdentifierName("IsNearly")),
    //                 ArgumentList(
    //                     Argument(MemberAccessExpression(
    //                         SyntaxKind.SimpleMemberAccessExpression,
    //                         IdentifierName("other"),
    //                         IdentifierName(f.identifier)
    //                     )),
    //                     Argument(fieldType switch {
    //                         FieldType.Float or FieldType.Other => IdentifierName("threshold"),
    //                         FieldType.Double => CastExpression(PredefinedType(Token(SyntaxKind.DoubleKeyword)), IdentifierName("threshold")),
    //                         FieldType.Length => InvocationExpression(
    //                             MemberAccessExpression(
    //                                 SyntaxKind.SimpleMemberAccessExpression,
    //                                 IdentifierName("threshold"),
    //                                 IdentifierName("Meters"))),
    //                         _ => throw new InvalidEnumArgumentException(nameof(fieldType), (int)fieldType, typeof(FieldType))
    //                     }
    //                 ))))
    //             .Aggregate((res, single) => BinaryExpression(
    //                 SyntaxKind.LogicalAndExpression,
    //                 res, single
    //             ))
    //         ))
    //         .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    private MemberDeclarationSyntax mkAngleTo(TypeDeclarationSyntax typeDecl, bool hasMagnitude) {
        var dotProductRes = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                IdentifierName("Dot"))
        ).WithArgumentList(ArgumentList(Argument(IdentifierName("other"))));
        
        return MethodDeclaration(IdentifierName("Angle"), Identifier("AngleTo"))
            .WithAttributeLists(AttributeList(Pure))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(
                Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier))))
            .WithExpressionBody(ArrowExpressionClause(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Angles"),
                        IdentifierName("Acos")))
                .WithArgumentList(ArgumentList(Argument(
                    hasMagnitude
                    ? BinaryExpression(
                        SyntaxKind.DivideExpression,
                        dotProductRes,
                        ParenthesizedExpression(
                            BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ThisExpression(),
                                    IdentifierName("Magnitude")),
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("other"),
                                    IdentifierName("Magnitude")))))
                    : dotProductRes)))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private IEnumerable<MemberDeclarationSyntax> mkLerp(TypeDeclarationSyntax typeDecl, (TypeSyntax type, SyntaxToken identifier) wrappedFieldDecl) {
        yield return mkCopyMethod(typeDecl, Identifier("LerpTo"), ParameterList(
            Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("t")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
            Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))
        ), ArgumentList(Argument(InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                IdentifierName(wrappedFieldDecl.identifier),
                IdentifierName("LerpTo")
            ),
            ArgumentList(
                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("other"), IdentifierName(wrappedFieldDecl.identifier))),
                Argument(IdentifierName("t")),
                Argument(IdentifierName("shouldClamp"))
            )
        ))));
    }

    private IEnumerable<MemberDeclarationSyntax> mkSlerp(TypeDeclarationSyntax typeDecl, (TypeSyntax type, SyntaxToken identifier) wrappedFieldDecl) {
        yield return mkCopyMethod(typeDecl, Identifier("SlerpTo"), ParameterList(
            Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("t")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
            Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))
        ), ArgumentList(Argument(InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                IdentifierName(wrappedFieldDecl.identifier),
                IdentifierName("LerpTo")
            ),
            ArgumentList(
                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("other"), IdentifierName(wrappedFieldDecl.identifier))),
                Argument(IdentifierName("t")),
                Argument(IdentifierName("shouldClamp"))
            )
        ))));
    }

    private sealed class SyntaxReceiver : ISyntaxReceiver {

        public sealed record GeometryHelperData(TypeDeclarationSyntax Type, AttributeSyntax Attribute);

        public readonly HashSet<GeometryHelperData> GeometryHelpers = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if (syntaxNode is TypeDeclarationSyntax tds &&
                tds.ChildTokens().Any(t => t.IsKind(SyntaxKind.PartialKeyword)) &&
                tds.AttributeLists.SelectMany(a => a.Attributes)
                    .FirstOrDefault(a => a.Name.ToString() == nameof(GeometryHelper)) is {} attribute) 
                GeometryHelpers.Add(new GeometryHelperData(tds, attribute));
        }
    }
    
    private static ITypeSymbol TypeSymbolFor(SyntaxNode node, SemanticModel semanticModel) => 
        semanticModel.GetTypeInfo(node).ConvertedType!;

}