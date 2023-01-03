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

                var firstFieldTypeModel = wrappedFieldDecls.Count == 1 ? TypeSymbolFor(wrappedFieldDecls[0].type, semanticModel) : null;
                var isVector = firstFieldTypeModel is INamedTypeSymbol { Name: "Vector3" };
                var isQuaternion = firstFieldTypeModel is INamedTypeSymbol { Name: "Quaternion" };
                var isUnity = firstFieldTypeModel is INamedTypeSymbol { ContainingNamespace.Name: "UnityEngine" };

                var wrappedType = isVector 
                    ? WrappedType.Vector 
                    : isQuaternion ? WrappedType.Quaternion : WrappedType.Other;
                
                var allMembers = new List<MemberDeclarationSyntax>();
                var extensionMethods = new List<MemberDeclarationSyntax>();

                if(wrappedFieldDecls.Count == 1)
                    allMembers.AddRange(mkAccessorProperties(wrappedType, wrappedFieldDecls[0].identifier, hasMagnitude, isUnity));
                
                allMembers.AddRange(mkMapAndScale(typeDecl, areFieldsIndependent, isVector, wrappedFieldDecls, hasMagnitude && hasOrientation, isUnity));
                if (isVector && areFieldsIndependent) {
                    allMembers.AddRange(mkWithFieldMethods(typeDecl, wrappedFieldDecls[0].identifier, isUnity));
                    allMembers.AddRange(mkMinMax(typeDecl, wrappedFieldDecls[0].identifier));
                }

                if (isVector) {
                    if (areFieldsIndependent) allMembers.AddRange(mkLerp(typeDecl, wrappedFieldDecls[0]));
                    if (hasOrientation) allMembers.AddRange(mkSlerp(typeDecl, wrappedFieldDecls[0]));
                } else if (isQuaternion)
                    allMembers.AddRange(mkSlerp(typeDecl, wrappedFieldDecls[0]));
                
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
                allMembers.Add(mkIsNearly(typeDecl, wrappedFieldDecls));
                
                // TODO DS: Equzality
                extensionMethods.Add(mkAverage(typeDecl, wrappedFieldDecls));
                
                // if fields are independent, make map and with
                // if has magnitude, make scale?, Length?, Min, Max
                var result = typeDecl
                    .WithBaseList(null)
                    .WithAttributeLists(List(Enumerable.Empty<AttributeListSyntax>()))
                    .ClearTrivia()
                    .WithMembers(List(allMembers));
                var resultStaticClass = ClassDeclaration($"{typeDecl.Identifier.Text}Extensions")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword)))
                    .WithMembers(List(extensionMethods));

                var allClasses = extensionMethods.Count > 0
                    ? new MemberDeclarationSyntax[] { result, resultStaticClass }
                    : new MemberDeclarationSyntax[] { result };
                
                var sourceText = SourceText.From(List(allClasses)
                    .WrapInParentsOf(typeDecl).NormalizeWhitespace().ToFullString(), Encoding.UTF8);
                
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

    private IEnumerable<MemberDeclarationSyntax> mkAccessorProperties(WrappedType wrappedType, SyntaxToken wrappedIdentifier, bool hasMagnitude, bool isUnity) {
        MemberDeclarationSyntax mk(string field) {
            var fieldMember = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(wrappedIdentifier),
                IdentifierName(isUnity ? field.ToLower() : field));
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

    private MethodDeclarationSyntax mkCopyMethod(TypeDeclarationSyntax typeDecl, SyntaxToken identifier, ParameterListSyntax parameters, ArgumentListSyntax arg, bool isStatic = false) => 
        MethodDeclaration(
            attributeLists: AttributeList(Pure),
            modifiers: isStatic ? TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)) : TokenList(Token(SyntaxKind.PublicKeyword)),
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

    private IEnumerable<MemberDeclarationSyntax> mkMapAndScale(TypeDeclarationSyntax typeDecl,
        bool areFieldsIndependent, bool isVector,
        IReadOnlyList<(TypeSyntax type, SyntaxToken id)> fields, bool hasMagnitudeAndDirection, bool isUnity) {
        if (fields.Count > 1) {
            for (var i = 0; i < fields.Count; i++) {
                var fieldNameWithoutUnderscore = fields[i].id.Text.Trim('_');
                yield return mkCopyMethod( // e.g. pose.WithPosition(GlobalPosition => GlobalPosition)
                    typeDecl,
                    Identifier($"With{char.ToUpper(fieldNameWithoutUnderscore[0]) + fieldNameWithoutUnderscore[1..]}"),
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
                        )).Concat(fields.Skip(i + 1).Select(f => Argument(IdentifierName(f.id)))))
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
            if(isVector && areFieldsIndependent) {
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
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName(isUnity ? "x" : "X")))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName(isUnity ? "y" : "Y")))))),
                        Argument(InvocationExpression(IdentifierName("mapping"), ArgumentList(Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(fieldId), IdentifierName(isUnity ? "z" : "Z"))))))
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

    private IEnumerable<MethodDeclarationSyntax> mkWithFieldMethods(TypeDeclarationSyntax typeDecl, SyntaxToken fieldId, bool isUnity) {
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
                            IdentifierName(isUnity ? a.ToLower() : a)));
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

     // Assumes all fields are have a `.IsNearly(other, double)`
     private MethodDeclarationSyntax mkIsNearly(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields) =>
         MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier("IsNearly"))
             .WithAttributeLists(AttributeList(Pure))
             .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
             .WithParameterList(ParameterList(
                 Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
                 Parameter(Identifier("threshold"))
                     .WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword)))
                     .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1E-6))))
             ))
             .WithExpressionBody(ArrowExpressionClause(fields
                 .Select(f => (ExpressionSyntax) InvocationExpression(
                     MemberAccessExpression(
                         SyntaxKind.SimpleMemberAccessExpression,
                         IdentifierName(f.identifier),
                         IdentifierName("IsNearly")),
                     ArgumentList(
                         Argument(MemberAccessExpression(
                             SyntaxKind.SimpleMemberAccessExpression,
                             IdentifierName("other"),
                             IdentifierName(f.identifier)
                         )),
                         Argument(IdentifierName("threshold")
                     ))))
                 .Aggregate((res, single) => BinaryExpression(
                     SyntaxKind.LogicalAndExpression,
                     res, single
                 ))
             ))
             .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
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
        yield return mkCopyMethod(typeDecl, Identifier("Lerp"), ParameterList(
            Parameter(Identifier("from")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("to")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("t")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
            Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))
        ), ArgumentList(Argument(InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                    IdentifierName("from"),
                    IdentifierName(wrappedFieldDecl.identifier)
                ),
                IdentifierName("LerpTo")
            ),
            ArgumentList(
                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("to"), IdentifierName(wrappedFieldDecl.identifier))),
                Argument(IdentifierName("t")),
                Argument(IdentifierName("shouldClamp"))
            )
        ))), isStatic: true);
    }

    private IEnumerable<MemberDeclarationSyntax> mkSlerp(TypeDeclarationSyntax typeDecl, (TypeSyntax type, SyntaxToken identifier) wrappedFieldDecl) {
        yield return mkCopyMethod(typeDecl, Identifier("SlerpTo"), ParameterList(
            Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("t")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
            Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))
        ), ArgumentList(Argument(InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                IdentifierName(wrappedFieldDecl.identifier),
                IdentifierName("SlerpTo")
            ),
            ArgumentList(
                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("other"), IdentifierName(wrappedFieldDecl.identifier))),
                Argument(IdentifierName("t")),
                Argument(IdentifierName("shouldClamp"))
            )
        ))));
        yield return mkCopyMethod(typeDecl, Identifier("Slerp"), ParameterList(
            Parameter(Identifier("from")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("to")).WithType(IdentifierName(typeDecl.Identifier)),
            Parameter(Identifier("t")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
            Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))
        ), ArgumentList(Argument(InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                    IdentifierName("from"),
                    IdentifierName(wrappedFieldDecl.identifier)
                ),
                IdentifierName("SlerpTo")
            ),
            ArgumentList(
                Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("to"), IdentifierName(wrappedFieldDecl.identifier))),
                Argument(IdentifierName("t")),
                Argument(IdentifierName("shouldClamp"))
            )
        ))), isStatic: true);
    }

    private MemberDeclarationSyntax mkAverage(
        TypeDeclarationSyntax typeDecl,
        IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields
    ) => mkCopyMethod(
        typeDecl,
        Identifier("Average"),
        ParameterList(Parameter(Identifier("enumerable"))
            .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
            .WithType(GenericName("IEnumerable").WithTypeArgumentList(TypeArgumentList(IdentifierName(typeDecl.Identifier))))),
        ArgumentList(fields.Select(t => Argument(
            InvocationExpression(MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("enumerable"),
                    IdentifierName("Select")
                )).WithArgumentList(ArgumentList(Argument(
                    SimpleLambdaExpression(Parameter(Identifier("e")))
                        .WithExpressionBody(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("e"),
                            IdentifierName(t.identifier)))))),
                IdentifierName("Average")
            ))))),
        isStatic: true
    );

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