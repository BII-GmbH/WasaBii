#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace BII.WasaBii.Core {
    public static class GenericExtensions {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? AsNullable<T>(this T t) where T : struct => t;

        /// <summary>
        /// If the <paramref name="condition"/> is true, calls the <paramref name="thenFunc"/>
        /// on the given value and returns the result. Returns the original value if false.
        /// </summary>
        /// <remarks>This will have overhead compared to e.g. a normal <c>if</c> or ternary operator.
        /// Only use it for readability purposes and avoid it in performance-critical contexts.
        /// Also, be advised that closures in <paramref name="thenFunc"/> lead to further overhead.
        /// Use the overloads with more parameters in this case.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc) where T : TRes =>
            condition ? thenFunc(t) : t;
        /// <inheritdoc cref="If{T, TRes}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, P, TRes>(this T t, bool condition, Func<T, P, TRes> thenFunc, P param) where T : TRes =>
            condition ? thenFunc(t, param) : t;
        /// <inheritdoc cref="If{T, TRes}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, P1, P2, TRes>(this T t, bool condition, Func<T, P1, P2, TRes> thenFunc, P1 p1, P2 p2) where T : TRes =>
            condition ? thenFunc(t, p1, p2) : t;
        /// <inheritdoc cref="If{T, TRes}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, P1, P2, P3, TRes>(this T t, bool condition, Func<T, P1, P2, P3, TRes> thenFunc, P1 p1, P2 p2, P3 p3) where T : TRes =>
            condition ? thenFunc(t, p1, p2, p3) : t;
        /// <inheritdoc cref="If{T, TRes}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes If<T, P1, P2, P3, P4, TRes>(this T t, bool condition, Func<T, P1, P2, P3, P4, TRes> thenFunc, P1 p1, P2 p2, P3 p3, P4 p4) where T : TRes =>
            condition ? thenFunc(t, p1, p2, p3, p4) : t;

        /// <summary>
        /// Performs action `f` on `t` and returns `t`. Inspired by Kotlin's `also`.
        /// Be sure not to mutate `t` here if it is a struct.
        /// </summary>
        /// <remarks>This will have overhead compared to e.g. using a temporary local variable.
        /// Only use it for readability purposes and avoid it in performance-critical contexts.
        /// Also, be advised that closures in <paramref name="f"/> lead to further overhead.
        /// Use the overloads with more parameters in this case.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T>(this T t, Action<T> f) {
            f(t);
            return t;
        }
        /// <inheritdoc cref="Also{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T, P>(this T t, Action<T, P> f, P param) {
            f(t, param);
            return t;
        }
        /// <inheritdoc cref="Also{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T, P1, P2>(this T t, Action<T, P1, P2> f, P1 p1, P2 p2) {
            f(t, p1, p2);
            return t;
        }
        /// <inheritdoc cref="Also{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T, P1, P2, P3>(this T t, Action<T, P1, P2, P3> f, P1 p1, P2 p2, P3 p3) {
            f(t, p1, p2, p3);
            return t;
        }
        /// <inheritdoc cref="Also{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Also<T, P1, P2, P3, P4>(this T t, Action<T, P1, P2, P3, P4> f, P1 p1, P2 p2, P3 p3, P4 p4) {
            f(t, p1, p2, p3, p4);
            return t;
        }

        /// <summary>
        /// Performs action `f` on `t` and returns the result. Inspired by Kotlin's `let`.
        /// </summary>
        /// <remarks>This will have overhead compared to e.g. using a temporary local variable.
        /// Only use it for readability purposes and avoid it in performance-critical contexts.
        /// Also, be advised that closures in <paramref name="f"/> lead to further overhead.
        /// Use the overloads with more parameters in this case.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static S Let<T, S>(this T t, Func<T, S> f) => f(t);
        /// <inheritdoc cref="Let{T,S}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static R Let<T, P, R>(this T t, Func<T, P, R> f, P param) => f(t, param);
        /// <inheritdoc cref="Let{T,S}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static R Let<T, P1, P2, R>(this T t, Func<T, P1, P2, R> f, P1 p1, P2 p2) => f(t, p1, p2);
        /// <inheritdoc cref="Let{T,S}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static R Let<T, P1, P2, P3, R>(this T t, Func<T, P1, P2, P3, R> f, P1 p1, P2 p2, P3 p3) => f(t, p1, p2, p3);
        /// <inheritdoc cref="Let{T,S}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static R Let<T, P1, P2, P3, P4, R>(this T t, Func<T, P1, P2, P3, P4, R> f, P1 p1, P2 p2, P3 p3, P4 p4) => f(t, p1, p2, p3, p4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1New, T2) MapItem1<T1, T2, T1New>(this (T1, T2) tuple, Func<T1, T1New> f) => (f(tuple.Item1), tuple.Item2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1, T2New) MapItem2<T1, T2, T2New>(this (T1, T2) tuple, Func<T2, T2New> f) => (tuple.Item1, f(tuple.Item2));
        
    }
}