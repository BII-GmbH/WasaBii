﻿#if UNITY_2022_1_OR_NEWER

using System;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Geometry
{
    public static class UnityNumericsExtensions
    {
        public static bool IsNearly(this Vector3 self, Vector3 other, double threshold = 1E-06) =>
            self.x.IsNearly(other.x, (float)threshold)
            && self.y.IsNearly(other.y, (float)threshold)
            && self.z.IsNearly(other.z, (float)threshold);

        public static Vector3 LerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            Vector3.Lerp(from, to, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));

        public static Vector3 SlerpTo(this Vector3 from, Vector3 to, double progress, bool shouldClamp = true) =>
            Vector3.Slerp(from, to, (float)(shouldClamp ? Math.Clamp(progress, 0, 1) : progress));

        public static bool IsNearly(this Quaternion lhs, Quaternion rhs, double equalityThreshold = 1E-30f) => 
            lhs.x.IsNearly(rhs.x, (float)equalityThreshold) 
            && lhs.y.IsNearly(rhs.y, (float)equalityThreshold) 
            && lhs.z.IsNearly(rhs.z, (float)equalityThreshold)
            && lhs.w.IsNearly(rhs.w, (float)equalityThreshold);

        public static Quaternion SlerpTo(
            this Quaternion from, Quaternion to, double progress, bool shouldClamp = true
        ) => shouldClamp ? Quaternion.Slerp(from, to, (float)progress) : Quaternion.SlerpUnclamped(from, to, (float)progress);

        public static Vector3 Min(this Vector3 a, Vector3 b) => Vector3.Min(a, b);
        public static Vector3 Max(this Vector3 a, Vector3 b) => Vector3.Max(a, b);

    }
}
#endif