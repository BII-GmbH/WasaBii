using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Unity.Exceptions;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary>
    /// Authors: Cameron Reuschel, Daniel Götz
    /// <br/><br/>
    /// Holds a number of extension methods to find a game object's
    /// components of a certain type in a concise and flexible manner.
    /// <br/><br/>
    /// All methods include a parameter of type <see cref="Search"/>,
    /// which enables you to search through all parents until the scene
    /// root, recursively through all children all both until a component
    /// of the specified type can be found.  
    /// </summary>
    // ReSharper disable all InvalidXmlDocComment
    public static class ComponentQueryExtensions {
        private static T SearchParents<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            while (true) {
                var res = mapper(go);
                if (!Util.IsNull(res)) return res;
                if (Util.IsNull(go.transform.parent)) return null;
                go = go.transform.parent.gameObject;
            }
        }

        private static T SearchChildren<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            var res = mapper(go);
            if (!Util.IsNull(res)) return res;
            foreach (var child in go.transform.GetChildren()) {
                var recres = SearchChildren(child.gameObject, mapper);
                if (!Util.IsNull(recres)) return recres;
            }

            return null;
        }

        private static T SearchSiblings<T>(GameObject go, Func<GameObject, T> mapper) where T : class {
            if (go.transform.parent == null) return mapper(go);
            foreach (var child in go.transform.parent.GetChildren()) {
                var res = mapper(child.gameObject);
                if (!Util.IsNull(res)) return res;
            }

            return null;
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
        public static T Find<T>(this GameObject go, [NotNull] Func<GameObject, T> fn, Search where)
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

        /// <inheritdoc cref="Find{T}(UnityEngine.GameObject,System.Func{UnityEngine.GameObject,T},Search)"/>
        [CanBeNull]
        public static T Find<T>(this Transform tr, [NotNull] Func<GameObject, T> fn, Search where = Search.InObjectOnly)
            where T : class =>
            tr.gameObject.Find<T>(fn, @where);

        /// <example>
        /// Before C# 7
        /// <code>
        /// Collider result;
        /// if(gameObject.Is&lt;Collider&gt;(out result))
        ///     result.trigger = true;
        /// </code>
        /// With C# 7
        /// <code>
        /// if(gameObject.Is&lt;Collider&gt;(out var result))
        ///     result.trigger = true;
        /// </code>
        /// </example>
        /// <param name="result">A component of type T if found, null otherwise.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if any object in the specified search scope has a component of type T.</returns>
        public static bool Is<T>(
            this GameObject go, out T result,  Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class {
            result = go.As<T>(where, includeInactive);
            return !Util.IsNull(result);
        }

        /// <inheritdoc cref="Is{T}(UnityEngine.GameObject,out T,Search)" />
        public static bool Is<T>(
            this Component comp, out T result, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class =>
            comp.gameObject.Is<T>(out result, @where, includeInactive);

        /// <inheritdoc cref="Is{T}(UnityEngine.GameObject,out T,Search)" />
        public static bool Is<T>(
            this Collision col, out T result, Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : class =>
            col.gameObject.Is<T>(out result, @where, includeInactive);

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>true if any object in the specified search scope has a component of type T.</returns>
        public static bool Is<T>(this GameObject go, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            go.Is<T>(out _, where, includeInactive);

        /// <inheritdoc cref="Is{T}(GameObject, Search)"/>
        public static bool Is<T>(this Component comp, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            comp.gameObject.Is<T>(where, includeInactive);

        /// <inheritdoc cref="Is{T}(GameObject, Search)"/>
        public static bool Is<T>(this Collision col, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            col.gameObject.Is<T>(where, includeInactive);

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>The first component of type T found in the search scope or throws if not found.</returns>
        [NotNull]
        public static T AsOrThrow<T>(this GameObject go, Search where = Search.InObjectOnly,
            bool includeInactive = false) where T : class =>
            go.As<T>(where, includeInactive).IsNotNull(out var res)
                ? res
                : throw new MissingComponentException($"The component of type {typeof(T)} could not be found on {go}");

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>The first component of type T found in the search scope or null if not found.</returns>
        [CanBeNull]
        public static T As<T>(this GameObject go, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            where switch {
                Search.InObjectOnly => go.GetComponent<T>(),
                Search.InChildren => go.GetComponentInChildren<T>(includeInactive),
                Search.InParents => go.GetComponentInParent<T>(includeInactive),
                Search.InSiblings => go.transform.parent.IsNull(out var p)
                    ? go.As<T>(includeInactive: includeInactive)
                    : go.transform.parent.GetChildren()
                        .Collect(child => child.GetComponent<T>())
                        .FirstOrDefault(),
                Search.InWholeHierarchy =>
                    go.transform.parent.IsNotNull(out var p) &&
                    p.gameObject.GetComponentInParent<T>(includeInactive).IsNotNull(out var parentSearch)
                        ? parentSearch
                        : go.GetComponentInChildren<T>(includeInactive),
                _ => throw new UnsupportedSearchException(where)
            };

        /// <inheritdoc cref="As{T}(GameObject, Search)"/>
        [CanBeNull]
        public static T As<T>(this Component comp, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            comp.gameObject.As<T>(@where, includeInactive);

        /// <inheritdoc cref="AsOrThrow{T}(GameObject, Search)"/>
        [NotNull]
        public static T AsOrThrow<T>(this Component comp, Search where = Search.InObjectOnly,
            bool includeInactive = false) where T : class =>
            comp.gameObject.AsOrThrow<T>(@where, includeInactive);

        /// <inheritdoc cref="As{T}(GameObject, Search)"/>
        [CanBeNull]
        public static T As<T>(this Collision col, Search where = Search.InObjectOnly, bool includeInactive = false)
            where T : class =>
            col.gameObject.As<T>(@where, includeInactive);

        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <returns>A lazily generated IEnumerable of all components of type T found in the search scope. Might be empty.</returns>
        public static IEnumerable<T> All<T>(this GameObject go, Search where = Search.InObjectOnly,
            bool includeInactive = false) where T : class {
            return where switch {
                Search.InObjectOnly => go.GetComponents<T>(),
                Search.InParents => go.GetComponentsInParent<T>(includeInactive),
                Search.InChildren => go.GetComponentsInChildren<T>(includeInactive),
                Search.InSiblings => go.transform.parent.IsNull(out var p)
                    ? go.All<T>(includeInactive: includeInactive)
                    : go.transform.parent.GetChildren()
                        .Collect(c => c.As<T>(includeInactive: includeInactive))
                        .ToArray(),
                Search.InWholeHierarchy => go.transform.parent.IfNotNull(
                        p => p.gameObject.GetComponentsInParent<T>(includeInactive), elseResult: Enumerable.Empty<T>())
                    .Concat(go.GetComponentsInChildren<T>(includeInactive)),
                _ => throw new UnsupportedSearchException(where)
            };
        }

        /// <inheritdoc cref="All{T}(GameObject, Search)"/>
        public static IEnumerable<T> All<T>(this Component comp, Search where = Search.InObjectOnly,
            bool includeInactive = false) where T : class =>
            comp.gameObject.All<T>(@where, includeInactive);

        /// <inheritdoc cref="All{T}(GameObject, Search)"/>
        public static IEnumerable<T> All<T>(this Collision col, Search where = Search.InObjectOnly,
            bool includeInactive = false) where T : class =>
            col.gameObject.All<T>(@where, includeInactive);

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
        public static void AssignComponent<T>(this GameObject go, out T variable, Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : class {
            T found = go.As<T>(where, includeInactive);
            if (Util.IsNull(found))
                throw new ComponentNotFoundException(
                    "Failed to assign component of type " + typeof(T) + " to " + go + ".");

            variable = found;
        }

        /// <summary>
        /// Searches through the specified search scope until a component of type T is found
        /// and assigns it to the passed variable reference. When no component could be found
        /// in the specified scope, a new component of type T is added to the game object instead.
        /// </summary>
        /// <param name="variable">A reference to the variable to be set.</param>
        /// <param name="where">Optional search scope if the object itself does not have the component.</param>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        public static void AssignComponentOrAdd<T>(this GameObject go, out T variable,
            Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : Component {
            T found = go.As<T>(where, includeInactive);
            if (found == null) found = go.AddComponent<T>();

            variable = found;
        }

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
        public static bool AssignIfAbsent<T>(this GameObject go, ref T variable, Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : class {
            if (variable != default(T)) // safer than null
            {
                Debug.Log(
                    "Tried to assign component of type " + typeof(T) + " but field already had value " + variable, go);
                return false;
            }

            go.AssignComponent(out variable, where, includeInactive);
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
        public static bool AssignIfAbsentOrAdd<T>(this GameObject go, ref T variable,
            Search where = Search.InObjectOnly,
            bool includeInactive = false)
            where T : Component {
            if (!Util.IsNull(variable)) {
                Debug.Log(
                    "Tried to assign component of type " + typeof(T) + " but field already had value " + variable, go);
                return false;
            }

            go.AssignComponentOrAdd(out variable, where, includeInactive);
            return true;
        }
    }
}