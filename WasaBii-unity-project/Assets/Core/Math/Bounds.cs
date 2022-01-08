using System;
using System.Collections.Generic;
using System.Numerics;

namespace BII.WasaBii.Core {

    /// A Unity-Independent data structure representing an AABB (axis-aligned bounding-box) 
    [Serializable]
    public readonly struct Bounds {
        
        public readonly Vector3 Center;
        
        public readonly Vector3 Size;
        public Vector3 Extends => Size / 2.0f;

        public Vector3 BottomCenter => new Vector3(Center.X, Center.Y - (Size.Y / 2.0f), Center.Z);
        public Vector3 TopCenter => new Vector3(Center.X, Center.Y + (Size.Y / 2.0f), Center.Z);

        public Vector3 Min => Center - Extends;
        public Vector3 Max => Center + Extends;

        public static Bounds FromMinMax(Vector3 min, Vector3 max) => new Bounds((max + min) / 2.0f, (max - min));

        public Bounds(Vector3 center, Vector3 size) {
            Center = center;
            Size = new Vector3(
                Math.Abs(size.X),    
                Math.Abs(size.Y),    
                Math.Abs(size.Z)    
            );
        }

        public static Bounds Encapsulating(IEnumerable<Vector3> positions) {
            using var enumerator = positions.GetEnumerator();

            if (!enumerator.MoveNext())
                return new Bounds();

            var res = new Bounds(enumerator.Current, size: Vector3.Zero);
            while (enumerator.MoveNext()) {
                res = res.Encapsulate(enumerator.Current);
            }

            return res;
        }

        public Bounds Encapsulate(Vector3 point) => Bounds.FromMinMax(Vector3.Min(Min, point), Vector3.Max(Max, point));

        public bool Contains(Vector3 point) 
            => point.X >= Min.X
            && point.X <= Max.X
            && point.Y >= Min.Y
            && point.Y <= Max.Y
            && point.Z >= Min.Z
            && point.Z <= Max.Z;
    }
    

    public static class BoundsExtensions {
        public static Bounds And(this Bounds a, Bounds b) {
            // `new Bounds(Vector3, Vector3)` takes `center` and `size`.
            // Not `min` and `max`.
            // The types are the same and swapping these can lead to several hours of debugging.
            // Every time.
            // And they say you learn from your mistakes.
            // Apparently I don't.
            return Bounds.FromMinMax(
                Vector3.Min(a.Min, b.Min),
                Vector3.Max(a.Max, b.Max)
            );
        }
        
        public static Bounds CalculateBounds(this IEnumerable<Vector3> vertices) {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var vertex in vertices) {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
            return Bounds.FromMinMax(min, max);
        }

        public static IEnumerable<Vector3> Vertices(this Bounds bounds) {
            var min = bounds.Min;
            var max = bounds.Max;
            yield return min;
            yield return new Vector3(min.X, min.Y, max.Z);
            yield return new Vector3(min.X, max.Y, min.Z);
            yield return new Vector3(min.X, max.Y, max.Z);
            yield return new Vector3(max.X, min.Y, min.Z);
            yield return new Vector3(max.X, min.Y, max.Z);
            yield return new Vector3(max.X, max.Y, min.Z);
            yield return max;
        }
        
        public static Bounds WithCenter(this Bounds bounds, Vector3 center) =>
            new Bounds(center: center, size: bounds.Size);

        public static Bounds WithCenter(this Bounds bounds, Func<Vector3, Vector3> centerGetter) =>
            bounds.WithCenter(centerGetter(bounds.Center));

        public static Bounds WithSize(this Bounds bounds, Vector3 size) =>
            new Bounds(center: bounds.Center, size: size);

        public static Bounds WithSize(this Bounds bounds, Func<Vector3, Vector3> sizeGetter) =>
            bounds.WithSize(sizeGetter(bounds.Size));
    }
}