using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    public static class TransformExtensions {

        public static void Apply(this Transform t, Vector3 pos, Quaternion rot) {
            t.position = pos;
            t.rotation = rot;
        }

        public static void Apply(this Transform t, GlobalPosition pos, GlobalRotation rot) 
            => t.Apply(pos.AsUnityVector, rot.AsUnityQuaternion);

        public static void ApplyLocal(this Transform t, Vector3 pos, Quaternion rot) {
            t.localPosition = pos;
            t.localRotation = rot;
        }

        public static void ApplyLocal(this Transform t, LocalPosition pos, LocalRotation rot)
            => t.ApplyLocal(pos.AsUnityVector, rot.AsUnityQuaternion);

        public static void Apply(this Transform t, GlobalPose worldLocation)
            => t.Apply(worldLocation.Position, worldLocation.Rotation);

        public static void ApplyLocal(this Transform t, LocalPose localLocation)
            => t.ApplyLocal(localLocation.Position, localLocation.Rotation);

        public static void Apply(this Transform t, (Vector3 Position, Vector3 Tangent) positionTangentPair) {
            t.position = positionTangentPair.Position;
            t.forward = positionTangentPair.Tangent;
        }

        public static void Apply(this Transform t, Matrix4x4 transformMatrix) => t.ApplyLocal(
            t.parent == null ? transformMatrix : t.parent.worldToLocalMatrix * transformMatrix
        );

        public static void ApplyLocal(this Transform t, Matrix4x4 transformMatrix) {
            t.localPosition = transformMatrix.GetPosition();
            t.localRotation = transformMatrix.rotation.normalized;
            t.localScale = (Matrix4x4.Rotate(t.localRotation.Inverse()) * transformMatrix).lossyScale;
        }

        public static void SetToGlobalZero(this Transform t) {
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.SetLossyScale(Vector3.one);
        }

        public static void SetToLocalZero(this Transform t) {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        
        public static void SetLossyScale(this Transform t, Vector3 lossyScale) {
            // m * localScaleMatrix = globalScaleMatrix * m
            // => localScaleMatrix = m.inverse * globalScaleMatrix * m
            var m = t.localToWorldMatrix;
            t.localScale = (m.inverse * Matrix4x4.Scale(lossyScale) * m).lossyScale;
        }

        public static Matrix4x4 LocalToParentMatrix(this Transform t) =>
            Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);

        /// Traverses depth-first.
        /// Lazy. Do not modify hierarchy while consuming
        public static IEnumerable<Transform> GetAllChildrenRecursive(this Transform transform, bool includeSelf = false)
            => transform.GetChildren().SelectMany(c => c.GetAllChildrenRecursive(includeSelf: true))
                .If(includeSelf, children => children.Prepend(transform));
    }

}
