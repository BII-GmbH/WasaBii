using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {

    public static class SystemVectorExtensions {

        public static Vector3 WithX(this Vector3 v, float x) =>
            new Vector3(x, v.Y, v.Z);
        
        public static Vector3 WithX(this Vector3 v, Func<float, float> mapping) =>
            new Vector3(mapping(v.X), v.Y, v.Z);

        public static Vector3 WithY(this Vector3 v, float y) => 
            new Vector3(v.X, y, v.Z);
        
        public static Vector3 WithY(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, mapping(v.Y), v.Z);

        public static Vector3 WithZ(this Vector3 v, float z) => 
            new Vector3(v.X, v.Y, z);
        
        public static Vector3 WithZ(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(v.X, v.Y, mapping(v.Z));

        public static Vector3 Map(this Vector3 v, Func<float, float> mapping) => 
            new Vector3(mapping(v.X), mapping(v.Y), mapping(v.Z));

        public static float DistanceTo(this Vector3 v1, Vector3 v2)
            => Vector3.Distance(v1, v2);

        public static Vector3 Normalized(this Vector3 v) => Vector3.Normalize(v);

        public static Angle AngleTo(this Vector3 a, Vector3 b) => Angles.Acos(Vector3.Dot(a, b) / ((double) a.Length() * b.Length()));

        public static Vector3 Min(this Vector3 a, Vector3 b) => new(
            MathF.Min(a.X, b.X),
            MathF.Min(a.Y, b.Y),
            MathF.Min(a.Z, b.Z)
        );

        public static Vector3 Max(this Vector3 a, Vector3 b) => new(
            MathF.Max(a.X, b.X),
            MathF.Max(a.Y, b.Y),
            MathF.Max(a.Z, b.Z)
        );
        
        public static bool IsNearly(this Vector3 self, Vector3 other, double threshold = 1E-06) =>
            self.X.IsNearly(other.X, (float)threshold)
            && self.Y.IsNearly(other.Y, (float)threshold)
            && self.Z.IsNearly(other.Z, (float)threshold);

        public static Vector3 Sum(this IEnumerable<Vector3> vectors) => vectors.Aggregate((l, r) => l + r);

        public static Vector3 Average(this IEnumerable<Vector3> vectors) =>
            vectors.Average(Vector3.Add, (accum, count) => accum / count);

        public static Vector3 LerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            Vector3.Lerp(from, to, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));
        
        /// <summary>
        /// Spherical linear interpolation from <paramref name="from"/> to <paramref name="to"/>. This is the same
        /// as rotating one vector to the other while simultaneously lerping the length.
        /// </summary>
        /// <param name="from">The start. Will be returned at <paramref name="progress"/> = 0.</param>
        /// <param name="to">The end. Will be returned at <paramref name="progress"/> = 1.</param>
        /// <param name="progress">The amount of interpolation requested.</param>
        /// <param name="shouldClamp">Defines how to handle <paramref name="progress"/> values outside the (0-1)
        /// range. If this is false, values lower than 0 or greater than 1 will result in extrapolation. Otherwise,
        /// all values outside this range will be clamped to 0 or 1 such that only true interpolation can occur.</param>
        /// <param name="axisIfOpposite">In case the two vectors point in exactly opposite directions, the rotation should
        /// be 180°, but the axis is undefined and must be chosen perpendicular to the vectors. **Must be normalized!**</param>
        // https://en.wikipedia.org/wiki/Slerp
        public static Vector3 SlerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true, Func<Vector3, Vector3>? axisIfOpposite = null) {
            var fromLen = from.Length();
            var toLen = to.Length();

            var fromNormalized = from / fromLen;
            var toNormalized = to / toLen;
            
            var dot = Vector3.Dot(fromNormalized, toNormalized);
            var theta = Angles.Acos(dot);
            if (shouldClamp) progress = Math.Clamp(progress, 0, 1);
            return dot switch {
                // vectors are parallel, no rotation
                >= 0.999f => from.LerpTo(to, progress, shouldClamp: false), // Already clamped if desired
                // vectors are opposite, rotate 180° around any axis
                <= -0.999f => (float)MathD.Lerp(fromLen, toLen, progress) * Vector3.Transform(
                    fromNormalized, 
                    Quaternion.CreateFromAxisAngle(
                        axisIfOpposite?.Invoke(from) ?? calcPerpendicularAxis(from), 
                        (float) (progress * Math.PI)
                    )
                ),
                _ => (
                    (float)((1 - progress) * theta).Sin() * fromNormalized
                     + (float)(progress * theta).Sin() * toNormalized
                ) * (float) (
                    (progress * toLen + fromLen * (1 - progress)) // Lerp length
                    / theta.Sin())
            };
        }

        /// <summary>
        /// Calculates the angle between the two vectors when projected onto the plane defined by its
        /// <paramref name="normal"/>. Will be negative if the angular difference's orientation opposes
        /// the coordinate system's orientation. See <paramref name="handedness"/> for more information.
        /// </summary>
        /// <param name="from">The vector from which the angle diff is measured.
        /// Might not lie on the plane. Must not be perpendicular to the plane!</param>
        /// <param name="to">The vector to which the angle diff is measured.
        /// Might not lie on the plane. Must not be perpendicular to the plane!</param>
        /// <param name="normal">The plane's normal vector. Must be normalized!</param>
        /// <param name="handedness">Whether to use a left- or right-handed coordinate system. Flipping this will
        /// negate the result.</param>
        /// <remarks> Do not confuse this method with <see cref="SignedAngleTo"/>,
        /// which does not project the vectors onto the plane!</remarks>
        public static Angle SignedAngleOnPlaneTo(this Vector3 from, Vector3 to, Vector3 normal, Handedness handedness = Handedness.Default) {
            from -= Vector3.Dot(from, normal) * normal;
            to -= Vector3.Dot(to, normal) * normal;
            return Angles.Atan2(
                Vector3.Dot(
                    handedness switch {
                        Handedness.Left => Vector3.Cross(from, to),
                        Handedness.Right => Vector3.Cross(to, from),
                        _ => throw new InvalidEnumArgumentException(nameof(handedness), (int)handedness, typeof(Handedness))
                    }, 
                    normal
                ),
                Vector3.Dot(from, to)
            );
        }
        
        /// <summary>
        /// Calculates the angle between the two vectors. Will be negative if the angular difference's orientation
        /// opposes the coordinate system's orientation when projected onto the plane defined by its
        /// <paramref name="normal"/>. See <paramref name="handedness"/> for more information.
        /// </summary>
        /// <param name="from">The vector from which the angle diff is measured.</param>
        /// <param name="to">The vector to which the angle diff is measured.</param>
        /// <param name="normal">The plane's normal vector. Must be normalized!</param>
        /// <param name="handedness">Whether to use a left- or right-handed coordinate system. Flipping this will
        /// negate the result.</param>
        /// <remarks> This is equivalent to Unity's <see cref="UnityEngine.Vector3.SignedAngle"/>. Do not confuse this
        /// with <see cref="SignedAngleOnPlaneTo"/>, which projects the vectors onto the plane!</remarks>
        public static Angle SignedAngleTo(this Vector3 from, Vector3 to, Vector3 normal, Handedness handedness = Handedness.Default) {
            var unsignedAngle = from.AngleTo(to);
            var axis = handedness switch {
                Handedness.Left => Vector3.Cross(from, to),
                Handedness.Right => Vector3.Cross(to, from),
                _ => throw new InvalidEnumArgumentException(nameof(handedness), (int)handedness, typeof(Handedness))
            };
            var sign = axis.IsNearly(Vector3.Zero) // Vectors are parallel or opposite, sign is arbitrary
                ? 1
                : MathF.Sign(Vector3.Dot(axis, normal));
            return unsignedAngle * sign;
        }

        /// <summary>
        /// Returns a quaternion <c>q</c> such that Vector3.Transform(<paramref name="from"/>, <c>q</c>) == <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The vector to rotate to <paramref name="to"/>. **Must be normalized!**</param>
        /// <param name="to">The vector to which <paramref name="from"/> should be rotated. **Must be normalized!**</param>
        /// <param name="axisIfOpposite">In case the two vectors point in exactly opposite directions, the rotation should
        /// be 180°, but the axis is undefined and must be chosen perpendicular to the vectors. **Must be normalized!**</param>
        public static Quaternion RotationTo(this Vector3 from, Vector3 to, Func<Vector3, Vector3>? axisIfOpposite = null) {
            // https://stackoverflow.com/a/1171995
            var normal = Vector3.Cross(from, to);
            var dot = Vector3.Dot(from, to);
            return dot <= -0.999f
                // vectors are opposite, rotate 180° around any axis
                ? Quaternion.CreateFromAxisAngle(axisIfOpposite?.Invoke(from) ?? calcPerpendicularAxis(from), MathF.PI)
                : Quaternion.Normalize(new Quaternion(normal, 1 + dot));
        }

        /// The default way of calculating an axis perpendicular to <paramref name="vec"/> if no override is given.
        /// Designed such that it returns the Y-Axis (0,1,0) or its inverse (0,-1,0) if that is a valid option.
        private static Vector3 calcPerpendicularAxis(Vector3 vec) {
            // Calculate the axis by taking the cross product with the input and a unit
            // vector. Assuming that this unit vector is not parallel to the input, the
            // result is guaranteed to be perpendicular to them both. By only considering
            // the X-Axis and the Z-Axis, the result will be the Y-Axis if the input is
            // perpendicular to it. We choose the axis with the lesser absolute dot product
            // with the input, i.e. the one that is "more perpendicular" to avoid numerical
            // instability that could occur when accidentally choosing one that is (almost)
            // parallel to the input.
            var unitRef = MoreLinqExtensions.MinBy(
                new[] { Vector3.UnitX, Vector3.UnitZ },
                unit => Vector3.Dot(unit, vec).Abs()
            ).GetOrThrow();
            return Vector3.Cross(unitRef, vec).Normalized();
        }
    }

}