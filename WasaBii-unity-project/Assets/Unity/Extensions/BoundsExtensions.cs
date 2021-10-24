using System;
using System.Linq;
using CoreLibrary;
using UnityEngine;
using static BII.WasaBii.Core.EnumerableExtensions;
using BIIBounds = BII.WasaBii.Core.Bounds;

namespace BII.Utilities.Unity {
    public static class BoundsExtensions {

        public static Vector3 Top(this Bounds bounds)
            => bounds.center.WithY(bounds.max.y);

        public static Vector3 Bottom(this Bounds bounds)
            => bounds.center.WithY(bounds.min.y);

        /// <summary>
        /// The rect you get when removing y from the bounds (the top face of the box).
        /// </summary>
        public static Rect TopRect(this Bounds bounds)
            => new Rect(bounds.min.xz(), bounds.size.xz());

        public static Bounds And(this Bounds a, Bounds b) {
            // `new Bounds(Vector3, Vector3)` takes `center` and `size`.
            // Not `min` and `max`.
            // The types are the same and swapping these can lead to several hours of debugging.
            // Every time.
            // And they say you learn from your mistakes.
            // Apparently I don't.
            a.SetMinMax(
                Vector3.Min(a.min, b.min),
                Vector3.Max(a.max, b.max)
            );
            return a;
        }
    
        public static Bounds WithCenter(this Bounds bounds, Vector3 center) =>
            new Bounds(center: center, size: bounds.size);

        public static Bounds WithCenter(this Bounds bounds, Func<Vector3, Vector3> centerGetter) =>
            bounds.WithCenter(centerGetter(bounds.center));

        public static Bounds WithSize(this Bounds bounds, Vector3 size) =>
            new Bounds(center: bounds.center, size: size);

        public static Bounds WithSize(this Bounds bounds, Func<Vector3, Vector3> sizeGetter) =>
            bounds.WithSize(sizeGetter(bounds.size));
        
        public static BIIBounds ToBIIBounds(this Bounds bounds) => new BIIBounds(
            new System.Numerics.Vector3(bounds.center.x, bounds.center.y, bounds.center.z),
            new System.Numerics.Vector3(bounds.size.x, bounds.size.y, bounds.size.z)
        );
        
        public static Bounds? TotalColliderBounds(this GameObject go) {
            Physics.SyncTransforms(); // Enforces all colliders to update so we can be sure the bounds are valid.
            var wasActive = go.activeSelf;
            if (!wasActive) go.SetActive(true);
            var ret = go.All<Collider>(Search.InChildren)
                .Select(cc => cc.bounds)
                .IfNotEmpty(
                    then: bounds => bounds.Aggregate(BoundsExtensions.And),
                    elseResult: (Bounds?) null
                );
            if(!wasActive) go.SetActive(false);
            return ret;
        }


    }
}
