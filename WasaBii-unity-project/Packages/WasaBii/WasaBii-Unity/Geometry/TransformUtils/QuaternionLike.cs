using System;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class QuaternionLikeExtensions {
        
        [Pure] public static Quaternion Inverse(this Quaternion q) => Quaternion.Inverse(q);
        
        [Pure] public static Angle AngleOn(this Quaternion q, Vector3 axis) {
            // An arbitrary vector that is orthogonal to `axis`.
            // Taken from https://math.stackexchange.com/a/3077100
            // Prove: 
            // axis dot vec = axis.x * (axis.y + axis.z) + axis.y * (axis.z - axis.x) + axis.z * (-axis.x - axis.y)
            //     = x*y-y*x + x*z-z*x + y*z-y*z
            //     = 0
            var vec = new Vector3(axis.y + axis.z, axis.z - axis.x, -axis.x - axis.y);
            return Vector3.SignedAngle(vec, q * vec, axis).Degrees();
        }

        // [Pure] public static Angle AngleTo<T>(this T self, T other) 
        // where T : struct, QuaternionLike<T> =>
        //     Quaternion.Angle(self.AsQuaternion, other.AsQuaternion).Degrees();
        //
        // [Pure] public static T Inverse<T>(this T t) where T : struct, QuaternionLike<T> => 
        //     t.CopyWithDifferentValue(t.AsQuaternion.Inverse());

    }

}