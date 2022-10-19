using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Bezier;
using UnityEngine;
using static System.Reflection.BindingFlags;

namespace BII.WasaBii.Core {

    public static class ImmutableValidation {
        
        // Additional types that we allow for convenience although they are technically mutable
        public static readonly IImmutableSet<Type> ExtraAllowedTypes = 
            ImmutableHashSet.Create(
                // Allowed for convenience and because we usually use them as if they were immutable.
                typeof(System.Numerics.Vector3), typeof(System.Numerics.Quaternion),
                typeof(Vector3), typeof(Quaternion), typeof(Color),
                // Same is true for the (normalized) spline location
                typeof(SplineLocation), typeof(NormalizedSplineLocation),
                // Guids are practically immutable, but not implemented in an immutable fashion
                typeof(Guid),
                // Types are effectively immutable
                typeof(Type)
            );

        // Generic instances of these are considered immutable when their generic parameters are immutable
        public static readonly IImmutableSet<Type> ConditionallyImmutableGenerics =
            ImmutableHashSet.Create(
                typeof(Option<>), 
                typeof(Result<,>), 
                // Technically, a lazy can be stateful depending on the factory closure.
                // However, that would be stupid. We cannot validate this, so we just trust you people here.
                typeof(Lazy<>),
                typeof(Spline<,>),
                typeof(BezierSegment<,>)
            );
        
        
        /// <inheritdoc cref="MustBeImmutableAttribute"/>
        /// Adds all validated types to the passed set.
        public static IEnumerable<string> ValidateTrueImmutability(Type toValidate, ISet<Type>? alreadyValidated = null) {
            alreadyValidated ??= new HashSet<Type>();

            return validateTypeIsImmutable(toValidate, new SingleLinkedList<string>());

            IEnumerable<string> validateTypeIsImmutable(Type type, SingleLinkedList<string> contexts) {
                if (type.GetCustomAttribute<__IgnoreMustBeImmutableAttribute>() != null) yield break;

                // Ensure we don't validate twice and don't run into cycles.
                if (alreadyValidated!.Contains(type)) yield break;
                alreadyValidated.Add(type);
                
                string fail(string reason) => $"{string.Join(" / ", contexts.Reverse())} / [{type}]: {reason}";
                
                // Primitive types cannot be mutable
                if (type.IsPrimitive) yield break;

                // Enums are always immutable
                if (type.IsEnum || typeof(Enum).IsAssignableFrom(type)) yield break;

                // We also allow strings. Who mutates string references?
                if (type == typeof(string)) yield break;

                // We also allow certain generics, as long as their type arguments are immutable
                if (type.IsGenericType && ConditionallyImmutableGenerics.Any(
                    genericType => genericType.IsAssignableFrom(type.GetGenericTypeDefinition())
                )) {
                    foreach (var genericArgument in type.GetGenericArguments())
                        foreach (var err in validateTypeIsImmutable(
                             genericArgument,
                             contexts.Prepend($"Generic Arg {genericArgument} of {type}")
                         )) yield return err;
                    yield break;
                }
                
                // We also allow tuples, as long as their contained values are immutable,
                // even though value tuples are technically mutable, for convenience.
                if (typeof(ITuple).IsAssignableFrom(type)) {
                    foreach (var err in type.GetGenericArguments()
                        .SelectMany(t => validateTypeIsImmutable(t, contexts.Prepend("Value of Tuple")))
                    ) yield return err;
                    yield break;
                }

                // Also allow if it is one of the specifically allowed convenience types
                if (ExtraAllowedTypes.Contains(type)) yield break;

                // We allow all immutable collections even though they might internally be mutable
                if (typeof(ICollection).IsAssignableFrom(type)
                    && type.Assembly == typeof(ImmutableList).Assembly
                    && type.Namespace == typeof(ImmutableList).Namespace
                ) {
                    if (!type.IsGenericType) yield return fail("Non-generic immutable collection");
                    else foreach (var genericArgument in type.GetGenericArguments())
                        foreach (var err in validateTypeIsImmutable(
                            genericArgument,
                            contexts.Prepend($"Generic Arg of {type}")
                        )) yield return err;
                    yield break;
                }

                // Validate all fields in the whole hierarchy and their field types
                var currentType = type;
                do {
                    foreach (var field in 
                        currentType.GetFields(Instance | Public | NonPublic | DeclaredOnly).Distinct()
                    ) {
                        if (field.IsLiteral) continue;
                        
                        else if (!field.IsInitOnly)
                            yield return fail($"Field {field.Name} can be mutated after construction.");
                        
                        else if ((field.FieldType.IsInterface || field.FieldType.IsAbstract)
                            && field.FieldType.GetCustomAttribute<MustBeImmutableAttribute>() == null
                            && field.FieldType.GetCustomAttribute<__IgnoreMustBeImmutableAttribute>() == null
                            && !(field.FieldType.Assembly == typeof(ImmutableList).Assembly 
                                && field.FieldType.Namespace == typeof(ImmutableList).Namespace
                                && field.FieldType.IsGenericType) // special support for immutable collections
                            && !(ConditionallyImmutableGenerics.Any(t => 
                                field.FieldType.IsGenericType && t.IsAssignableFrom(field.FieldType.GetGenericTypeDefinition())))
                        ) yield return fail($"Field {field.Name} is of an abstract type ({field.FieldType}) that is not [MustBeImmutable].");
                        
                        else foreach (var err in validateTypeIsImmutable(
                            field.FieldType,
                            contexts.Prepend($"Field {{{field.Name}}} in type [{currentType.Name}]")
                        )) yield return err;
                    }
                    currentType = currentType.BaseType;
                } while (currentType != null && !currentType.IsInterface && currentType != typeof(object));
            }
        }
    }
}