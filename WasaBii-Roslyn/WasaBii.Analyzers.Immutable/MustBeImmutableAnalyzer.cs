#pragma warning disable RS1026 // we don't want weird validation race conditions when one type-to-be-validated references another.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BII.WasaBii.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustBeImmutableAnalyzer : DiagnosticAnalyzer
{
    public enum NotImmutableReason {
        // Field is not readonly and can be overridden at any point, oof
        NonReadonlyField,
        // We do not allow `object` and `dynamic` etc
        IsUntypedReference,
        // If a field is of an abstract type, then that type needs to be marked for validation via attribute
        NonImmutableAbstractFieldType,
        // This indicates an edge case that has not been handled in the code
        UnexpectedNonNamedType, 
        // We only allow immutable collections where we can validate the contained type
        ImmutableCollectionWithoutGenerics,
        // For unbounded generics, we cannot ensure immutability without knowing all usages
        HasUnboundGenericParameter
    }

    public sealed record ContextPathPart(string Desc) {
        public override string ToString() => Desc;
    }

    public sealed record ImmutablityViolation(
        ImmutableStack<ContextPathPart> Context,
        NotImmutableReason Reason,
        ImmutableArray<Location> Locations
    ) {
        public static ImmutablityViolation From(
            NotImmutableReason reason, ImmutableArray<Location> locations
        ) => new(ImmutableStack<ContextPathPart>.Empty, reason, locations);

        public static ImmutablityViolation From(
            ContextPathPart lastContext, NotImmutableReason reason, ImmutableArray<Location> locations
        ) => new(ImmutableStack<ContextPathPart>.Empty.Push(lastContext), reason, locations);

        public ImmutablityViolation AddContext(ContextPathPart contextPathPart) =>
            this with {Context = this.Context.Push(contextPathPart)};
    };

    public const string DiagnosticId = "WasaBiiImmutability";

    private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
        id: DiagnosticId, // TODO CR: do we want release tracking?
        title: "Type is not immutable",
        messageFormat: "This causes '{0}' to not be immutable: {1}",
        category: "WasaBii",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Validate types marked with [MustBeImmutable]"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
    
    // Note CR: when iterating, roslyn automatically substitutes generic types.
    //  => We could have violation entries for `Option<MutableA>` as well as `Option<MutableB>`.
    //  => We validate the fields' actual substituted types, even when declared as a generic type.

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        var allViolations = new Dictionary<ITypeSymbol, IReadOnlyCollection<ImmutablityViolation>>(SymbolEqualityComparer.Default); 
        var topLevelToValidate = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default); 
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // Step 1: collect all violations
        
        context.RegisterCompilationStartAction(compilationContext => {

            var comp = compilationContext.Compilation;

            bool IsAssignableTo(ITypeSymbol baseType, ITypeSymbol subType) {
                var conversion = comp.ClassifyConversion(subType, baseType);
                // User-defined implicit conversions do not guarantee an immutable source
                return conversion is {IsImplicit: true, IsUserDefined: false}; 
            }
            
            var mustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(MustBeImmutableAttribute).FullName);
            var ignoreMustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(__IgnoreMustBeImmutableAttribute).FullName);

            if (mustBeImmutableSymbol == null || ignoreMustBeImmutableSymbol == null) 
                return; // attributes not referenced in compilation unit, so nothing to check
            
            var iEnumerableSymbol = comp.GetTypeByMetadataName(typeof(IEnumerable).FullName);

            var allowedTypes = new[] {
                // Technically, a lazy can be stateful depending on the factory closure.
                // However, that would be stupid. We cannot validate this, so we just trust you people here.
                "System.Lazy",
                // Allowed for convenience and because we usually use them as if they were immutable.
                "System.Numerics.Vector3",
                "System.Numerics.Quaternion",
                // Guids are practically immutable, but not implemented in an immutable fashion
                "System.Guid",
                // Types are effectively immutable
                "System.Type"
            }.Select(t => comp.GetTypeByMetadataName(t)!).ToImmutableHashSet(SymbolEqualityComparer.Default);

            bool Equal(ISymbol? a, ISymbol? b) => SymbolEqualityComparer.Default.Equals(a, b);

            compilationContext.RegisterSyntaxNodeAction(ctx => {
                var model = ctx.SemanticModel;
                if (ctx.Node is TypeDeclarationSyntax tds) {
                    var typeInfo = model.GetDeclaredSymbol(tds)!;

                    bool HasAttribute(INamedTypeSymbol t) =>
                        t.GetAttributes().Any(a => Equal(a.AttributeClass, mustBeImmutableSymbol));

                    IEnumerable<INamedTypeSymbol> AllBaseTypesOf(INamedTypeSymbol t) {
                        var curr = t;
                        do { yield return curr; } while ((curr = curr.BaseType) != null);
                    }

                    if (HasAttribute(typeInfo) 
                        || typeInfo.AllInterfaces.Any(HasAttribute) 
                        || AllBaseTypesOf(typeInfo).Any(HasAttribute)
                    ) {
                        topLevelToValidate.Add(typeInfo);
                        if (!seen.Contains(typeInfo))
                            ValidateWithContextAndRemember(typeInfo, lastContext: null, allowAbstract: true);
                    }
                }

                // Idea: Mutually recurse between these two to properly add the context step by step and accumulate issues upwards

                IEnumerable<ImmutablityViolation> ValidateWithContextAndRemember(
                    ITypeSymbol type, ContextPathPart? lastContext, bool allowAbstract = false
                ) {
                    if (allViolations.TryGetValue(type, out var existingViolations))
                        return existingViolations;
                    var violations = ValidateType(type, allowAbstract);
                    var wrapped = lastContext == null
                        ? violations.ToList()
                        : violations.Select(v => v.AddContext(lastContext)).ToList();
                    allViolations.Add(type, wrapped);
                    return wrapped;
                }
                
                IEnumerable<ImmutablityViolation> ValidateType(ITypeSymbol type, bool allowAbstract) {
                    if (seen.Contains(type)) yield break; // circular reference, otherwise would have a violation entry
                    seen.Add(type);

                    // Note CR: attribute is not inherited, so must be placed on every class here
                    if (type.GetAttributes().Any(a => Equal(a.AttributeClass, ignoreMustBeImmutableSymbol))) 
                        yield break;

                    // Explicitly forbid dynamic types and object references: They could be anything! 
                    if (Equal(type, comp.GetSpecialType(SpecialType.System_Object)) || type is IDynamicTypeSymbol) {
                        yield return ImmutablityViolation.From(NotImmutableReason.IsUntypedReference, type.Locations);
                        yield break;
                    }

                    // Numbers and readonly unmanaged structs fulfill this constraint.
                    // Non-unmanaged readonly structs however may contain references
                    //  to mutable objects and need to be validated separately.
                    if (type.IsReadOnly && type.IsUnmanagedType) yield break;

                    // We need a named symbol for generic shenanigans etc; all exceptions should be checked before this
                    if (type is not INamedTypeSymbol namedType) {
                        yield return ImmutablityViolation.From(NotImmutableReason.UnexpectedNonNamedType, type.Locations);
                        yield break;
                    }

                    // Also allow if it is one of the specifically allowed convenience types
                    if (allowedTypes.Contains(namedType)) yield break;
                    
                    // TODO CR PREMERGE: externally defined allowed types go here

                    // // Allowed for convenience and because we usually use them as if they were immutable.
                    // typeof(Vector3), typeof(Quaternion), typeof(Color),
                    // // Same is true for the (normalized) spline location
                    // typeof(SplineLocation), typeof(NormalizedSplineLocation),
                    // // Guids are practically immutable, but not implemented in an immutable fashion
                    // typeof(Guid),
                    // // Types are effectively immutable
                    // typeof(Type)

                    // Enums are always immutable; detectable by whether they have an underlying type.
                    if (namedType.EnumUnderlyingType != null) yield break;

                    // We also allow strings. Who mutates string references?
                    if (IsAssignableTo(comp.GetSpecialType(SpecialType.System_String), type)) yield break;
                    
                    // We also allow tuples, as long as their contained values are immutable,
                    //  even though value tuples are technically mutable, for convenience.
                    if (type.IsTupleType) {
                        var tup = namedType.TupleUnderlyingType ?? namedType; // resolve named tuples
                        var violations = tup.TypeArguments
                            .SelectMany(targ => ValidateWithContextAndRemember(targ, new($"tuple member of type `{targ.Name}`")));
                        foreach (var violation in violations) yield return violation;
                        yield break;
                    }

                    // Ensure that all generics are bound to concrete values;
                    //  otherwise they could be substituted for mutable values
                    foreach (var typeArg in namedType.TypeArguments) { // empty if not generic
                        if (typeArg.TypeKind is TypeKind.TypeParameter or TypeKind.Unknown)
                            yield return ImmutablityViolation.From(
                                new($"generic param `{typeArg.Name}`"), 
                                NotImmutableReason.HasUnboundGenericParameter,
                                typeArg.Locations
                            );
                        // Otherwise type is bound and we can check the fields recursively
                    }

                    // We allow all immutable collections even though they might internally be mutable
                    if (
                        iEnumerableSymbol != null
                        && type.ContainingAssembly.Name == typeof(ImmutableArray).Namespace.Split(",").First() 
                        && type.AllInterfaces.Contains(iEnumerableSymbol)
                    ) {
                        if (!namedType.IsGenericType)
                            yield return ImmutablityViolation.From(NotImmutableReason.ImmutableCollectionWithoutGenerics, namedType.Locations);
                        else
                            foreach (var generic in namedType.TypeArguments) 
                            foreach (var violation in ValidateWithContextAndRemember(
                                generic, 
                                new($"element of type `{generic.Name}`"))
                            ) yield return violation;
                        yield break;
                    }
                    
                    // Unless we allow them explicitly, all abstract types must be marked with an attribute
                    //  so that all subtypes will be either validated or ignored.
                    if (!allowAbstract && type.IsAbstract && !type.GetAttributes()
                            // .Any(a => Equal(a.AttributeClass, mustBeImmutableSymbol) ||
                            //           Equal(a.AttributeClass, ignoreMustBeImmutableSymbol))
                            .Any(a => a.AttributeClass?.Name == "MustBeImmutableAttribute" ||
                                      a.AttributeClass?.Name == "__IgnoreMustBeImmutableAttribute")
                    ) yield return ImmutablityViolation.From(NotImmutableReason.NonImmutableAbstractFieldType, type.Locations);

                    // Validate all fields in the whole inheritance hierarchy and their field types
                    var currentType = namedType;
                    var contextStack = ImmutableStack<ContextPathPart>.Empty;
                    do {
                        foreach (var field in currentType.GetMembers().OfType<IFieldSymbol>()) {
                            if (field.IsConst) continue;

                            if (!field.IsReadOnly)
                                yield return new ImmutablityViolation(
                                    contextStack.Push(new($"field `{field.Name}: {field.Type.Name}`")), 
                                    NotImmutableReason.NonReadonlyField,
                                    field.Locations
                                );

                            foreach (var violation in ValidateWithContextAndRemember(
                                 field.Type, 
                                 new($"field `{field.Name}: {field.Type.Name}`")
                            )) yield return violation with {
                                Locations = Equal(currentType, namedType) 
                                    ? field.Locations 
                                    : namedType.BaseType!.Locations
                            };
                        }
                        
                        currentType = currentType.BaseType;
                        contextStack = contextStack.Push(new($"base type `{currentType}`"));
                    } while (currentType != null && !Equal(currentType, comp.GetSpecialType(SpecialType.System_Object)));
                }

            }, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration);
            
            // Step 2: report all collected violations
            compilationContext.RegisterCompilationEndAction(ctx => {
                foreach (var type in topLevelToValidate)
                foreach (var violation in allViolations[type]) 
                foreach (var location in violation.Locations) {
                    var violationStr = $"{violation.Reason} ({string.Join(" / ", violation.Context)})";
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptor, 
                        location, 
                        $"{type.ContainingNamespace}.{type.Name}", 
                        violationStr
                    ));
                }
            });
        });
    }
}