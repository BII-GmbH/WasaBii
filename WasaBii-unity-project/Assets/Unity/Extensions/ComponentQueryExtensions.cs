using System;
using CoreLibrary;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.Utilities.Unity {
    public static class ComponentQueryExtensions {
        /// <inheritdoc cref="Is{TSource,TResult}(TSource,out TResult,CoreLibrary.Search)"/>
        public static bool Is<TSource, TResult>(this TSource obj, Search where = Search.InObjectOnly)
        where TResult : class =>
            obj.Is<TSource, TResult>(out _, where);

        /// <summary>
        /// This function is designed to extend the Corelibrary <code>Is</code> functions
        /// so that it can be called on any class, not just GameObjects and Components.
        /// </summary>
        public static bool Is<TSource, TResult>(this TSource obj, out TResult ret, Search where = Search.InObjectOnly)
        where TResult : class {
            switch (obj) {
                case TResult t:
                    ret = t;
                    return true;
                case Component component:   return component.Is<TResult>(out ret, where);
                case GameObject gameObject: return gameObject.Is<TResult>(out ret, where);
                default:
                    ret = null;
                    return false;
            }
        }

        public static TResult As<TSource, TResult>(this TSource obj, Search where = Search.InObjectOnly)
        where TResult : class
            => obj.Is<TSource, TResult>(out var ret, where) ? ret : null;

        public static void DoIfType<T>(
            this object obj, [NotNull] Action<T> ifAction, Action elseAction = null, Search where = Search.InObjectOnly
        ) where T : class {
            if (obj.Is<object, T>(out var res, where))
                ifAction.Invoke(res);
            else
                elseAction?.Invoke();
        }
    }
}