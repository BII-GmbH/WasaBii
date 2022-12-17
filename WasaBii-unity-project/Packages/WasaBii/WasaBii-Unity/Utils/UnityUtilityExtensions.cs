#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    public static class UnityUtilityExtensions {
        
#region General Extensions

        /// <summary>
        /// An IEnumerable containing all the children of this Transform in order.
        /// It is safe to modify the transforms children during iteration.
        /// <b>Iterable only once</b>.
        /// </summary>
        [Pure]
        public static IEnumerable<Transform> GetChildren(this Transform transform) => 
            transform.Cast<Transform>();

        /// <inheritdoc cref="UnityUtils.IsNull{T}"/>
        [Pure]
        public static bool IsNull<T>(this T value) => UnityUtils.IsNull(value);

        [Pure]
        public static bool IsNotNull<T>(this T value)
            => !value.IsNull();

        [Pure]
        public static bool IsNull<T>(this T value, out T t) {
            t = value;
            return UnityUtils.IsNull(value);
        }

        [Pure]
        public static bool IsNotNull<T>(this T value, out T t)
            => !value.IsNull(out t);

        /// <summary>
        /// Returns the <paramref name="value"/> it was invoked on if it isn't null.
        /// Otherwise, return the provided alternate value <paramref name="otherwise"/>.
        /// This null check is performed using <see cref="UnityUtils.IsNull{T}"/>,
        /// so this method is safe-to-use on classes deriving from <see cref="UnityEngine.Object"/>.
        /// Therefore it works equivalent to the null-coalescing operator.
        /// </summary>
        public static T OrWhenNull<T>(this T value, T otherwise) where T : class =>
            !value.IsNull() ? value : otherwise; 
        
        /// <summary>
        /// Returns the <paramref name="value"/> it was invoked on if it isn't null.
        /// Otherwise, return the alternate value received
        /// by calling the <paramref name="otherValueGetter"/>.
        /// This null check is performed using <see cref="UnityUtils.IsNull{T}"/>,
        /// so this method is safe-to-use on classes deriving from <see cref="UnityEngine.Object"/>.
        /// Therefore it works equivalent to the null-coalescing operator.
        /// </summary>
        public static T OrWhenNull<T>(this T value,  Func<T> otherValueGetter) where T : class =>
            !value.IsNull() ? value : otherValueGetter(); 
        
        /// <summary><para>
        /// Invokes the given <code>action</code> when the value is <b>not</b>
        /// null using <code>Util.IsNull</code>, returning a <code>TResult</code>.
        /// If the value itself is null however, it calls <code>elseAction</code> if present.
        /// </para><para>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// </para><para>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling.
        /// </para></summary>
        public static void IfNotNull<T>(
            this T value,
            Action<T> action,
            Action? elseAction = null
        ) where T : class {
            if (!value.IsNull()) action(value);
            else elseAction?.Invoke();
        }

        /// <summary><para>
        /// Invokes the given <code>action</code> when the value is <b>not</b> null using <code>Util.IsNull</code>,
        /// returning a <code>TResult</code>. If the value itself or the result of the action is null however,
        /// it returns the result of <code>elseAction</code>.
        /// </para><para>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// </para><para>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </para></summary>
        public static TResult IfNotNull<T, TResult>(
            this T? value,
            Func<T, TResult> action,
            Func<TResult> elseAction
        ) where T : class =>
            value.IsNotNull() && action(value!).IsNotNull(out var res) 
                ? res : elseAction();

        /// <summary><para>
        /// Invokes the given <code>action</code> when the value is <b>not</b> null using <code>Util.IsNull</code>,
        /// returning a <code>TResult</code>. If the value itself or the result of the action is null however,
        /// it returns <code>elseResult</code> or the type's default value instead.
        /// </para><para>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// </para><para>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </para></summary>
        public static TResult? IfNotNull<T, TResult>(
            this T? value,
            Func<T, TResult> action,
            TResult? elseResult = default
        ) where T : class =>
            IfNotNull(value, action, () => elseResult);

        /// <summary><para>
        /// Destroys the object in the recommended way.
        /// Safe to use in code that is shared between editor and runtime.
        /// </para><para>
        /// In Unity, it is recommended to always use Destroy(obj). However,
        /// when used in editor code, object destruction is delayed forever.
        /// For this reason, DestroyImmediate(obj) must be used in editor code.
        /// This function encapsulates the preprocessor code necessary 
        /// to determine whether the code is being run in editor mode.
        /// </para><para>
        /// Note that transforms cannot be destroyed. When a user still
        /// attempts to destroy a transform, a warning is logged and the
        /// transforms game object is destroyed instead.
        /// </para></summary>
        public static void SafeDestroy(this UnityEngine.Object obj) {
            var transform = obj as Transform;
            if (transform != null) {
                transform.gameObject.SafeDestroy();
                Debug.LogWarning(
                    "Calling SafeDestroy() on a transform. " +
                    "Destroying the GameObject instead, since transforms cannot be destroyed."
                );
                return;
            }

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
            #endif
            UnityEngine.Object.Destroy(obj);
        }

#endregion General Extensions
    }
}