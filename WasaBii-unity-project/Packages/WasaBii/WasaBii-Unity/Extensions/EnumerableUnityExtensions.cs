#nullable enable

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

        public static Vector3 Average(this IEnumerable<Vector3> enumerable) =>
            enumerable.Average(
                addition: (vec1, vec2) => vec1 + vec2,
                division: (vec, i) => vec / i
            );

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