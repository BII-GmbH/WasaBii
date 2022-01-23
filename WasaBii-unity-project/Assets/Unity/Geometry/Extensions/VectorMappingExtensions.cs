using System;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class VectorMappingExtensions {
        
#region Mapping
    #region 2D

        /// <summary>
        /// Returns a new vector with specific coordinates changed to new values.
        /// This method is intended to be used with named parameters.
        /// </summary>
        /// <example>
        /// var foo = transform.position.With(x: 2);
        /// </example>
        /// <returns>A new vector with it's coordinates set to everything specified that is not null.</returns>
        [Pure]
        public static Vector2 With(this Vector2 vec, float? x = null, float? y = null, float? z = null) {
            if (x != null) vec.x = x.Value;
            if (y != null) vec.y = y.Value;
            return vec; // a copy, since Vector2 is a struct.
        }

        /// <returns>A new vector with it's x coordinate set to the specified value.</returns>
        [Pure]
        public static Vector2 WithX(this Vector2 vec, float x) => new(x, vec.y);

        /// <returns>A new vector with it's y coordinate set to the specified value.</returns>
        [Pure]
        public static Vector2 WithY(this Vector2 vec, float y) => new(vec.x, y);

        /// <returns>A new vector with it's x coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector2 WithX(this Vector2 vec, [NotNull] Func<float, float> fx) => new(fx(vec.x), vec.y);

        /// <returns>A new vector with it's y coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector2 WithY(this Vector2 vec, [NotNull] Func<float, float> fy) => new(vec.x, fy(vec.y));

    #endregion 2D
    #region 3D
        
        /// <summary>
        /// Returns a new vector with specific coordinates changed to new values.
        /// This method is intended to be used with named parameters.
        /// </summary>
        /// <example>
        /// var foo = transform.position.With(x: 2, z: 3);
        /// </example>
        /// <returns>A new vector with it's coordinates set to everything specified that is not null.</returns>
        [Pure]
        public static Vector3 With(this Vector3 vec, float? x = null, float? y = null, float? z = null) {
            if (x != null) vec.x = x.Value;
            if (y != null) vec.y = y.Value;
            if (z != null) vec.z = z.Value;
            return vec; // a copy, since Vector3 is a struct.
        }

        /// <returns>A new vector with it's x coordinate set to the specified value.</returns>
        [Pure]
        public static Vector3 WithX(this Vector3 vec, float x) => new(x, vec.y, vec.z);

        /// <returns>A new vector with it's y coordinate set to the specified value.</returns>
        [Pure]
        public static Vector3 WithY(this Vector3 vec, float y) => new(vec.x, y, vec.z);

        /// <returns>A new vector with it's z coordinate set to the specified value.</returns>
        [Pure]
        public static Vector3 WithZ(this Vector3 vec, float z) => new(vec.x, vec.y, z);

        /// <returns>A new vector with it's x coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector3 WithX(this Vector3 vec, [NotNull] Func<float, float> fx) => new(fx(vec.x), vec.y, vec.z);

        /// <returns>A new vector with it's y coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector3 WithY(this Vector3 vec, [NotNull] Func<float, float> fy) => new(vec.x, fy(vec.y), vec.z);

        /// <returns>A new vector with it's z coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector3 WithZ(this Vector3 vec, [NotNull] Func<float, float> fz) => new(vec.x, vec.y, fz(vec.z));

    #endregion 3D
    #region 4D

        /// <summary>
        /// Returns a new vector with specific coordinates changed to new values.
        /// This method is intended to be used with named parameters.
        /// </summary>
        /// <example>
        /// var foo = transform.position.With(x: 2, z: 3, w: 1);
        /// </example>
        /// <returns>A new vector with it's coordinates set to everything specified that is not null.</returns>
        [Pure]
        public static Vector4 With(
            this Vector4 vec, float? x = null, float? y = null, float? z = null, float? w = null
        ) {
            if (x != null) vec.x = x.Value;
            if (y != null) vec.y = y.Value;
            if (z != null) vec.z = z.Value;
            if (w != null) vec.w = w.Value;
            return vec; // a copy, since Vector3 is a struct.
        }

        /// <returns>A new vector with it's x coordinate set to the specified value.</returns>
        [Pure]
        public static Vector4 WithX(this Vector4 vec, float x) => new(x, vec.y, vec.z, vec.w);

        /// <returns>A new vector with it's y coordinate set to the specified value.</returns>
        [Pure]
        public static Vector4 WithY(this Vector4 vec, float y) => new(vec.x, y, vec.z, vec.w);

        /// <returns>A new vector with it's z coordinate set to the specified value.</returns>
        [Pure]
        public static Vector4 WithZ(this Vector4 vec, float z) => new(vec.x, vec.y, z, vec.w);

        /// <returns>A new vector with it's w coordinate set to the specified value.</returns>
        [Pure]
        public static Vector4 WithW(this Vector4 vec, float w) => new(vec.x, vec.y, vec.z, w);

        /// <returns>A new vector with it's x coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector4 WithX(this Vector4 vec, [NotNull] Func<float, float> fx) => new(fx(vec.x), vec.y, vec.z, vec.w);

        /// <returns>A new vector with it's y coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector4 WithY(this Vector4 vec, [NotNull] Func<float, float> fy) => new(vec.x, fy(vec.y), vec.z, vec.w);

        /// <returns>A new vector with it's z coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector4 WithZ(this Vector4 vec, [NotNull] Func<float, float> fz) => new(vec.x, vec.y, fz(vec.z), vec.w);

        /// <returns>A new vector with it's z coordinate set to the result
        /// of applying the specified function to the original value.</returns>
        [Pure]
        public static Vector4 WithW(this Vector4 vec, [NotNull] Func<float, float> fw) => new(vec.x, vec.y, vec.z, fw(vec.w));

    #endregion 4D
#endregion Mapping
#region Expansion

        /// Expands the vector to a 3D one by appending <see cref="z"/> as the last value.
        [Pure]
        public static Vector3 WithZ(this Vector2 xy, float z) => new(xy.x, xy.y, z);

        /// Expands the vector to a 3D one by inserting <see cref="y"/> in between the
        /// existing values, transforming the original y component to the z dimension.
        [Pure]
        public static Vector3 AsXZWithY(this Vector2 xz, float y) => new(xz.x, y, xz.y);

        /// Expands the vector to a 4D one by appending <see cref="w"/> as the last value.
        [Pure]
        public static Vector4 WithW(this Vector3 xyz, float w) => new(xyz.x, xyz.y, xyz.z, w);

#endregion Expansion
#region Deconstruction
     
    [Pure]
    public static void Deconstruct(this Vector2 v, out float x, out float y) => (x, y) = (v.x, v.y);
    
    [Pure]
    public static void Deconstruct(this Vector3 v, out float x, out float y, out float z) => (x, y, z) = (v.x, v.y, v.z);
       
    [Pure]
    public static void Deconstruct(this Vector4 v, out float x, out float y, out float z, out float w) => (x, y, z, w) = (v.x, v.y, v.z, v.w);

#endregion Deconstruction

    }
}