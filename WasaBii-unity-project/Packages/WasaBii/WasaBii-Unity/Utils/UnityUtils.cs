#nullable enable

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary>
    /// This class serves as a namespace for every non-specific unity utility function.
    /// </summary>
    public static class UnityUtils {
        
        /// <summary>
        /// A null check that works for any generic type T. This works
        /// works for unity components as well as every other type.
        /// 
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </summary>
        [JetBrains.Annotations.Pure]
        public static bool IsNull<T>(T value) =>
            // a `where T : class` constraint is not possible, since that would disallow nullables.
            value == null || value is UnityEngine.Object obj && obj == null;

        /// <summary>
        /// A field is not assigned if its value is equal to either its
        /// default value, null, or Unity's definition of equal to null.
        /// 
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter and true is returned.
        /// 
        /// Otherwise the field remains unchanged, the getter
        /// is never called and the operation returns false.
        /// </summary>
        public static bool IfAbsentCompute<T>(ref T? field, Func<T> getter) {
            if (IsNull(field) || Equals(field, default)) {
                field = getter();
                return true;
            }

            return false;
        }

        /// <summary>
        /// A field is not assigned if its value is equal to either its
        /// default value, null, or Unity's definition of equal to null.
        /// 
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter.
        /// 
        /// Regardless if it was or wasn't assigned the field is returned afterwards.
        /// If the getter produces a non-null or non-default value result,
        /// the returned field will always be assigned.
        /// </summary>
        public static T IfAbsentComputeThenReturn<T>(ref T? field, [NotNull] Func<T> getter) {
            IfAbsentCompute(ref field, getter);
            return field!;
        }

        /// <summary>
        /// A nullable field is not assigned if its value is equal to null.
        /// 
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter.
        /// 
        /// Regardless if it was or wasn't assigned the field is returned afterwards.
        /// Since the getter must produce a non-null result,
        /// the returned field will always be non-null.
        /// </summary>
        public static T IfAbsentComputeThenReturn<T>(ref T? field, Func<T> getter) where T : struct {
            IfAbsentCompute(ref field, () => getter());
            if (field == null) throw new ArgumentException($"{nameof(getter)} returned null");
            return field!.Value;
        }

        /// If the field is null, it will be assigned the given value.
        /// Otherwise, the <paramref name="whenNotNull"/> action is executed.
        public static void AssignValueIfNull<T>(ref T? field, T value, Action<T> whenNotNull) where T : class {
            if (IsNull(field))
                field = value;
            else whenNotNull(field!);
        }
    }
}