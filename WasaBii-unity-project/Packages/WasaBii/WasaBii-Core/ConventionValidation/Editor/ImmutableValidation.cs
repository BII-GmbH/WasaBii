using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BII.WasaBii.Core.Editor {

    public static class CompileTimeImmutableValidation {
        
        [DidReloadScripts]
        [MenuItem("WasaBii/Validation/Immutability")]
        public static void ValidateTrueImmutableTypes() {
            var errors = ValidateAllTypes();

            foreach (var error in errors) 
                Debug.LogError($"[{error.type}] is not immutable:\n{string.Join("\n", error.messages.Select(m => "  - " + m))}");
            
            if (errors.Count > 0) throw new Exception(
                $"{errors.Count} types marked with [{nameof(MustBeImmutableAttribute)}] were not immutable.");
            else 
                Debug.Log($"Immutability validation done. 0 issues found.");
        }

        public static List<(Type type, List<string> messages)> ValidateAllTypes() {
            var errors = new List<(Type type, List<string> messages)>();

            var typesToValidate = TypeCache.GetTypesWithAttribute<MustBeImmutableAttribute>()
                .SelectMany(
                    trueImmutableType => trueImmutableType.IsAbstract || trueImmutableType.IsInterface 
                        ? TypeCache.GetTypesDerivedFrom(trueImmutableType) 
                        : (IEnumerable<Type>) new[] {trueImmutableType}
                ).Where(type => 
                    // Ignore all namespaces with a `Tests` segment by convention
                    !(type.Namespace != null && new Regex(@"^.*?\.Tests(\..*?$|$)").IsMatch(type.Namespace))
                );
            
            var validated = new HashSet<Type>();
                
            foreach (var toValidate in typesToValidate) {
                var validationErrors = ImmutableValidation.ValidateTrueImmutability(toValidate, validated).ToList();
                if (validationErrors.IsNotEmpty()) errors.Add((toValidate, validationErrors));
            }

            return errors;
        }
    }
}