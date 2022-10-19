using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using WasaBii_Geometry_Generator;
using WasaBii.Geometry.Shared;

namespace BII.WasaBii.UnitSystem;

using static SyntaxFactory;
using static SyntaxFactoryUtils;

[Generator]
public sealed class GeometryHelperGenerator : ISourceGenerator {
    
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

        var workspace = new AdhocWorkspace();
        
        try {
            foreach (var (typeDecl, attribute) in ((SyntaxReceiver)context.SyntaxReceiver!).GeometryHelpers) {
                var fieldsDelcs = typeDecl.Members.OfType<FieldDeclarationSyntax>().ToArray();
                var allFields = fieldsDelcs.Select(f => f.Declaration)
                    .SelectMany(decl => decl.Variables
                        .Select(variable => (type: decl.Type, variable))).ToArray();
                var semanticModel = context.Compilation.GetSemanticModel(typeDecl.SyntaxTree);
                var attributeData = semanticModel.GetDeclaredSymbol(typeDecl)!.GetAttributes()
                    .First(a => a.AttributeClass!.Name == nameof(GeometryHelper));
                var attributeArguments = attributeData.GetArgumentValues().ToArray();
                
                var areFieldsIndependent = (bool) attributeArguments.First(a => a.Name == "areFieldsIndependent").Value;
                // var areAllFieldsLength = fieldsDelcs.All(f =>
                //     TypeSymbolFor(f.Declaration.Type, semanticModel) is INamedTypeSymbol {
                //         Name: "Length", ContainingNamespace: var ns
                //     } && ns.ToString() == "BII.WasaBii.UnitSystem");
                var hasMagnitude = (bool) attributeArguments.First(a => a.Name == "hasMagnitude").Value;

                var allMembers = new List<MemberDeclarationSyntax> {mkConstructor(typeDecl, allFields)};
                if (areFieldsIndependent) {
                    allMembers.AddRange(mkMapAndScale(typeDecl, allFields, hasMagnitude));
                    allMembers.AddRange(mkWithFieldMethods(typeDecl, allFields));
                    allMembers.AddRange(mkMinMax(typeDecl, allFields));
                    allMembers.Add(mkLerp(typeDecl, allFields));
                }

                if (hasMagnitude) {
                    allMembers.Add(mkSqrMagnitude(allFields.Select(f => f.variable)));
                    allMembers.Add(mkMagnitude());
                }
                allMembers.Add(mkIsNearly(typeDecl, allFields, semanticModel));
                
                // if fields are independent, make map and with
                // if has magnitude, make scale?, Length?, Min, Max
                // isNearly, Lerp, Slerp
                var result = typeDecl
                    .WithAttributeLists(List(Enumerable.Empty<AttributeListSyntax>()))
                    .WithMembers(List(allMembers))
                    .ClearTrivia();
                
                var root = Formatter.Format(result.WrapInParentsOf(typeDecl), workspace);
                var sourceText = root.GetText(Encoding.UTF8);

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

    private static bool isLength(SyntaxNode type, SemanticModel semanticModel) =>
        TypeSymbolFor(type, semanticModel) is INamedTypeSymbol {
            Name: "Length", ContainingNamespace: var ns
        } && ns.ToString() == "BII.WasaBii.UnitSystem";

    private MemberDeclarationSyntax mkConstructor(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields) {
        return ConstructorDeclaration(
            attributeLists: NoAttributes,
            modifiers: TokenList(Token(SyntaxKind.PrivateKeyword)),
            Identifier(typeDecl.Identifier.Text),
            parameterList: ParameterList(fields.Select(f => Parameter(NoAttributes, TokenList(), f.type, Identifier(f.variable.Identifier.Text.ToLower()), null))),
            initializer: null!,
            body: Block(fields.Select(f => ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, 
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(f.variable.Identifier)), 
                IdentifierName(f.variable.Identifier.Text.ToLower())))))
        );
    }
    
    private MethodDeclarationSyntax mkCopyMethod(TypeDeclarationSyntax typeDecl, SyntaxToken identifier, ParameterListSyntax parameters, ArgumentListSyntax arguments) => 
        MethodDeclaration(
            attributeLists: AttributeList(Pure),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
            returnType: ParseTypeName(typeDecl.Identifier.Text),
            explicitInterfaceSpecifier:null,
            identifier,
            typeParameterList:null,
            parameters,
            constraintClauses: List<TypeParameterConstraintClauseSyntax>(),
            body: null,
            expressionBody: ArrowExpressionClause(ImplicitObjectCreationExpression(arguments, null))
        ).WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private IEnumerable<MethodDeclarationSyntax> mkMapAndScale(TypeDeclarationSyntax typeDecl,
        IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields, bool hasMagnitude) {
        if (fields.Select(f => f.type).Distinct().SingleOrDefault() is { } fieldType) {
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
                ArgumentList(fields.Select(f => Argument(
                    InvocationExpression(
                        IdentifierName("mapping"),
                        ArgumentList(Argument(IdentifierName(f.variable.Identifier)))
                    )
                )))
            );
            if(hasMagnitude) yield return MethodDeclaration(
                attributeLists: AttributeList(Pure),
                modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
                returnType: ParseTypeName(typeDecl.Identifier.Text),
                explicitInterfaceSpecifier:null,
                Identifier("Scale"),
                typeParameterList:null,
                ParameterList(Parameter(NoAttributes, TokenList(), type: PredefinedType(Token(SyntaxKind.DoubleKeyword)), Identifier("factor"), null)),
                constraintClauses: List<TypeParameterConstraintClauseSyntax>(),
                body: null,
                expressionBody: ArrowExpressionClause(InvocationExpression(
                    IdentifierName("Map"),
                    ArgumentList(Argument(
                        SimpleLambdaExpression(Parameter(Identifier("l")))
                            .WithExpressionBody(BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                IdentifierName("factor"),
                                IdentifierName("l")))))))
            ).WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }
    }

    private IEnumerable<MethodDeclarationSyntax> mkWithFieldMethods(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields) {
        for (var i = 0; i < fields.Count; i++) {
            var (type, variable) = fields[i];

            static IEnumerable<ArgumentSyntax> asArguments(IEnumerable<(TypeSyntax, VariableDeclaratorSyntax name)> args)
                => args.Select(f => Argument(IdentifierName(f.name.Identifier)));
            MethodDeclarationSyntax mkMethod(ParameterSyntax parameter, ExpressionSyntax constructorArgument) => 
                mkCopyMethod(
                    typeDecl,
                    identifier: Identifier($"With{variable.Identifier}"),
                    parameters: ParameterList(parameter),
                    arguments: ArgumentList(asArguments(fields.Take(i))
                        .Append(Argument(constructorArgument))
                        .Concat(asArguments(fields.Skip(i + 1))))
                    );

            yield return mkMethod(
                Parameter(
                    List(Enumerable.Empty<AttributeListSyntax>()),
                    TokenList(),
                    type,
                    Identifier(variable.Identifier.Text.ToLower()),
                    null
                ), 
                IdentifierName(variable.Identifier.Text.ToLower()));
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
                    ArgumentList(Argument(IdentifierName(variable.Identifier))))
                );
        }
    }
    
    /// Assumes all fields are `Length`s
    private MemberDeclarationSyntax mkSqrMagnitude(IEnumerable<VariableDeclaratorSyntax> fields) => 
        PropertyDeclaration(
            attributeLists: AttributeList(Pure),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
            type: IdentifierName("Area"),
            explicitInterfaceSpecifier: null,
            identifier: Identifier("SqrMagnitude"),
            accessorList: null,
            expressionBody: ArrowExpressionClause(
                fields
                    .Select(f => BinaryExpression(SyntaxKind.MultiplyExpression, IdentifierName(f.Identifier), IdentifierName(f.Identifier)))
                    .Aggregate((sum, toAdd) => BinaryExpression(SyntaxKind.AddExpression, sum, toAdd))),
            initializer: null,
            Token(SyntaxKind.SemicolonToken)
        );

    /// Assumes `SqrMagnitude` exists
    private MemberDeclarationSyntax mkMagnitude() => 
        PropertyDeclaration(
            attributeLists: AttributeList(Pure),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
            type: IdentifierName("Length"),
            explicitInterfaceSpecifier: null,
            identifier: Identifier("Magnitude"),
            accessorList: null,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Math"), IdentifierName("Sqrt")),
                        ArgumentList(Argument(InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("SqrMagnitude"),
                                IdentifierName("AsSquareMeters")))))),
                    IdentifierName("Meters"))
            )),
            initializer: null,
            Token(SyntaxKind.SemicolonToken)
        );

    /// Assumes all fields have a `.Min` and `.Max` (extension) method
    private IEnumerable<MethodDeclarationSyntax> mkMinMax(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields) {
        MethodDeclarationSyntax make(string operation) => mkCopyMethod(
            typeDecl,
            Identifier(operation),
            ParameterList(Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier))),
            ArgumentList(fields.Select(f => Argument(InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(f.variable.Identifier),
                    IdentifierName(operation)),
                ArgumentList(Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("other"),
                    IdentifierName(f.variable.Identifier)))))
            )))
        );

        yield return make("Min");
        yield return make("Max");
    }

    /// Assumes all fields are either `Length` or have a `.IsNearly(other, double)`
    private MethodDeclarationSyntax mkIsNearly(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields, SemanticModel semanticModel) =>
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
                        IdentifierName(f.variable.Identifier),
                        IdentifierName("IsNearly")),
                    ArgumentList(
                        Argument(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("other"),
                            IdentifierName(f.variable.Identifier)
                        )),
                        Argument(isLength(f.type, semanticModel)
                            ? InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("threshold"),
                                    IdentifierName("Meters")))
                            : IdentifierName("threshold"))
                    )))
                .Aggregate((res, single) => BinaryExpression(
                    SyntaxKind.LogicalAndExpression,
                    res, single
                ))
            ))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    
    /// Assumes all fields have a `.LerpTo(other, double)`
    private MethodDeclarationSyntax mkLerp(TypeDeclarationSyntax typeDecl, IReadOnlyList<(TypeSyntax type, VariableDeclaratorSyntax variable)> fields) =>
        MethodDeclaration(IdentifierName(typeDecl.Identifier), Identifier("LerpTo"))
            .WithAttributeLists(AttributeList(Pure))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(
                Parameter(Identifier("other")).WithType(IdentifierName(typeDecl.Identifier)),
                Parameter(Identifier("progress")).WithType(PredefinedType(Token(SyntaxKind.DoubleKeyword))),
                Parameter(Identifier("shouldClamp")).WithType(PredefinedType(Token(SyntaxKind.BoolKeyword))).WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression)))))
            .WithExpressionBody(ArrowExpressionClause(
                ImplicitObjectCreationExpression().WithArgumentList(ArgumentList(
                    fields
                        .Select(f => Argument(InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(f.variable.Identifier),
                                IdentifierName("LerpTo")),
                            ArgumentList(
                                Argument(MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("other"),
                                    IdentifierName(f.variable.Identifier))),
                                Argument(IdentifierName("progress")),
                                Argument(IdentifierName("shouldClamp")))))
                            )
                ))
            ))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

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