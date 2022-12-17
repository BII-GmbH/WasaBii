using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class FirstOrExtensions {

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> enumerable, Predicate<T>? predicate = null) {
            foreach (var element in enumerable) {
                if (predicate?.Invoke(element) ?? true) return element.Some();
            }
            return Option<T>.None;
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> enumerable) where T : struct
            => enumerable.Select(e => e.AsNullable()).FirstOrDefault();

        public static T? FirstOrNull<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        where T : struct {
            foreach (var item in enumerable)
                if (predicate(item))
                    return item;
            return null;
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, T elseResult) {
            using (var enumerator = enumerable.GetEnumerator()) {
                return enumerator.MoveNext()
                    ? enumerator.Current
                    : elseResult;
            }
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, T elseResult) {
            foreach (var item in enumerable)
                if (predicate(item))
                    return item;
            return elseResult;
        }

        public static T FirstOrThrow<T>(
            this IEnumerable<T> enumerable, Func<Exception> elseException, Predicate<T>? predicate = null
        ) {
            if (predicate == null) predicate = t => t != null;
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) {
                if (predicate(enumerator.Current)) return enumerator.Current;
            }

            throw elseException();
        }
        
    }
}