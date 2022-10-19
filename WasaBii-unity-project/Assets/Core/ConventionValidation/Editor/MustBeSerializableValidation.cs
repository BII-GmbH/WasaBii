using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BII.WasaBii.Core.Editor {

    public static class CompileTimeMustBeSerializableValidation {
        
        [DidReloadScripts]
        [MenuItem("WasaBii/Validation/MustBeSerializable")]
        public static void ValidateMustBeSerializableTypes() {
            var errors = ValidateAllTypes();

            foreach (var error in errors)
                if (error.annotatedType == error.derivedType) {
                    Debug.LogError($"[{error.derivedType}] is not MustBeSerializable:\n{string.Join("\n", error.messages.Select(m => "  - " + m))}");
                } else {
                    Debug.LogError($"[{error.derivedType}] derived from [{error.annotatedType}] is not MustBeSerializable:\n{string.Join("\n", error.messages.Select(m => "  - " + m))}");
                }
            
            if (errors.Count > 0) {
                throw new Exception(
                    $"{errors.Count} types marked with [MustBeSerializable] had non-serializable fields.");
            } else 
                Debug.Log($"MustBeSerializable validation done. 0 issues found.");
        }

        public static List<(Type annotatedType, Type derivedType, List<string> messages)> ValidateAllTypes() {
            var errors = new List<(Type annotatedType, Type derivedType, List<string> messages)>();

            IEnumerable<(Type, Type)> typesToValidate = TypeCache.GetTypesWithAttribute<MustBeSerializableAttribute>()
                .SelectMany(annotatedType =>
                    TypeCache.GetTypesDerivedFrom(annotatedType).Prepend(annotatedType).Select(derivedType => (mustBeSerializableType: annotatedType, actualType: derivedType))
                ).Where(types =>
                    // Ignore all namespaces with a `Tests` segment by convention
                    !(types.actualType.Namespace != null && new Regex(@"^.*?\.Tests(\..*?$|$)").IsMatch(types.actualType.Namespace))
                );

            var validated = new HashSet<Type>();

            foreach (var (annotatedType, toValidate) in typesToValidate) {
                var validationErrors = MustBeSerializableValidation.ValidateMustBeSerializable(toValidate, validated).ToList();
                if (validationErrors.IsNotEmpty()) errors.Add((annotatedType, toValidate, validationErrors));
            }

            return errors;
        }
    }
}