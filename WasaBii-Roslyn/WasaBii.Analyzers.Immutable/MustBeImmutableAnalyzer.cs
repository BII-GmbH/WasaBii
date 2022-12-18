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
public class MustBeImmutableAnalyzer : DiagnosticAnalyzer {
    
    // Types with exactly these full names will be assumed to be immutable for the sake of validation here.
    private static readonly ImmutableArray<string> AllowedTypeIdentifiers = ImmutableArray.Create(
        // Technically, a lazy can be stateful depending on the factory closure.
        // However, that would be stupid. We cannot validate this, so we just trust you people here.
        "System.Lazy",
        // Types are effectively immutable.
        "System.Type"
    );
    
    public enum NotImmutableReason {
        NonReadonlyField,
        IsUntypedReference,
        NonImmutableAbstractFieldType,
        ImmutableCollectionWithoutGenerics,
        NotImmutableUnboundGenericParameter,
        UnexpectedNonNamedType, 
    }

    public static string ExplanationFor(NotImmutableReason reason) => reason switch {
        NotImmutableReason.NonReadonlyField => 
            "Fields in reference types must be marked as `readonly`.",
        NotImmutableReason.IsUntypedReference => 
            "Untyped references like `object` and `dynamic` cannot be validated.",
        NotImmutableReason.NonImmutableAbstractFieldType =>
            "All abstract types used as fields need a [MustBeImmutable] attribute.",
        NotImmutableReason.ImmutableCollectionWithoutGenerics => 
            "Untyped immutable collections cannot be validated.",
        NotImmutableReason.NotImmutableUnboundGenericParameter =>
            "Generic parameter must either be `unmanaged` or constrained to a type with the [MustBeImmutable] attribute.",
        NotImmutableReason.UnexpectedNonNamedType => 
            "Unexpected type: not sure what to do here.",
        var other => other.ToString() // fallback for when there's no message yet
    };

    public sealed record ContextPathPart(string Desc) {
        public override string ToString() => Desc;
    }

    public sealed record ImmutabilityViolation(
        ImmutableStack<ContextPathPart> Context,
        NotImmutableReason Reason,
        ImmutableArray<Location> Locations
    ) {
        public static ImmutabilityViolation From(
            NotImmutableReason reason, ImmutableArray<Location> locations
        ) => new(ImmutableStack<ContextPathPart>.Empty, reason, locations);

        public static ImmutabilityViolation From(
            ContextPathPart lastContext, NotImmutableReason reason, ImmutableArray<Location> locations
        ) => new(ImmutableStack<ContextPathPart>.Empty.Push(lastContext), reason, locations);

        public ImmutabilityViolation AddContext(ContextPathPart contextPathPart) =>
            this with {Context = this.Context.Push(contextPathPart)};
    };

    public const string DiagnosticId = "WasaBiiImmutability";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: DiagnosticId, // TODO CR: do we want release tracking?
        title: "Type is not immutable",
        messageFormat: "'{0}' not immutable: {1}",
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

        var allViolations = new Dictionary<ITypeSymbol, IReadOnlyCollection<ImmutabilityViolation>>(SymbolEqualityComparer.Default); 
        var topLevelToValidate = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        context.RegisterCompilationStartAction(compilationContext => {
            var comp = compilationContext.Compilation;
            
            var mustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(MustBeImmutableAttribute).FullName);
            var ignoreMustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(__IgnoreMustBeImmutableAttribute).FullName);

            if (mustBeImmutableSymbol == null || ignoreMustBeImmutableSymbol == null) 
                return; // attributes not referenced in compilation unit, so nothing to check

            var iEnumerableSymbol = comp.GetTypeByMetadataName(typeof(IEnumerable).FullName);

            var allowedTypes = AllowedTypeIdentifiers
                .Select(t => comp.GetTypeByMetadataName(t)!)
                .ToImmutableHashSet(SymbolEqualityComparer.Default);
            
            var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            // Step 1: collect all violations
            compilationContext.RegisterSyntaxNodeAction(
                ctx => {
                    var model = ctx.SemanticModel;
                    if (ctx.Node is TypeDeclarationSyntax tds) {
                        var typeInfo = model.GetDeclaredSymbol(tds)!;
                        if (HasAttribute(typeInfo, mustBeImmutableSymbol)) {
                            topLevelToValidate.Add(typeInfo);
                            if (!seen.Contains(typeInfo))
                                ValidateWithContextAndRemember(typeInfo, lastContext: null, isReferencedAsField: false);
                        }
                    }
                }, 
                SyntaxKind.ClassDeclaration, 
                SyntaxKind.StructDeclaration, 
                SyntaxKind.InterfaceDeclaration, 
                SyntaxKind.RecordDeclaration
            );
            
            // Step 2: report all collected violations
            compilationContext.RegisterCompilationEndAction(ctx => {
                foreach (var type in topLevelToValidate)
                foreach (var violation in allViolations[type]) 
                foreach (var location in violation.Locations) {
                    var violationStr = $"{string.Join(" / ", violation.Context)} -- {ExplanationFor(violation.Reason)}";
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptor, 
                        location, 
                        $"{type.ContainingNamespace}.{type.Name}", 
                        violationStr
                    ));
                }
            });
            
#region Validation Logic
            
            // Idea: Mutually recurse between these two to properly add the context step by step and accumulate issues upwards

            IEnumerable<ImmutabilityViolation> ValidateWithContextAndRemember(
                ITypeSymbol type, ContextPathPart? lastContext, bool isReferencedAsField = true
            ) {
                if (allViolations.TryGetValue(type, out var existingViolations))
                    return existingViolations;
                
                if (!seen.Add(type)) // circular reference, otherwise would have a violation entry
                    return Enumerable.Empty<ImmutabilityViolation>();
                
                var violations = ValidateType(type, isReferencedAsField);
                var wrapped = lastContext == null
                    ? violations.ToList()
                    : violations.Select(v => v.AddContext(lastContext)).ToList();
                allViolations.Add(type, wrapped);
                return wrapped;
            }
            
            IEnumerable<ImmutabilityViolation> ValidateType(ITypeSymbol type, bool isReferencedAsField) {
                // Note CR: attribute is not inherited, so must be placed on every class individually
                if (type.GetAttributes().Any(a => Equal(a.AttributeClass, ignoreMustBeImmutableSymbol))) 
                    yield break;

                // Explicitly forbid dynamic types and object references: They could be anything! 
                if (Equal(type, comp.GetSpecialType(SpecialType.System_Object)) || type is IDynamicTypeSymbol) {
                    yield return ImmutabilityViolation.From(NotImmutableReason.IsUntypedReference, type.Locations);
                    yield break;
                }

                // Numbers and readonly unmanaged structs fulfill this constraint.
                // Non-unmanaged readonly structs however may contain references
                //  to mutable objects and need to be validated separately.
                // Unmanaged mutable structs need to be referenced via a readonly field at some point.
                if ((type.IsReadOnly || isReferencedAsField) && type.IsUnmanagedType) yield break;
                
                // We also allow strings. Who mutates string references?
                if (comp.ClassifyConversion(comp.GetSpecialType(SpecialType.System_String), type) 
                    is {IsImplicit: true, IsUserDefined: false} // User-defined implicit conversions do not guarantee an immutable source
                ) yield break; 
                
                // If any field either is or references a generic type,
                //  then we need to check that that type is immutable via constraints.

                if (type is ITypeParameterSymbol typeParam) {
                    if (HasAttributeDirect(typeParam, ignoreMustBeImmutableSymbol)) {
                        // No violation, just ignore
                    } else {
                        // We need to ensure that any type constraint has the attribute
                        if (!typeParam.ConstraintTypes.Any(ct => HasAttribute(ct, mustBeImmutableSymbol)))
                            yield return ImmutabilityViolation.From(
                                new($"<{typeParam.Name}>"), 
                                NotImmutableReason.NotImmutableUnboundGenericParameter,
                                typeParam.Locations
                            );
                    }
                    // Otherwise type is bound and we can check the fields recursively

                    yield break; // Is generic, so we must rely on validation of constraint types
                }

                if (type is not INamedTypeSymbol namedType) {
                    // We need a named symbol for generic shenanigans etc; all exceptions should be checked before this
                    yield return ImmutabilityViolation.From(NotImmutableReason.UnexpectedNonNamedType, type.Locations);
                    yield break;
                }

                // Also allow if it is one of the specifically allowed convenience types
                if (allowedTypes.Contains(namedType)) yield break;

                // We allow all immutable collections even though they might internally be mutable
                if (
                    iEnumerableSymbol != null
                    && type.ContainingAssembly.Name == typeof(ImmutableArray).Namespace.Split(",").First() 
                    && type.AllInterfaces.Contains(iEnumerableSymbol)
                ) {
                    if (!namedType.IsGenericType)
                        yield return ImmutabilityViolation.From(NotImmutableReason.ImmutableCollectionWithoutGenerics, namedType.Locations);
                    else
                        foreach (var generic in namedType.TypeArguments) 
                        foreach (var violation in ValidateWithContextAndRemember(
                            generic, 
                            new($"<{generic.Name}>"))
                        ) yield return violation;
                    yield break;
                }
                
                // All abstract types must be marked with an attribute so that all subtypes will be validated.
                if (type.IsAbstract && !type.GetAttributes()
                        .Any(a => Equal(a.AttributeClass, mustBeImmutableSymbol) ||
                                  Equal(a.AttributeClass, ignoreMustBeImmutableSymbol))
                ) yield return ImmutabilityViolation.From(NotImmutableReason.NonImmutableAbstractFieldType, type.Locations);

                // Validate all fields in the whole inheritance hierarchy and their field types
                var currentType = namedType;
                var contextStack = ImmutableStack<ContextPathPart>.Empty;
                do {
                    foreach (var field in currentType.GetMembers().OfType<IFieldSymbol>()) {
                        if (field.IsConst) continue;

                        // All fields in reference types must be marked as readonly.
                        // All fields in top level structs (= marked with the attribute) must be readonly too.
                        // But if we encounter a non-readonly struct field that has been stored in a field,
                        //  then at some point that struct or a containing struct need to be in a readonly field.
                        // This essentially causes all members beneath to be readonly, recursively.
                        // = We allow non-readonly fields only in structs that are in a readonly field
                        if (!field.IsReadOnly && (type.TypeKind != TypeKind.Struct || !isReferencedAsField))
                            yield return new ImmutabilityViolation(
                                contextStack.Push(new($"{field.Name}: {field.Type.Name}")), 
                                NotImmutableReason.NonReadonlyField,
                                field.Locations
                            );

                        foreach (var violation in ValidateWithContextAndRemember(
                             field.Type, 
                             new($"{field.Name}: {field.Type.Name}")
                        )) yield return violation with {
                            Locations = Equal(currentType, namedType) 
                                ? field.Locations 
                                : namedType.BaseType!.Locations
                        };
                    }
                    
                    currentType = currentType.BaseType;
                    contextStack = contextStack.Push(new($"base {currentType}"));
                } while (currentType != null && !Equal(currentType, comp.GetSpecialType(SpecialType.System_Object)));
            }
            
#endregion
            
        });
    }
    
#region Pure Utils
    
    private static bool Equal(ISymbol? a, ISymbol? b) => SymbolEqualityComparer.Default.Equals(a, b);
    
    private static bool HasAttributeDirect(ISymbol t, ISymbol attribute) =>
        t.GetAttributes().Any(a => Equal(a.AttributeClass, attribute));
            
    private static bool HasAttribute(ITypeSymbol typeSymbol, ISymbol attribute) {
        IEnumerable<ITypeSymbol> AllBaseTypesOf(ITypeSymbol t) {
            var curr = t;
            do { yield return curr; } while ((curr = curr.BaseType) != null);
        }
        return HasAttributeDirect(typeSymbol, attribute) 
               || typeSymbol.AllInterfaces.Any(i => HasAttributeDirect(i, attribute)) 
               || AllBaseTypesOf(typeSymbol).Any(b => HasAttributeDirect(b, attribute));
    }
    
#endregion
}