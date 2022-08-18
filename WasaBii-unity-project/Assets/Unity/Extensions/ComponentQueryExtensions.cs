using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Unity.Exceptions;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {

    /// <summary><para>
    /// Holds a number of extension methods to find a game object's
    /// components of a certain type in a concise and flexible manner.
    /// </para><para>
    /// All methods include a parameter of type <see cref="Search"/>,
    /// which enables you to search through all parents until the scene
    /// root, recursively through all children all both until a component
    /// of the specified type can be found.  
    /// </para></summary>
    // ReSharper disable all InvalidXmlDocComment
    public static class ComponentQueryExtensions {
        
        private static Option<T> ToOption<T>(this T component) =>
            Util.IsNull(component) ? Option.None : Option.Some(component);
        
        private static Option<T> SearchParents<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            while (true) {
                var res = mapper(go);
                if (!Util.IsNull(res)) return res;
                if (Util.IsNull(go.transform.parent)) return Option.None;
                go = go.transform.parent.gameObject;
            }
        }

        private static Option<T> SearchChildren<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            var toSearch = new Queue<GameObject>(new []{go});
            while (toSearch.IsNotEmpty()) {
                var curr = toSearch.Dequeue();
                var res = mapper(curr);
                if (!Util.IsNull(res)) return res;
                go.transform.GetChildren().ForEach(c => toSearch.Enqueue(c.gameObject));
            }
            return Option.None;
        }

        private static Option<T> SearchSiblings<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            if (go.transform.parent == null) return mapper(go);
            foreach (var child in go.transform.parent.GetChildren()) {
                var res = mapper(child.gameObject);
                if (!Util.IsNull(res)) return res;
            }
            return Option.None;
        }

        /// <summary>
        /// This method enables you to search an object's hierarchy
        /// for a more complex conditions. The passed function <paramref name="fn"/>
        /// is applied to every object in the specified search path until
        /// it returns a value that is not null. This value is returned.
        /// When nothing is found, null is returned.
        /// </summary>
        /// <example>
        /// T GetComponentInParent<T>(GameObject go) =>
        ///     go.Find(obj => obj.As<T>(), Search.InParents);
        /// </example>
        /// <param name="fn">
        /// This function is applied to every game object in the search path
        /// until it returns a value that is not null. This result is returned.
        /// </param>
        /// <param name="where">
        /// The search scope if <paramref name="fn"/> returns null for the object itself.
        /// </param>
        /// <returns
        /// >The first non-null result from applying <paramref name="fn"/>
        /// to each game object in the search path, or null if nothing was found.
        /// </returns>
        [CanBeNull]
        public static Option<T> Find<T>(this GameObject go, [NotNull] Func<GameObject, T> fn, Search where)
            where T : class =>
            where switch {
                Search.InObjectOnly => fn(go),
                Search.InChildren => SearchChildren(go, fn),
                Search.InParents => SearchParents(go, fn),
                Search.InSiblings => SearchSiblings(go, fn),
                Search.InWholeHierarchy => 
                    go.transform.parent.IsNotNull(out var p) && 
                    SearchParents(p.gameObject, fn).IsNotNull(out var parentSearch) 
                        ? parentSearch : SearchChildren(go, fn),
                _ => throw new UnsupportedSearchException(where)
            };

        /// <inheritdoc cref="Find{T}(GameObject,System.Func{UnityEngine.GameObject,T},Search)"/>
        [CanBeNull]
        public static Option<T> Find<T>(this Transform tr, [NotNull] Func<GameObject, T> fn, Search where = Search.InObjectOnly)
            where T : class =>
            tr.gameObject.Find<T>(fn, @where);

        /// <example>
        /// <code>
        /// if(gameObject.Is&lt;Collider&gt;(out var result))
        ///     result.trigger = true;
        /// </code>
        /// </example>
        /// <param name="result">A component of type T if found, null otherwise.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if any object in the specified search scope has a component of type T.</returns>
        public static bool IsComponent<T>(
            this GameObject go, out T result, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class => go.AsComponent<T>(where, includeInactive).TryGetValue(out result);

        /// <inheritdoc cref="IsComponent{T}(UnityEngine.GameObject,out T,BII.WasaBii.Unity.Search,bool)" />
        public static bool IsComponent<T>(
            this Component comp, out T result, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class =>
            comp.gameObject.IsComponent<T>(out result, @where, includeInactive);

        /// <inheritdoc cref="IsComponent{T}(UnityEngine.GameObject,out T,BII.WasaBii.Unity.Search,bool)" />
        public static bool IsComponent<T>(this object obj, out T ret, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class {
            switch (obj) {
                case T t:
                    ret = t;
                    return true;
                case Component component:   return component.IsComponent<T>(out ret, where, includeInactive);
                case GameObject gameObject: return gameObject.IsComponent<T>(out ret, where, includeInactive);
                default:
                    ret = null;
                    return false;
            }
        }

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if any object in the specified search scope has a component of type T.</returns>
        public static bool IsComponent<T>(this GameObject go, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            go.IsComponent<T>(out _, where, includeInactive);

        /// <inheritdoc cref="IsComponent{T}(UnityEngine.GameObject,BII.WasaBii.Unity.Search,bool)"/>
        public static bool IsComponent<T>(this Component comp, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            comp.gameObject.IsComponent<T>(where, includeInactive);
        
        /// <inheritdoc cref="IsComponent{T}(UnityEngine.GameObject,BII.WasaBii.Unity.Search,bool)" />
        public static bool IsComponent<T>(this object obj, Search where = Search.InObjectOnly)
            where T : class => obj.IsComponent<T>(out _, where);

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>The first component of type T found in the search scope or throws if not found.</returns>
        public static T AsComponentOrThrow<T>(
            this GameObject go, 
            Search where = Search.InObjectOnly,
            bool includeInactive = false
        ) where T : class =>
            go.AsComponent<T>(where, includeInactive).GetOrThrow(() =>
                new MissingComponentException($"The component of type {typeof(T)} could not be found on {go}"));

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>The first component of type T found in the search scope or null if not found.</returns>
        public static Option<T> AsComponent<T>(
            this GameObject go, 
            Search where = Search.InObjectOnly, 
            bool includeInactive = false
        ) where T : class =>
            where switch {
                Search.InObjectOnly => go.GetComponent<T>().ToOption(),
                Search.InChildren => go.GetComponentInChildren<T>(includeInactive).ToOption(),
                Search.InParents => go.GetComponentInParent<T>(includeInactive).ToOption(),
                Search.InSiblings => go.transform.parent.IsNull(out var p)
                    ? go.AsComponent<T>(includeInactive: includeInactive)
                    : go.transform.parent.GetChildren()
                        .Collect(child => child.GetComponent<T>())
                        .FirstOrDefault().ToOption(),
                Search.InWholeHierarchy =>
                    go.transform.parent.IsNotNull(out var p) &&
                    p.gameObject.GetComponentInParent<T>(includeInactive).IsNotNull(out var parentSearch)
                        ? parentSearch.ToOption()
                        : go.GetComponentInChildren<T>(includeInactive).ToOption(),
                _ => throw new UnsupportedSearchException(where)
            };

        /// <inheritdoc cref="As{T}(GameObject, Search)"/>
        public static Option<T> AsComponent<T>(
            this Component comp, 
            Search where = Search.InObjectOnly, 
            bool includeInactive = false
        ) where T : class =>
            comp.gameObject.AsComponent<T>(where, includeInactive);

        /// <inheritdoc cref="AsOrThrow{T}(GameObject, Search)"/>
        public static T AsComponentOrThrow<T>(
            this Component comp, 
            Search where = Search.InObjectOnly, 
            bool includeInactive = false
        ) where T : class =>
            comp.gameObject.AsComponentOrThrow<T>(where, includeInactive);

        /// <inheritdoc cref="As{T}(GameObject, Search)"/>
        public static Option<T> AsComponent<T>(
            this object obj, 
            Search where = Search.InObjectOnly, 
            bool includeInactive = false
        ) where T : class => 
            obj.IsComponent<T>(out var res, where, includeInactive) ? res : Option.None;

        /// <inheritdoc cref="AsOrThrow{T}(GameObject, Search)"/>
        public static T AsComponentOrThrow<T>(
            this object obj, 
            Search where = Search.InObjectOnly, 
            bool includeInactive = false
        ) where T : class => 
            obj.AsComponent<T>(where, includeInactive).GetOrThrow(
                () => new MissingComponentException($"The component of type {typeof(T)} could not be found on {obj}"));

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>A lazily generated IEnumerable of all components of type T found in the search scope. Might be empty.</returns>
        public static IEnumerable<T> All<T>(
            this GameObject go, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class => where switch {
            Search.InObjectOnly => go.GetComponents<T>(),
            Search.InParents => go.GetComponentsInParent<T>(includeInactive),
            Search.InChildren => go.GetComponentsInChildren<T>(includeInactive),
            Search.InSiblings => go.transform.parent.IsNull(out var p)
                ? go.All<T>(includeInactive: includeInactive)
                : go.transform.parent.GetChildren()
                    .Collect(c => c.AsComponent<T>(includeInactive: includeInactive))
                    .ToArray(),
            Search.InWholeHierarchy => go.transform.parent.IfNotNull(p => 
                    p.gameObject.GetComponentsInParent<T>(includeInactive), elseResult: Enumerable.Empty<T>())
                .Concat(go.GetComponentsInChildren<T>(includeInactive)),
            _ => throw new UnsupportedSearchException(where)
        };

        /// <inheritdoc cref="All{T}(GameObject, Search)"/>
        public static IEnumerable<T> All<T>(
            this Component comp, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class => comp.gameObject.All<T>(where, includeInactive);

        /// <inheritdoc cref="All{T}(GameObject, Search)"/>
        public static IEnumerable<T> All<T>(
            this object obj,  Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class =>
            obj switch {
                GameObject go => go.All<T>(where, includeInactive),
                Component comp => comp.All<T>(where, includeInactive),
                T t => new[] { t },
                _ => Enumerable.Empty<T>()
            };

        /// <summary>
        /// Searches through the specified search scope until a component of type T is found
        /// and assigns it to the passed variable reference. Throws an exception if nothing could be found.
        /// </summary>
        /// <param name="variable">A reference to the variable to be set.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <exception cref="ComponentNotFoundException">
        /// If there was no component to be found in the specified search scope.
        /// </exception>
        public static void AssignComponent<T>(
            this MonoBehaviour m, 
            out T component, 
            Search where = Search.InObjectOnly,
            bool includeInactive = false
        ) where T : class {
            if (!m.gameObject.AsComponent<T>(where, includeInactive).TryGetValue(out component)) 
                throw new ComponentNotFoundException(
                    "Failed to assign component of type " + typeof(T) + " to " + m.gameObject + ".");
        }

        /// <summary>
        /// Searches through the specified search scope until a component of type T is found
        /// and assigns it to the passed variable reference. When no component could be found
        /// in the specified scope, a new component of type T is added to the game object instead.
        /// </summary>
        /// <param name="variable">A reference to the variable to be set.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        public static void AssignComponentOrAdd<T>(
            this MonoBehaviour m, 
            out T component,
            Search where = Search.InObjectOnly,
            bool includeInactive = false
        ) where T : Component => 
            component = m.gameObject.AsComponent<T>(where, includeInactive)
                .GetOrElse(() => m.gameObject.AddComponent<T>());

        /// <summary>
        /// Searches through the specified search scope until a component of type T is found
        /// and assigns it to the passed variable reference if and only iff the variable has
        /// nothing assigned to it yet. Throws an exception if nothing could be found.
        /// </summary>
        /// <seealso cref="AssignComponent{T}"/>
        /// <param name="variable">A reference to the variable to be set if unset so far.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if new value was assigned, false if variable already has a value.</returns>
        /// <exception cref="Exception">If there was no component to be found in the specified search scope.</exception>
        public static bool AssignIfAbsent<T>(this MonoBehaviour m, ref T variable, Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : class {
            if (variable != default(T)) // safer than null
            {
                Debug.Log(
                    "Tried to assign component of type " + typeof(T) + " but field already had value " + variable, m.gameObject);
                return false;
            }

            m.AssignComponent(out variable, where, includeInactive);
            return true;
        }

        /// <summary>
        /// Searches through the specified search scope until a component of type T is found
        /// and assigns it to the passed variable reference if and only iff the variable has
        /// nothing assigned to it yet. When no component could be found in the specified scope,
        /// a new component of type T is added to the game object instead.
        /// </summary>
        /// <param name="variable">A reference to the variable to be set.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if new value was assigned, false if variable already has a value.</returns>
        public static bool AssignIfAbsentOrAdd<T>(this MonoBehaviour m, ref T variable,
            Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : Component {
            if (!Util.IsNull(variable)) {
                Debug.Log(
                    "Tried to assign component of type " + typeof(T) + " but field already had value " + variable, m.gameObject);
                return false;
            }

            m.AssignComponentOrAdd(out variable, where, includeInactive);
            return true;
        }
    }
}