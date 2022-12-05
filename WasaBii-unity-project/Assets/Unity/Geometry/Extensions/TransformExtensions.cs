using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    public static class TransformExtensions {

        public static void Apply(this Transform t, Vector3 pos, Quaternion rot) {
            t.position = pos;
            t.rotation = rot;
        }

        public static void Apply(this Transform t, GlobalPosition pos, GlobalRotation rot) 
            => t.Apply(pos.AsNumericsVector, rot.AsQuaternion);

        public static void ApplyLocal(this Transform t, Vector3 pos, Quaternion rot) {
            t.localPosition = pos;
            t.localRotation = rot;
        }

        public static void ApplyLocal(this Transform t, LocalPosition pos, LocalRotation rot)
            => t.ApplyLocal(pos.AsVector, rot.AsQuaternion);

        public static void Apply(this Transform t, GlobalPose worldLocation)
            => t.Apply(worldLocation.Position, worldLocation.Rotation);

        public static void ApplyLocal(this Transform t, LocalPose localLocation)
            => t.ApplyLocal(localLocation.Position, localLocation.Rotation);

        public static void Apply(this Transform t, (Vector3 Position, Vector3 Tangent) positionTangentPair) {
            t.position = positionTangentPair.Position;
            t.forward = positionTangentPair.Tangent;
        }

        public static void Apply(this Transform t, Matrix4x4 transformMatrix) {
            t.position = transformMatrix.ExtractPosition();
            t.rotation = transformMatrix.ExtractRotation();
            t.SetLossyScale(transformMatrix.ExtractScale());
            transformMatrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale).inverse * transformMatrix;
            if (transformMatrix.m00.IsNearly(-1, 0.01f)) {
                t.SetLossyScale(t.lossyScale.WithX(x => x * transformMatrix.m00));
            }
        }

        public static void ApplyLocal(this Transform t, Matrix4x4 transformMatrix) {
            t.localPosition = transformMatrix.ExtractPosition();
            t.localRotation = transformMatrix.ExtractRotation();
            t.localScale = transformMatrix.ExtractScale();
            transformMatrix = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale).inverse * transformMatrix;
            if (transformMatrix.m00.IsNearly(-1, 0.01f)) {
                t.localScale = t.localScale.WithX(x => x * transformMatrix.m00);
            }
        }

        public static void ResetGlobals(this Transform t) {
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.SetLossyScale(Vector3.one);
        }

        public static void ResetLocals(this Transform t) {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }


        public static GlobalPose Location(this Transform t) => new GlobalPose(t.position, t.rotation);
        public static LocalPose LocalLocation(this Transform t) => new LocalPose(t.localPosition, t.localRotation);

        public static void SetLossyScale(this Transform t, Vector3 lossyScale) {
            t.localScale = lossyScale.CombineWith(t.lossyScale.Map(f => 1 / f), (a, b) => a * b);
        }

        public static Matrix4x4 LocalToParentMatrix(this Transform t) =>
            Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);

        /// Traverses depth-first.
        public static IEnumerable<Transform> GetAllChildrenRecursive(this Transform transform, bool includeSelf = false)
            => transform.GetChildren().SelectMany(c => c.GetAllChildrenRecursive(includeSelf: true))
                .If(includeSelf, children => children.Prepend(transform));
    }

}
