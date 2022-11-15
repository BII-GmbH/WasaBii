#nullable enable
using System;

namespace BII.WasaBii.Core {
    public static class GenericExtensions {
        public static TRes If<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc)
            => condition ? thenFunc(t) : elseFunc(t);

        public static TRes If<T, TRes>(this T t, Func<bool> condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc)
            => t.If(condition(), thenFunc, elseFunc);

        public static TRes If<T, TRes>(
            this T t, Func<T, bool> condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc
        )
            => t.If(condition(t), thenFunc, elseFunc);

        public static TRes If<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc) where T : TRes
            => t.If(condition, thenFunc, t2 => t2);

        public static TRes If<T, TRes>(this T t, Func<bool> condition, Func<T, TRes> thenFunc) where T : TRes
            => t.If(condition(), thenFunc);

        public static TRes If<T, TRes>(this T t, Func<T, bool> condition, Func<T, TRes> thenFunc) where T : TRes
            => t.If(condition(t), thenFunc);

        public static T? AsNullable<T>(this T t) where T : struct => t;

        /// Performs action `f` on `t` and returns `t`. Inspired by Kotlin's `also`.
        /// Be sure not to mutate `t` here if it is a struct.
        public static T Also<T>(this T t, Action<T> f) {
            f(t);
            return t;
        }

        /// Performs action `f` on `t` and returns the result. Inspired by Kotlin's `let`.
        public static S Let<T, S>(this T t, Func<T, S> f) => f(t);
        public static R Let<T, S, R>(this T t, Func<T, S, R> f, S s) => f(t, s);
        public static (T1New, T2) Let1<T1, T2, T1New>(this (T1, T2) tuple, Func<T1, T1New> f) => (f(tuple.Item1), tuple.Item2);
        public static (T1, T2New) Let2<T1, T2, T2New>(this (T1, T2) tuple, Func<T2, T2New> f) => (tuple.Item1, f(tuple.Item2));
        
    }
}