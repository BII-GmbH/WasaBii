using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    /// <summary>
    /// This struct provides implicit conversions for all supported types that can be used as parents.
    /// It is designed to reduce the level of complexity in this file, eliminating the need to implement
    /// an overload per supported type for all further functions.
    /// For example, the functions `Position.AsLocalPositionOf` and `Offset.AsLocalOffsetOf` take two
    /// parents each, thus requiring n² implementations for n parent types. With the implicit conversion,
    /// one implementation suffices without reducing the possibilities of usage.
    /// Also, it provides a central and maintainable way to manage all supported types.
    /// </summary>
    public readonly struct TransformProvider {
        public readonly Matrix4x4 LocalToGlobalMatrix;
        public Matrix4x4 GlobalToLocalMatrix => LocalToGlobalMatrix.inverse;
        
        private TransformProvider(Matrix4x4 localToGlobalMatrix) 
            => this.LocalToGlobalMatrix = localToGlobalMatrix;
        private TransformProvider(Vector3 position, Quaternion rotation, Vector3 scale) 
            => LocalToGlobalMatrix = Matrix4x4.TRS(position, rotation, scale);
        
        /// <summary>
        /// Transforms a position from local space to global space
        /// </summary>
        public Vector3 TransformPoint(Vector3 local) 
            => LocalToGlobalMatrix * new Vector4(local.x, local.y, local.z, 1);
        /// <summary>
        /// Transforms a position from global space to local space
        /// </summary>
        public Vector3 InverseTransformPoint(Vector3 global) 
            => GlobalToLocalMatrix * new Vector4(global.x, global.y, global.z, 1);

        /// <summary>
        /// Transforms a vector from local space to global space
        /// </summary>
        public Vector3 TransformVector(Vector3 local) 
            => LocalToGlobalMatrix * new Vector4(local.x, local.y, local.z, 0);
        /// <summary>
        /// Transforms a vector from global to local space
        /// </summary>
        public Vector3 InverseTransformVector(Vector3 global) 
            => GlobalToLocalMatrix * new Vector4(global.x, global.y, global.z, 0);

        /// <summary>
        /// Transforms a quaternion from local space to global space
        /// </summary>
        public Quaternion TransformQuaternion(Quaternion local)
            => LocalToGlobalMatrix.rotation * local;

        /// <summary>
        /// Transforms a quaternion from global to local space
        /// </summary>
        public Quaternion InverseTransformQuaternion(Quaternion global)
            => GlobalToLocalMatrix.rotation * global;

        public static TransformProvider From(Vector3? pos = null, Quaternion? rotation = null, Vector3? scale = null)
            => new TransformProvider(pos ?? Vector3.zero, rotation ?? Quaternion.identity, scale ?? Vector3.one);
        
        public static implicit operator TransformProvider(Component component)
            => new TransformProvider(component.transform.localToWorldMatrix);
        public static implicit operator TransformProvider(GameObject gameObject)
            => new TransformProvider(gameObject.transform.localToWorldMatrix);
        public static implicit operator TransformProvider(Matrix4x4 localToGlobalMatrix)
            => new TransformProvider(localToGlobalMatrix);
    }

}