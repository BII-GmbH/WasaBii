﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class OtherCollectionExtensions {
        
        public static bool TryPeek<T>(this Stack<T> stack, out T t) {
            if (stack.Any()) {
                t = stack.Peek();
                return true;
            } else {
                t = default!;
                return false;
            }
        }
        
        // first item is to prevent `list.Add()`, which adds nothing.
        // second item is to prevent `list.Add(something)`, which is just a single add
        public static void Add<T>(this IList<T> source, T firstItem, T secondItem, params T[] items) {
            source.Add(firstItem);
            source.Add(secondItem);
            if(source is List<T> l) l.AddRange(items);
            else foreach(var item in items) source.Add(item);
        }
        
        public static ImmutableHashSet<T> AddAllImmutable<T>(this ImmutableHashSet<T> set, IEnumerable<T> toAdd) {
            var builder = set.ToBuilder();
            builder.AddAll(toAdd);
            return builder.ToImmutable();
        }
        
        public static void AddAll<T>(this ISet<T> set, IEnumerable<T> toAdd) => set.UnionWith(toAdd);
        
        public static string Join(this IEnumerable<string> enumerable, string separator = "") => string.Join(separator, enumerable);
    }
}