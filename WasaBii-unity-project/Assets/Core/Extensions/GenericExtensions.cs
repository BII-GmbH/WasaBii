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
        
        public static bool TryGetValue<T>(this T? source, out T res) where T : struct {
            if (source is { } val) {
                res = val;
                return true;
            } else {
                res = default;
                return false;
            }
        }

    }
}