#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace BII.WasaBii.Core {
    
    public static class GenericExtensions {

        /// <summary>
        /// If the <paramref name="condition"/> is true, calls the <paramref name="thenFunc"/>
        /// on the given value and returns the result. Returns the original value if false.
        /// </summary>
        /// <remarks>This will have overhead compared to e.g. a normal <c>if</c> or ternary operator.
        /// Only use it for readability purposes and avoid it in performance-critical contexts.
        /// Also, be advised that closures in <paramref name="thenFunc"/> lead to further overhead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes ApplyIf<T, TRes>(this T t, bool condition, Func<T, TRes> thenFunc) where T : TRes =>
            condition ? thenFunc(t) : t;
        
        /// <inheritdoc cref="ApplyIf{T, TRes}(T, bool, Func{T, TRes})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRes ApplyIf<T, TRes>(this T t, Func<T, bool> condition, Func<T, TRes> thenFunc) where T : TRes =>
            condition(t) ? thenFunc(t) : t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1New, T2) MapItem1<T1, T2, T1New>(this (T1, T2) tuple, Func<T1, T1New> f) => (f(tuple.Item1), tuple.Item2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T1, T2New) MapItem2<T1, T2, T2New>(this (T1, T2) tuple, Func<T2, T2New> f) => (tuple.Item1, f(tuple.Item2));
        
    }
}