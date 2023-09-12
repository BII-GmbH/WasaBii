#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace BII.WasaBii.Core {
    public static class GenericExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc) => 
            condition ? thenFunc(t) : elseFunc(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, Func<bool> condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc) => 
            condition() ? thenFunc(t) : elseFunc(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, Func<T, bool> condition, Func<T, TRes> thenFunc, Func<T, TRes> elseFunc) => 
            condition(t) ? thenFunc(t) : elseFunc(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc) where T : TRes =>
            condition ? thenFunc(t) : t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, Func<bool> condition, Func<T, TRes> thenFunc) where T : TRes =>
            condition() ? thenFunc(t) : t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, Func<T, bool> condition, Func<T, TRes> thenFunc) where T : TRes => 
            condition(t) ? thenFunc(t) : t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? AsNullable<T>(this T t) where T : struct => t;

        /// Performs action `f` on `t` and returns `t`. Inspired by Kotlin's `also`.
        /// Be sure not to mutate `t` here if it is a struct.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T>(this T t, Action<T> f) {
            f(t);
            return t;
        }

        /// Performs action `f` on `t` and returns the result. Inspired by Kotlin's `let`.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static S Let<T, S>(this T t, Func<T, S> f) => f(t);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static R Let<T, S, R>(this T t, Func<T, S, R> f, S s) => f(t, s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1New, T2) MapItem1<T1, T2, T1New>(this (T1, T2) tuple, Func<T1, T1New> f) => (f(tuple.Item1), tuple.Item2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1, T2New) MapItem2<T1, T2, T2New>(this (T1, T2) tuple, Func<T2, T2New> f) => (tuple.Item1, f(tuple.Item2));
        
    }
}