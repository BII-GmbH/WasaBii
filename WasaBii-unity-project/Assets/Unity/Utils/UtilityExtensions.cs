using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// Author: Cameron Reuschel, David Schantz
    /// <br/><br/>
    /// The class holding all nonspecific extension methods in the core library.
    public static class UtilityExtensions {
        
#region General Extensions

        /// An IEnumerable containing all the children of this Transform in order.
        /// It is safe to modify the transforms children during iteration.
        /// <b>Iterable only once</b>.
        [Pure]
        public static IEnumerable<Transform> GetChildren(this Transform transform) => 
            transform.Cast<Transform>(); // for proper typing

        /// <inheritdoc cref="Util.IsNull{T}"/>
        [Pure]
        public static bool IsNull<T>(this T value) => Util.IsNull(value);

        [Pure]
        public static bool IsNotNull<T>(this T value)
            => !value.IsNull();

        [Pure]
        public static bool IsNull<T>(this T value, out T t) {
            t = value;
            return Util.IsNull(value);
        }

        [Pure]
        public static bool IsNotNull<T>(this T value, out T t)
            => !value.IsNull(out t);

        /// Returns the <paramref name="value"/> it was invoked on if it isn't null.
        /// Otherwise, return the provided alternate value <paramref name="otherwise"/>.
        /// This null check is performed using <see cref="Util.IsNull{T}"/>,
        /// so this method is safe-to-use on classes deriving from <see cref="UnityEngine.Object"/>.
        /// Therefore it works equivalent to the null-coalescing operator.
        public static T OrWhenNull<T>(this T value, T otherwise) where T : class =>
            !value.IsNull() ? value : otherwise; 
        
        /// Returns the <paramref name="value"/> it was invoked on if it isn't null.
        /// Otherwise, return the alternate value received
        /// by calling the <paramref name="otherValueGetter"/>.
        /// This null check is performed using <see cref="Util.IsNull{T}"/>,
        /// so this method is safe-to-use on classes deriving from <see cref="UnityEngine.Object"/>.
        /// Therefore it works equivalent to the null-coalescing operator.
        public static T OrWhenNull<T>(this T value,  Func<T> otherValueGetter) where T : class =>
            !value.IsNull() ? value : otherValueGetter(); 
        
        /// Invokes the given <code>action</code> when the value is <b>not</b>
        /// null using <code>Util.IsNull</code>, returning a <code>TResult</code>.
        /// If the value itself is null however, it calls <code>elseAction</code> if present.
        /// <br/>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// <br/>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        public static void IfNotNull<T>(
            this T value,
            Action<T> action,
            Action elseAction = null
        ) where T : class {
            if (!value.IsNull()) action(value);
            else elseAction?.Invoke();
        }

        /// <summary>
        /// Invokes the given <code>action</code> when the value is <b>not</b> null using <code>Util.IsNull</code>,
        /// returning a <code>TResult</code>. If the value itself or the result of the action is null however,
        /// it returns the result of <code>elseAction</code>.
        /// <br/>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// <br/>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </summary>
        public static TResult IfNotNull<T, TResult>(
            this T value,
            Func<T, TResult> action,
            Func<TResult> elseAction
        ) where T : class =>
            value.IsNotNull() && action(value).IsNotNull(out var res) 
                ? res : elseAction();

        /// <summary>
        /// Invokes the given <code>action</code> when the value is <b>not</b> null using <code>Util.IsNull</code>,
        /// returning a <code>TResult</code>. If the value itself or the result of the action is null however,
        /// it returns <code>elseResult</code> or the type's default value instead.
        /// <br/>
        /// This method is designed as a replacement for patterns such as <code>value?.action() ?? elseAction()</code>.
        /// <br/>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </summary>
        public static TResult IfNotNull<T, TResult>(
            this T value,
            Func<T, TResult> action,
            TResult elseResult = default
        ) where T : class =>
            IfNotNull(value, action, () => elseResult);

        /// <summary>
        /// Destroys the object in the recommended way.
        /// Safe to use in code that is shared between editor and runtime.
        /// <br/>
        /// In Unity, it is recommended to always use Destroy(obj). However,
        /// when used in editor code, object destruction is delayed forever.
        /// For this reason, DestroyImmediate(obj) must be used in editor code.
        /// This function encapsulates the preprocessor code necessary 
        /// to determine whether the code is being run in editor mode.
        /// <br/>
        /// Note that transforms cannot be destroyed. When a user still
        /// attempts to destroy a transform, a warning is logged and the
        /// transforms game object is destroyed instead.
        /// </summary>
        /// <param name="obj"></param>
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
#region Linq Style Extensions

        /// <summary>
        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action) {
            foreach (var item in sequence) action(item);
        }

        /// <summary>
        /// <inheritdoc cref="ForEach{T}(System.Collections.Generic.IEnumerable{T},System.Action{T})"/>
        /// </summary>
        public static void ForEach<T1, T2>(this IEnumerable<(T1, T2)> sequence, Action<T1, T2> action) {
            foreach (var (t1, t2) in sequence) action(t1, t2);
        }
        
        /// <summary>
        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once. The action also takes
        /// the index of the element as second argument, thus allowing you to potentially replace
        /// simple counting for loops with this function.
        /// </summary>
        public static void ForEachWithIndex<T>(this IEnumerable<T> sequence, Action<T, int> action) {
            var i = 0;
            foreach (var item in sequence) action(item, i++);
        }

        /// <summary>
        /// Equal to calling <code>.Select(mapping).Where(v => v != null)</code>
        /// <br/>
        /// Nice for calling functions that may return no result such as
        /// <code>.Collect(v => v.As&lt;Whatever&gt;())</code>
        /// </summary>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, TRes> mapping
        ) where TRes : class =>
            sequence.Select(mapping).Where(v => v != null);

        /// <inheritdoc cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, int, TRes> mappingWithIndex
        ) where TRes : class =>
            sequence.Select(mappingWithIndex).Where(v => v != null);

        /// <summary>
        /// Similar to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>.
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        /// </summary>
        public static IEnumerable<TRes> Collect<T, TRes>(this IEnumerable<T> sequence, Func<T, TRes?> mapping)
        where TRes : struct {
            foreach (var value in sequence) if (mapping(value) is TRes res) yield return res;
        }

        /// <summary>
        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>
        /// <br/>
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        /// </summary>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes>> mapping
        ) {
            foreach (var value in sequence) 
            foreach (var mapped in mapping(value))
                if (mapped != null) yield return mapped;
        }
        
        /// <summary>
        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>
        /// <br/>
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,Nullable{TRes}})"/>
        /// </summary>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes?>> mapping
        ) where TRes : struct {
            foreach (var value in sequence) 
            foreach (var mapped in mapping(value))
                if (mapped is TRes res) yield return res;
        }

        /// <returns>True if the specified sequence contains no elements, false otherwise.</returns>
        public static bool IsEmpty<T>(this IEnumerable<T> sequence) => !sequence.Any();

        /// <returns>False if the specified sequence contains no elements, true otherwise.</returns>
        public static bool IsNotEmpty<T>(this IEnumerable<T> sequence) => sequence.Any();
        
        [NotNull] public static IEnumerable<T> AfterwardsDo<T>(this IEnumerable<T> enumerable, Action afterwards) {
            try {
                foreach (var value in enumerable) yield return value;
            } finally {
                afterwards();
            }
        }

        private static readonly System.Random Rng = new System.Random();

        /// <summary>
        /// Shuffles this sequence, yielding a <b>new</b> IEnumerable with all elements in random order.
        /// Uses the <a href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle">Fisher–Yates algorithm</a>.
        /// <br/>If the passed IEnumerable is only iterable once it is consumed in the process.
        /// </summary>
        [NotNull] public static List<T> Shuffled<T>(this IEnumerable<T> l, System.Random random = null) {
            if (random == null) random = Rng;
            var list = new List<T>(l);
            var n = list.Count;
            while (n > 1) {
                n--;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }
#endregion Linq Style Extensions
    }
}