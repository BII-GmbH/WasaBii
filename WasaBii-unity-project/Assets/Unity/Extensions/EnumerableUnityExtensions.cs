﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Unity.Geometry;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    public static class EnumerableUnityExtensions {
        
        public static Vector3 Sum(this IEnumerable<Vector3> enumerable) => 
            enumerable.Aggregate(Vector3.zero, (v1, v2) => v1 + v2);

        public static T Average<T>(
            this IEnumerable<T> enumerable,
            T seed,
            Func<T, T, T> addition,
            Func<T, int, T> division
        ) {
            var count = 0;
            var sum = seed;
            foreach (var t in enumerable) {
                count++;
                sum = addition(sum, t);
            }
            if (count == 0) return seed;
            return division(sum, count);
        }

        /// <remarks>
        /// <typeparamref name="T"/> is constrained to value types because we use <c>default</c> as a seed.
        /// For most types that you would use with this function (numbers, unit values, ...), this is equivalent to zero.
        /// If we allowed reference types, then the seed would always be null, and we would need additional nullability
        ///  constraints in <paramref name="addition"/> and for the return type.
        /// Use <see cref="Average{T}(System.Collections.Generic.IEnumerable{T},T,System.Func{T,T,T},System.Func{T,int,T})"/>
        ///  with a proper seed instead if you don't have a value type.
        /// </remarks>
        public static T Average<T>(
            this IEnumerable<T> enumerable,
            Func<T, T, T> addition,
            Func<T, int, T> division
        ) where T : struct => Average(enumerable, seed: default, addition, division);

        public static Vector3 Average(this IEnumerable<Vector3> enumerable) =>
            enumerable.Average(
                addition: (vec1, vec2) => vec1 + vec2,
                division: (vec, i) => vec / i
            );

        public static void ForEachDistinctPair<T>(this IEnumerable<T> enumerable, Action<T, T> action)
        where T : IEquatable<T> {
            var list = enumerable.ToList();
            foreach (var t1 in list)
            foreach (var t2 in list)
                if (!t1.Equals(t2))
                    action(t1, t2);
        }

        public static (List<T1>, List<T2>) Unzip<T1, T2>(this IEnumerable<(T1, T2)> enumerable) {
            var t1List = new List<T1>();
            var t2List = new List<T2>();
            
            foreach (var (t1, t2) in enumerable) {
                t1List.Add(t1);
                t2List.Add(t2);
            }

            return (t1List, t2List);
        }

        public static Bounds Bounds(this IEnumerable<Vector3> vertices) {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var vertex in vertices) {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            var ret = new Bounds();
            ret.SetMinMax(min, max);
            return ret;
        }

        public static Rect Bounds(this IEnumerable<Vector2> vertices) {
            using (var enumerator = vertices.GetEnumerator()) {
                if (!enumerator.MoveNext()) throw new ArgumentException("Enumerable must not be empty");
                var min = enumerator.Current;
                var max = min;
                while (enumerator.MoveNext()) {
                    var current = enumerator.Current;
                    min = Vector2.Min(min, current);
                    max = Vector2.Max(max, current);
                }

                return new Rect {
                    min = min,
                    max = max
                };
            }
        }

        public static Length TotalPathLength(this IEnumerable<Vector3> enumerable) {
            var (head, tail) = enumerable;
            return tail.Aggregate(
                    seed: (lastPos: head, currentLength: 0f),
                    (accum, currentPos) => (currentPos, accum.currentLength + accum.lastPos.DistanceTo(currentPos))
                )
                .currentLength.Meters();
        }
    }
}