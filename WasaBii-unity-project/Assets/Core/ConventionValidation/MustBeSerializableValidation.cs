using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using static System.Reflection.BindingFlags;

namespace BII.WasaBii.Core {
    
    public static class MustBeSerializableValidation {

        public static readonly IReadOnlyList<Type> ExtraAllowedTypes = new Type[] {
            // Contains all types that we explicitly support for serialization and are not from us.
            // Custom types should use the [MustBeSerialized] attribute instead.
            
            // NOTE CR: These types are not checked at all.
            //          This means that collection contents are not checked.
            //          Implementing this properly would be really hard, so we just live with worse hints for now.

            // System
            typeof(System.Decimal),
            typeof(System.String),
            typeof(System.Guid),
            typeof(System.Type),
            typeof(System.DateTime),
            typeof(System.Numerics.Vector3), typeof(System.Numerics.Quaternion),
            // Unity Engine
            typeof(UnityEngine.Vector3),
            typeof(UnityEngine.Quaternion),
            typeof(UnityEngine.Color),
            typeof(UnityEngine.Matrix4x4),
            // Unity Editor
        #if UNITY_EDITOR
            typeof(UnityEditor.BuildOptions),
        #endif
        
            // TODO DS for CR: Is there a way to check if it is auto generated?
            // People might go and manually implement non-serializable units :shrug:
            typeof(IUnitValue)
        };

        /// <summary>
        /// Ensures that all referenced types in fields of the passed type are [MustBeSerializable].
        /// This entails that all types are either [NonSerialized], primitive,
        ///   in <see cref="ExtraAllowedTypes"/> or have that attribute.
        /// Also emits an error when a private field in a non-sealed class is neither [NonSerialized] nor [SerializeInSubclasses]
        /// </summary>
        /// <remarks>
        /// The passed type itself is assumed to have a [MustBeSerializable] attribute,
        ///   so that we can unit test this with non-serializable types itself.
        /// Adds all validated types to the passed set.
        /// </remarks>
        public static IEnumerable<string> ValidateMustBeSerializable(Type toValidate, ISet<Type>? alreadyValidated = null) {
            alreadyValidated ??= new HashSet<Type>();

            return validateSerializableRecursively(toValidate, new SingleLinkedList<string>());

            IEnumerable<string> validateSerializableRecursively(Type type, SingleLinkedList<string> contexts) {
                if (toValidate.GetCustomAttribute<__IgnoreMustBeSerializableAttribute>() != null) yield break;
                
                // Ensure we don't validate twice and don't run into cycles.
                if (alreadyValidated!.Contains(type)) yield break;
                alreadyValidated.Add(type);

                string fail(string reason) => $"{string.Join(" / ", contexts.Reverse())} / [{type}]: {reason}";
                
                // Primitive types can always be serialized
                if (type.IsPrimitive) yield break;

                // Also allow if it is one of the specifically allowed convenience types
                if (ExtraAllowedTypes.Any(t => t.IsAssignableFrom(type))) yield break;

                // If it's a collection type, validate all contents
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) 
                    || typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(type)
                ) {
                    var genericArgs = type.GetGenericArguments();
                    if (genericArgs.IsEmpty()) fail("Non-generic IEnumerable or ITuple not supported");
                    
                    // assume that all generic types are contained as serialized fields in a collection type
                    foreach (var recRes in genericArgs
                        .Where(t => t != typeof(System.Object)) // we sometimes have untyped dictionaries and lists
                        .Where(t => !t.IsGenericParameter) // obviously can't validate when parameter is not specific
                        .SelectMany(t => validateSerializableRecursively(t,
                            contexts.Prepend($"Contents of collection/tuple of type [{type.Name}]"))
                    )) yield return recRes;
                    yield break;
                }
                
                // Nullables can be serialized, but we still validate the type
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    foreach (var recres in validateSerializableRecursively(
                        type.GetGenericArguments()[0],
                        contexts.Prepend("Value of Nullable")
                    )) yield return recres;
                    yield break;
                }

                if (type.GetCustomAttribute<MustBeSerializableAttribute>() == null 
                    && !typeof(ITuple).IsAssignableFrom(type) // we check tuple contents explicitly
                ) yield return fail("Does not have a [MustBeSerializable] attribute.");

                // Validate all fields in the whole hierarchy and their field types
                var currentType = type;
                do {
                    foreach (var field in currentType.GetFields(Instance | Public | NonPublic).Distinct()) {
                        var isNonSerialized = field.GetCustomAttribute<NonSerializedAttribute>() != null;

                        //TODO CR: remove this or limit it if it gets too obnoxious, but it might remove some annoying errors
                        if (type.Namespace != null &&
                            type.Namespace.StartsWith("BII") && // Note DS: Hello reviewer, we need to change this, right?
                            !type.IsSealed &&
                            type.IsClass &&
                            !isNonSerialized && 
                            field.IsPrivate &&
                            field.GetCustomAttribute<SerializeInSubclassesAttribute>() == null
                        ) yield return fail(
                            $"Field {{{field.Name}}} in non-sealed class is neither [NonSerialized] nor [SerializedInSubclasses]."
                        );
                        
                        if (!isNonSerialized 
                            && (!type.IsAbstract || field.GetCustomAttribute<SerializeInSubclassesAttribute>() != null)
                            // Note CR: validating this for all instantiations of generic types is theoretically possible (excluding reflection), but hard, so we don't.
                            && !field.FieldType.IsGenericParameter 
                        ) {
                            foreach (var err in validateSerializableRecursively(
                                field.FieldType,
                                contexts.Prepend($"Field {{{field.Name}}} in type [{currentType.Name}]")
                            )) yield return err;
                        }
                    }
                    currentType = currentType.BaseType;
                } while (currentType != null && !currentType.IsInterface && currentType != typeof(object));
            }
        }
    }
}