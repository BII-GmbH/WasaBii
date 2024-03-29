﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {

    public static class DictionaryExtensions {
        
        public static TVal GetOrElse<TKey, TVal>(
            this IReadOnlyDictionary<TKey, TVal> dict,
            TKey key,
            Func<TVal> elseResultGetter
        ) => dict.TryGetValue(key, out var existingValue) ? existingValue : elseResultGetter();

        public static void AppendOrAdd<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue item) {
            if (dict.TryGetValue(key, out var list))
                list.Add(item);
            else
                dict.Add(key, new List<TValue> {item});
        }

        public static void IncrementOrAdd<TKey>(this IDictionary<TKey, int> dict, TKey key, int defaultValue = 0) {
            if (dict.TryGetValue(key, out var current))
                dict[key] = current + 1;
            else
                dict.Add(key, defaultValue);
        }

        public static void AppendOrAdd<TKey, TValue>(
            this IDictionary<TKey, HashSet<TValue>> dict,
            TKey key,
            TValue item
        ) {
            if (dict.TryGetValue(key, out var list))
                list.Add(item);
            else
                dict.Add(key, new HashSet<TValue> {item});
        }

        public static bool IsContentEqualTo<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dict,
            IReadOnlyDictionary<TKey, TValue> otherDict,
            IEqualityComparer<TValue>? comparer = null
        ) {
            if (dict.Count != otherDict.Count) return false;
            comparer ??= EqualityComparer<TValue>.Default;
            return dict.All(
                kv => otherDict.TryGetValue(kv.Key, out var otherValue) && comparer.Equals(kv.Value, otherValue)
            );
        }

        public static TVal GetOrAdd<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key, Func<TVal> valueProvider) {
            if (dict.TryGetValue(key, out var val)) return val;
            val = valueProvider();
            dict.Add(key, val);
            return val;
        }

        public static TVal GetOrAdd<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key, TVal valueIfNotPresent) {
            if (dict.TryGetValue(key, out var val)) return val;
            dict.Add(key, valueIfNotPresent);
            return valueIfNotPresent;
        }

        public static Option<TVal> TryGetValue<TKey, TVal>(this IReadOnlyDictionary<TKey, TVal> dict, TKey key) =>
            dict.TryGetValue(key, out var val) ? Option.Some(val) : Option.None;

        public static void ReplaceKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey replaceWith) {
            if (!dict.TryGetValue(oldKey, out var value)) return;
            dict.Remove(oldKey);
            dict.Add(replaceWith, value);
        }

        public static KeyValuePair<A, B> ToKVP<A, B>(this (A, B) tuple) => new(tuple.Item1, tuple.Item2);
        
        /// <summary>
        /// Allows key value pairs to be deconstructed into a tuple implicitly, 
        /// making foreach loops over dictionaries more readable
        /// </summary>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value) {
            key = kvp.Key;
            value = kvp.Value;
        }

    }

}