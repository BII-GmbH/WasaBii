using System;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    public interface QuaternionLike {
        Quaternion AsQuaternion { get; }
    }
    
    /// Supertype for all transform utils that essentially wrap a quaternion.
    public interface QuaternionLike<TSelf> : QuaternionLike, GeometryHelper<TSelf>
    where TSelf: struct, QuaternionLike<TSelf> {
        
        /// Constructs a new instance of this type with the given value.
        /// Needed by utilities like `Map`.
        TSelf CopyWithDifferentValue(Quaternion newValue);

        TSelf GeometryHelper<TSelf>.LerpTo(TSelf target, double progress, bool shouldClamp) =>
            CopyWithDifferentValue(shouldClamp 
                ? Quaternion.Lerp(AsQuaternion, target.AsQuaternion, (float)progress) 
                : Quaternion.LerpUnclamped(AsQuaternion, target.AsQuaternion, (float)progress)
            );

        TSelf GeometryHelper<TSelf>.SlerpTo(TSelf target, double progress, bool shouldClamp) =>
            CopyWithDifferentValue(shouldClamp 
                ? Quaternion.Slerp(AsQuaternion, target.AsQuaternion, (float)progress) 
                : Quaternion.SlerpUnclamped(AsQuaternion, target.AsQuaternion, (float)progress)
            );
    }
    
    public static class QuaternionLikeExtensions {
        
        [Pure] public static bool IsNearly<T>(this T left, T right, float equalityThreshold = 1E-30f) where T : struct, QuaternionLike =>
            left.AsQuaternion.IsNearly(right.AsQuaternion, equalityThreshold);

        [Pure]
        public static T Map<T>(this T t, Func<Quaternion, Quaternion> f) where T : struct, QuaternionLike<T>
            => t.CopyWithDifferentValue(f(t.AsQuaternion));

        [Pure] public static T LerpTo<T>(this T start, T end, double perc, bool shouldClamp = true)
            where T : struct, QuaternionLike<T> => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static T SlerpTo<T>(this T start, T end, double perc, bool shouldClamp = true) 
            where T : struct, QuaternionLike<T> => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public static Quaternion Inverse(this Quaternion q) => Quaternion.Inverse(q);
        
        [Pure] public static Units.Angle AngleOn(this Quaternion q, Vector3 axis) {
            // An arbitrary vector that is orthogonal to `axis`.
            // Taken from https://math.stackexchange.com/a/3077100
            // Prove: 
            // axis dot vec = axis.x * (axis.y + axis.z) + axis.y * (axis.z - axis.x) + axis.z * (-axis.x - axis.y)
            //     = x*y-y*x + x*z-z*x + y*z-y*z
            //     = 0
            var vec = new Vector3(axis.y + axis.z, axis.z - axis.x, -axis.x - axis.y);
            return Vector3.SignedAngle(vec, q * vec, axis).Degrees();
        }

        [Pure] public static Angle AngleTo<T>(this T self, T other) 
        where T : struct, QuaternionLike<T> =>
            Quaternion.Angle(self.AsQuaternion, other.AsQuaternion).Degrees();

        [Pure] public static T Inverse<T>(this T t) where T : struct, QuaternionLike<T> => 
            t.CopyWithDifferentValue(t.AsQuaternion.Inverse());

    }

}