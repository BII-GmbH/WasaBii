using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a world-space position.
    /// </summary>
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, memberType: nameof(Length), convertToMemberType: nameof(LengthConstructionExtensions.Meters), hasOrientation: false)]
    public partial struct GlobalPosition : IsGlobalVariant<GlobalPosition, LocalPosition> {
        
        public static readonly GlobalPosition Zero = new(System.Numerics.Vector3.Zero);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public GlobalPosition(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public GlobalPosition(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public GlobalPosition(float x, float y, float z) => AsUnityVector = new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public GlobalPosition(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public GlobalPosition(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public GlobalPosition(Length x, Length y, Length z) : this((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()) {}

        /// <summary>
        /// <inheritdoc cref="TransformProvider.InverseTransformPoint"/>
        /// This is the inverse of <see cref="LocalPosition.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalPosition RelativeTo(TransformProvider parent) 
            => parent.InverseTransformPoint(this);

        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => new(-pos.AsNumericsVector);
        
        [Pure] public Length DistanceTo(GlobalPosition p2) => (p2 - this).Magnitude;
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(System.Numerics.Vector3, System.Numerics.Vector3)"/>
        [Pure] public GlobalPosition PointReflect(GlobalPosition on)
            => on + (on - this);

        /// <inheritdoc cref="GeometryUtils.Reflect(System.Numerics.Vector3, System.Numerics.Vector3, System.Numerics.Vector3)"/>
        [Pure] public GlobalPosition Reflect(
            GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => pointOnPlane - (this - pointOnPlane).Reflect(planeNormal);
        
        [Pure] public GlobalPosition Rotate(
            GlobalPosition pivot, GlobalRotation rotation    
        ) => pivot + (this - pivot) * rotation;

    }
    
    public static partial class GlobalPositionExtensions {
        
        [Pure] public static GlobalPosition AsGlobalPosition(this System.Numerics.Vector3 globalPosition)
            => new(globalPosition);

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalPosition AsGlobalPosition(this UnityEngine.Vector3 globalPosition) 
            => new(globalPosition);

        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.Component component) 
            => new(component.transform.position);
        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.GameObject gameObject) 
            => new(gameObject.transform.position);
        
        #endif

    }
    
}