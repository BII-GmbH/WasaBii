using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using BII.WasaBii.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BII.WasaBii.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustBeImmutableAnalyzer : DiagnosticAnalyzer {
    
    public enum NotImmutableReason {
        NonReadonlyField,
        IsUntypedReference,
        NonImmutableAbstractFieldType,
        ImmutableCollectionWithoutGenerics,
        NotImmutableUnboundGenericParameter,
        UnexpectedNonNamedType, 
    }

    private static string ExplanationFor(NotImmutableReason reason) => reason switch {
        NotImmutableReason.NonReadonlyField => 
            "Fields in reference types must be marked as readonly.",
        NotImmutableReason.IsUntypedReference => 
            "Untyped references like object and dynamic cannot be validated.",
        NotImmutableReason.NonImmutableAbstractFieldType =>
            "All abstract types used as fields need a [MustBeImmutable] attribute.",
        NotImmutableReason.ImmutableCollectionWithoutGenerics => 
            "Untyped immutable collections cannot be validated.",
        NotImmutableReason.NotImmutableUnboundGenericParameter =>
            "Generic parameter must either be unmanaged or constrained to a type with the [MustBeImmutable] attribute.",
        NotImmutableReason.UnexpectedNonNamedType => 
            "Internal analyzer error: Cannot validate unnamed type. Please open a bug issue with an example.",
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

    public const string DiagnosticId = "WasaBiiImmutable";

    private static readonly DiagnosticDescriptor Descriptor = new(
        id: DiagnosticId,
        title: "Type is not immutable",
        messageFormat: "{2} — {1} — [MustBeImmutable] inherited from: {3}",
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
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        
        context.RegisterCompilationStartAction(compilationContext => {
            var allViolations = new ConcurrentDictionary<ITypeSymbol, ImmutableArray<ImmutabilityViolation>>(SymbolEqualityComparer.Default);
            
            // if this contains an object, then lock on it to get the entry in allViolations once acquired
            var validationSync = new ConcurrentDictionary<ITypeSymbol, object>(SymbolEqualityComparer.Default);
            
            var comp = compilationContext.Compilation;
            
            var mustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(MustBeImmutableAttribute).FullName);
            var ignoreMustBeImmutableSymbol = comp.GetTypeByMetadataName(typeof(__IgnoreMustBeImmutableAttribute).FullName);

            if (mustBeImmutableSymbol == null || ignoreMustBeImmutableSymbol == null) 
                return; // attributes not referenced in compilation unit, so nothing to check

            var iEnumerableSymbol = comp.GetTypeByMetadataName(typeof(IEnumerable).FullName);
            var typeSymbol = comp.GetTypeByMetadataName(typeof(Type).FullName)!;
            var lazySymbol = comp.GetTypeByMetadataName(typeof(Lazy<>).FullName)!;
            
            compilationContext.RegisterSymbolAction(ctx => {
                var type = (INamedTypeSymbol) ctx.Symbol;
                var attributeSources = WithAttribute(type, mustBeImmutableSymbol).ToList();
                if (!attributeSources.Any()) return;
                
                if (validationSync.ContainsKey(type) || allViolations.ContainsKey(type)) 
                    return; // either done or in progress
                
                var violations = ValidateWithContextAndRemember(type, lastContext: null, isReferencedAsField: false);
                foreach (var violation in violations) {
                    foreach (var location in violation.Locations) {
                        var violationPath = string.Join(" / ", violation.Context);
                        var violationStr = ExplanationFor(violation.Reason);
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            Descriptor,
                            location,
                            type.ToDisplayString(),
                            violationStr,
                            violationPath,
                            string.Join(", ", attributeSources.Select(t => t.ToDisplayString()))
                        ));
                    }
                }
            }, SymbolKind.NamedType);
            
#region Validation Logic

            // Idea: Mutually recurse between these two to properly add the context step by step and accumulate issues upwards

            IEnumerable<ImmutabilityViolation> ValidateWithContextAndRemember(
                ITypeSymbol type, ContextPathPart? lastContext, bool isReferencedAsField = true
            ) {
                var alreadyAdded = true;
                var lockObject = validationSync.GetOrAdd(
                    type, 
                    valueFactory: _ => { alreadyAdded = false; return new object(); }
                );
                lock (lockObject) {
                    if (allViolations.TryGetValue(type, out var existingViolations))
                        return lastContext == null
                            ? existingViolations
                            : existingViolations.Select(v => v.AddContext(lastContext)).ToImmutableArray();
                    
                    if (alreadyAdded) 
                        // Added on same thread, so we have a circular type reference => return empty for now
                        return Enumerable.Empty<ImmutabilityViolation>();
                    
                    var violations = ValidateType(type, isReferencedAsField).ToImmutableArray();
                    allViolations.TryAdd(type, violations); // locked for type, so this can't be false

                    return lastContext == null
                        ? violations
                        : violations.Select(v => v.AddContext(lastContext));
                }
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

                // We assume that Type references in C# are not used in a mutable fashion
                if (Equal(type, typeSymbol)) yield break;

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
                        if (!typeParam.ConstraintTypes.Any(ct => WithAttribute(ct, mustBeImmutableSymbol).Any()))
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
                
                // In Lazy<T>, we only validate the type that will eventually be held.
                // It is effectively immutable after the lazy initialization.
                if (Equal(namedType.OriginalDefinition, lazySymbol)) {
                    var generic = namedType.TypeArguments.Single();
                    foreach (var violation in ValidateWithContextAndRemember(
                        generic, 
                        new($"Lazy<{generic.Name}>"))
                    ) yield return violation;
                    yield break;
                }

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
                
                // All fields of abstract types must be marked with [MustBeImmutable] so that all subtypes will be validated.
                if (isReferencedAsField && type.IsAbstract && !type.GetAttributes()
                        .Any(a => Equal(a.AttributeClass, mustBeImmutableSymbol)
                               || Equal(a.AttributeClass, ignoreMustBeImmutableSymbol))
                ) yield return ImmutabilityViolation.From(
                    NotImmutableReason.NonImmutableAbstractFieldType, 
                    type.Locations
                );

                // Validate all fields in the whole inheritance hierarchy and their field types
                var currentType = namedType;
                var contextStack = ImmutableStack<ContextPathPart>.Empty;
                do {
                    foreach (var field in currentType.IsTupleType 
                         ? currentType.TupleElements // In case of named tuples, ignore ItemN etc
                         : currentType.GetMembers().OfType<IFieldSymbol>()
                    ) {
                        if (field.IsConst) continue;

                        // ReSharper disable once AccessToModifiedClosure // intentional
                        var locs = new Lazy<ImmutableArray<Location>>(() =>
                            Equal(currentType, namedType) ? field.Locations : namedType.BaseType!.Locations);

                        // All fields in reference types must be marked as readonly.
                        // All fields in top level structs (= marked with the attribute) must be readonly too.
                        // But if we encounter a non-readonly struct field that has been stored in a field,
                        //  then at some point that struct or a containing struct need to be in a readonly field.
                        // This essentially causes all members beneath to be readonly, recursively.
                        // = We allow non-readonly fields only in structs that are in a readonly field
                        if (!field.IsReadOnly && (type.TypeKind != TypeKind.Struct || !isReferencedAsField)) 
                            yield return new ImmutabilityViolation(
                                contextStack.Push(new($"{field.Type.Name} {field.Name}")), 
                                NotImmutableReason.NonReadonlyField,
                                locs.Value
                            );

                        foreach (var violation in ValidateWithContextAndRemember(
                            field.Type, 
                            new($"{field.Type.Name} {field.Name}")
                        )) yield return violation with { Locations = locs.Value };
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
            
    private static IEnumerable<ITypeSymbol> WithAttribute(ITypeSymbol typeSymbol, ISymbol attribute) {
        IEnumerable<ITypeSymbol> AllBaseTypesOfIncluding(ITypeSymbol t) {
            var curr = t;
            do { yield return curr; } while ((curr = curr.BaseType) != null);
        }
        foreach (var b in AllBaseTypesOfIncluding(typeSymbol).Where(b => HasAttributeDirect(b, attribute))) yield return b;
        foreach (var i in typeSymbol.AllInterfaces.Where(i => HasAttributeDirect(i, attribute))) yield return i;
    }
    
#endregion
}