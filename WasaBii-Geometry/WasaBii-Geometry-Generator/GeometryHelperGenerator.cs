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

    public void Execute(GeneratorExecutionContext context) {
        // Ensure proper printing of decimal constants as valid C# code
        var origCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        
        try {
            foreach (var (typeDecl, attribute) in ((SyntaxReceiver)context.SyntaxReceiver!).GeometryHelpers) {
                var fieldsDecls = typeDecl.Members.OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword));
                var initializablePropertyDecls = typeDecl.Members.OfType<PropertyDeclarationSyntax>()
                    .Where(p => p.Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword)
                        && (p.AccessorList?.Accessors.Any(a => a.Kind() 
                            is SyntaxKind.InitAccessorDeclaration or SyntaxKind.SetAccessorDeclaration) ?? false));
                var allFields = fieldsDecls.Select(f => f.Declaration)
                    .SelectMany(decl => decl.Variables
                        .Select(variable => (type: decl.Type, identifier: variable.Identifier)))
                    .Concat(initializablePropertyDecls.Select(prop => (type: prop.Type, identifier: prop.Identifier)))
                    .ToArray();
                var semanticModel = context.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
                var attributeData = semanticModel.GetDeclaredSymbol(typeDecl)!.GetAttributes()
                    .First(a => a.AttributeClass!.Name == nameof(GeometryHelper));
                var attributeArguments = attributeData.GetArgumentValues().ToArray();
                
                var areFieldsIndependent = (bool) attributeArguments.First(a => a.Name == "areFieldsIndependent").Value;
                // For some reason, code generation will just silently die if I use `FieldType` as variable type.
                // As long as it's temporary variables only, it seems to work :shrug:
                var fieldType = (FieldType) attributeArguments.First(a => a.Name == "fieldType").Value;
                var hasMagnitude = (bool) attributeArguments.First(a => a.Name == "hasMagnitude").Value;
                var hasDirection = (bool) attributeArguments.First(a => a.Name == "hasDirection").Value;
                
                var allMembers = new List<MemberDeclarationSyntax>();

                if (allFields.Length == 3 &&
                    allFields.TryFind(t => t.identifier.ValueText.ToLower() == "x", out var x) &&
                    allFields.TryFind(t => t.identifier.ValueText.ToLower() == "y", out var y) &&
                    allFields.TryFind(t => t.identifier.ValueText.ToLower() == "z", out var z)) {
                    allMembers.AddRange(mkAsVector(fieldType, x.identifier, y.identifier, z.identifier));
                }
                if (areFieldsIndependent) {
                    allMembers.AddRange(mkMapAndScale(typeDecl, allFields, hasMagnitude));
                    allMembers.AddRange(mkWithFieldMethods(typeDecl, allFields));
                    allMembers.AddRange(mkMinMax(typeDecl, allFields));
                    // TODO DS: Lerp as static utility
                    allMembers.Add(mkLerp(typeDecl, allFields));
                }
                
                if(fieldType is not FieldType.Other){
                    allMembers.Add(mkDotProduct(typeDecl, allFields.Select(f => f.identifier), fieldType));
                
                    if (hasMagnitude) {
                        allMembers.Add(mkSqrMagnitude(allFields.Select(f => f.identifier), fieldType));
                        allMembers.Add(mkMagnitude(fieldType));
                    }
                
                    if (hasDirection) {
                        allMembers.Add(mkAngleTo(typeDecl, hasMagnitude));
                    }
                }
                // allMembers.Add(mkSlerp(typeDecl, allFields, isBasic));
                allMembers.Add(mkIsNearly(typeDecl, allFields, semanticModel, fieldType));
                
                // TODO DS: Equzality
                
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
        }
        catch (Exception e) {
            context.ReportDiagnostic(Diagnostic.Create(UnexpectedGenerationIssue, Location.None, e.Message));
        }
        finally {
            Thread.CurrentThread.CurrentCulture = origCulture;
        }
    }

    private IEnumerable<MemberDeclarationSyntax> mkAsVector(FieldType fieldType, SyntaxToken x, SyntaxToken y, SyntaxToken z) {
        MemberDeclarationSyntax MkVector(TypeSyntax vectorType, string propertyName) => PropertyDeclaration(
            vectorType,
            Identifier(propertyName)
        ).WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithExpressionBody(ArrowExpressionClause(ImplicitObjectCreationExpression()
                .WithArgumentList(ArgumentList(new []{x, y, z}.Select(id => Argument(fieldType switch {
                    FieldType.Float => IdentifierName(id),
                    FieldType.Double => CastExpression(PredefinedType(Token(SyntaxKind.FloatKeyword)), IdentifierName(id)),
                    FieldType.Length => CastExpression(
                        PredefinedType(Token(SyntaxKind.FloatKeyword)), 
                        InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(id),
                            IdentifierName("AsMeters")))
                    ),
                    _ => throw new InvalidEnumArgumentException(nameof(fieldType), (int)fieldType, typeof(FieldType))
                }))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        yield return MkVector(
            QualifiedName(QualifiedName(IdentifierName("System"), IdentifierName("Numerics")), IdentifierName("Vector3")), 
            "AsNumericsVector"
        );
        yield return MkVector(
            QualifiedName(IdentifierName("UnityEngine"), IdentifierName("Vector3")),
            "AsUnityVector"
        ).WithLeadingTrivia(trivia => trivia.Prepend(Trivia(
            IfDirectiveTrivia(IdentifierName(CodeGenerationUtils.UnityCompilerToken), false, true, true))))
        .WithTrailingTrivia(trivia => trivia.Append(Trivia(
            EndIfDirectiveTrivia(false))));
    }

    private MethodDeclarationSyntax mkCopyMethod(TypeDeclarationSyntax typeDecl, SyntaxToken identifier, ParameterListSyntax parameters, IEnumerable<SyntaxToken> fields, IEnumerable<ExpressionSyntax> fieldInitializers) => 
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
            expressionBody: ArrowExpressionClause(makeCopy(fields, fieldInitializers))
        ).WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    private ExpressionSyntax makeCopy(IEnumerable<SyntaxToken> fields, IEnumerable<ExpressionSyntax> fieldInitializers) => 
        ImplicitObjectCreationExpression().WithInitializer(InitializerExpression(
            SyntaxKind.ObjectInitializerExpression,
            SeparatedList<SyntaxNode>(fields.Zip(fieldInitializers, (f, e) => 
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(f),
                    e
                )))));

    private sealed class CompareTypeSyntaxByNane : IEqualityComparer<TypeSyntax> {
        public bool Equals(TypeSyntax x, TypeSyntax y) => x.ToFullString().Equals(y.ToFullString());

        public int GetHashCode(TypeSyntax obj) => obj.ToFullString().GetHashCode();
    }

    private IEnumerable<MemberDeclarationSyntax> mkMapAndScale(TypeDeclarationSyntax typeDecl,
        IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields, bool hasMagnitude) {
        if (fields.Select(f => f.type).Distinct(new CompareTypeSyntaxByNane()).SingleOrDefault() is { } fieldType) {
            yield return mkCopyMethod(
                typeDecl,
                Identifier("Map"),
                ParameterList(Parameter(
                    List(Enumerable.Empty<AttributeListSyntax>()),
                    TokenList(),
                    GenericName(Identifier("Func"), TypeArgumentList(fieldType, fieldType)),
                    Identifier("mapping"),
                    null
                )),
                fields.Select(f => f.identifier),
                fields.Select(f => InvocationExpression(
                    IdentifierName("mapping"),
                    ArgumentList(Argument(IdentifierName(f.identifier)))
                ))
            );
            if(hasMagnitude) {
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

    private IEnumerable<MethodDeclarationSyntax> mkWithFieldMethods(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields) {
        for (var i = 0; i < fields.Count; i++) {
            var (type, identifier) = fields[i];

            static IEnumerable<ExpressionSyntax> asArguments(IEnumerable<(TypeSyntax, SyntaxToken identifier)> args)
                => args.Select(f => IdentifierName(f.identifier));
            MethodDeclarationSyntax mkMethod(ParameterSyntax parameter, ExpressionSyntax constructorArgument) => 
                mkCopyMethod(
                    typeDecl,
                    identifier: Identifier($"With{identifier}"),
                    parameters: ParameterList(parameter),
                    fields.Select(f => f.identifier),
                    asArguments(fields.Take(i))
                        .Append(constructorArgument)
                        .Concat(asArguments(fields.Skip(i + 1))));

            yield return mkMethod(
                Parameter(
                    List(Enumerable.Empty<AttributeListSyntax>()),
                    TokenList(),
                    type,
                    Identifier(identifier.Text.ToLower()),
                    null
                ), 
                IdentifierName(identifier.Text.ToLower()));
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
                    ArgumentList(Argument(IdentifierName(identifier))))
                );
        }
    }

    private static TypeSyntax typeFor(FieldType fieldType) => fieldType switch {
         FieldType.Float => PredefinedType(Token(SyntaxKind.FloatKeyword)),
         FieldType.Double => PredefinedType(Token(SyntaxKind.DoubleKeyword)),
         FieldType.Length => IdentifierName("Length"),
        _ => throw new InvalidEnumArgumentException($"{fieldType} is no valid value for {nameof(fieldType)}")
    };
    
    private static TypeSyntax squareTypeFor(FieldType fieldType) => fieldType switch {
         FieldType.Float => PredefinedType(Token(SyntaxKind.FloatKeyword)),
         FieldType.Double => PredefinedType(Token(SyntaxKind.DoubleKeyword)),
         FieldType.Length => IdentifierName("Area"),
        _ => throw new InvalidEnumArgumentException($"{fieldType} is no valid value for {nameof(fieldType)}")
    };
    
    /// Assumes all fields are `Length`s
    private MemberDeclarationSyntax mkDotProduct(
        TypeDeclarationSyntax typeDecl,
        IEnumerable<SyntaxToken> fields,
        FieldType fieldType
    ) => MethodDeclaration(squareTypeFor(fieldType), Identifier("Dot"))
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithAttributeLists(AttributeList(Pure))
        .WithParameterList(ParameterList(Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier))))
        .WithExpressionBody(ArrowExpressionClause(
            fields
                .Select(f => BinaryExpression(SyntaxKind.MultiplyExpression, IdentifierName(f), MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("other"), IdentifierName(f))))
                .Aggregate((sum, toAdd) => BinaryExpression(SyntaxKind.AddExpression, sum, toAdd))))
        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    /// Assumes all fields are `Length`s
    private MemberDeclarationSyntax mkSqrMagnitude(
        IEnumerable<SyntaxToken> fields,
        FieldType fieldType
    ) => PropertyDeclaration(
        attributeLists: NoAttributes,
        modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
        type: squareTypeFor(fieldType),
        explicitInterfaceSpecifier: null,
        identifier: Identifier("SqrMagnitude"),
        accessorList: null,
        expressionBody: ArrowExpressionClause(
            fields
                .Select(f => BinaryExpression(SyntaxKind.MultiplyExpression, IdentifierName(f), IdentifierName(f)))
                .Aggregate((sum, toAdd) => BinaryExpression(SyntaxKind.AddExpression, sum, toAdd))),
        initializer: null,
        Token(SyntaxKind.SemicolonToken)
    );
    
    /// Assumes `SqrMagnitude` exists
    private MemberDeclarationSyntax mkMagnitude(
        FieldType fieldType
    ) => 
        PropertyDeclaration(
            attributeLists: NoAttributes,
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
            type: typeFor(fieldType),
            explicitInterfaceSpecifier: null,
            identifier: Identifier("Magnitude"),
            accessorList: null,
            expressionBody: ArrowExpressionClause(fieldType switch { 
                FieldType.Float => InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Mathf"), IdentifierName("Sqrt")),
                    ArgumentList(Argument(IdentifierName("SqrMagnitude")))),
                FieldType.Double => InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Math"), IdentifierName("Sqrt")),
                    ArgumentList(Argument(IdentifierName("SqrMagnitude")))),
                FieldType.Length => MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("SqrMagnitude"), IdentifierName("Sqrt")), 
                _ => throw new InvalidEnumArgumentException($"{fieldType} is no valid value for {nameof(fieldType)}")
            }),
            initializer: null,
            Token(SyntaxKind.SemicolonToken)
        );

    /// Assumes all fields have a `.Min` and `.Max` (extension) method
    private IEnumerable<MethodDeclarationSyntax> mkMinMax(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields) {
        MethodDeclarationSyntax make(string operation) => mkCopyMethod(
            typeDecl,
            Identifier(operation),
            ParameterList(Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier))),
            fields.Select(f => f.identifier),
            fields.Select(f => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Units"),
                    IdentifierName(operation)),
                ArgumentList(
                    Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName(f.identifier))),
                    Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("other"),
                    IdentifierName(f.identifier)))))
            )
        );

        yield return make("Min");
        yield return make("Max");
    }

    /// Assumes all fields are either `Length` or have a `.IsNearly(other, double)`
    private MethodDeclarationSyntax mkIsNearly(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields, SemanticModel semanticModel, FieldType fieldType) =>
        MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier("IsNearly"))
            .WithAttributeLists(AttributeList(Pure))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(
                Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
                Parameter(Identifier("threshold"))
                    .WithType(PredefinedType(Token(SyntaxKind.FloatKeyword)))
                    .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1E-6f))))
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
                        Argument(fieldType switch {
                            FieldType.Float or FieldType.Other => IdentifierName("threshold"),
                            FieldType.Double => CastExpression(PredefinedType(Token(SyntaxKind.DoubleKeyword)), IdentifierName("threshold")),
                            FieldType.Length => InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("threshold"),
                                    IdentifierName("Meters"))),
                            _ => throw new InvalidEnumArgumentException(nameof(fieldType), (int)fieldType, typeof(FieldType))
                        }
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

    /// Assumes all fields have a `.LerpTo(other, double)`
    private MethodDeclarationSyntax mkLerp(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields) =>
        MethodDeclaration(IdentifierName(typeDecl.Identifier), Identifier("LerpTo"))
            .WithAttributeLists(AttributeList(Pure))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(
                Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
                Parameter(Identifier("progress")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
                Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))))
            .WithExpressionBody(ArrowExpressionClause(
                makeCopy(
                    fields.Select(f => f.identifier),
                    fields
                        .Select(f => InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(f.identifier),
                                IdentifierName("LerpTo")),
                            ArgumentList(
                                Argument(MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("other"),
                                    IdentifierName(f.identifier))),
                                Argument(IdentifierName("progress")),
                                Argument(IdentifierName("shouldClamp"))))
                ))
            ))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    // /// Assumes all fields are `Length` (<see cref="isBasic"/> == true) or all fields have a `.SlerpTo(other, double)`
    // private MethodDeclarationSyntax mkSlerp(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, SyntaxToken identifier)> fields, bool isBasic) =>
    //     MethodDeclaration(IdentifierName(typeDecl.Identifier), Identifier("SlerpTo"))
    //         .WithAttributeLists(AttributeList(Pure))
    //         .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
    //         .WithParameterList(ParameterList(
    //             Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
    //             Parameter(Identifier("progress")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
    //             Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))))
    //         .WithExpressionBody(isBasic ? null : ArrowExpressionClause(
    //             ImplicitObjectCreationExpression().WithArgumentList(ArgumentList(
    //                 fields
    //                     .Select(f => Argument(InvocationExpression(
    //                         MemberAccessExpression(
    //                             SyntaxKind.SimpleMemberAccessExpression,
    //                             IdentifierName(f.identifier),
    //                             IdentifierName("SlerpTo")),
    //                         ArgumentList(
    //                             Argument(MemberAccessExpression(
    //                                 SyntaxKind.SimpleMemberAccessExpression,
    //                                 IdentifierName("other"),
    //                                 IdentifierName(f.identifier))),
    //                             Argument(IdentifierName("progress")),
    //                             Argument(IdentifierName("shouldClamp")))))
    //                     )
    //             ))))
    //         .WithBody(isBasic ? Body)
    //         .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    
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