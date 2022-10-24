using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class LastOrExtensions {
        
        public static T? LastOrNull<T>(this IEnumerable<T> enumerable) where T : struct
            => enumerable.Reverse().FirstOrNull();

        public static T? LastOrNull<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        where T : struct
            => enumerable.Reverse().FirstOrNull(predicate);

        public static T LastOr<T>(this IEnumerable<T> enumerable, T elseResult)
            => enumerable.Reverse().FirstOr(elseResult);

        public static T LastOr<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, T elseResult)
            => enumerable.Reverse().FirstOr(predicate, elseResult);

        public static Option<T> LastOrNone<T>(this IEnumerable<T> enumerable, Predicate<T>? predicate = null) =>
            enumerable.Reverse().FirstOrNone(predicate);

        public static T LastOrThrow<T>(
            this IEnumerable<T> enumerable,
            Func<Exception> elseException,
            Predicate<T>? predicate = null
        ) => enumerable.LastOrNone(predicate).GetOrThrow(elseException);
    }
}