using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a world-space position.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: false)]
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

        public LocalPosition RelativeToWorldZero => new(AsNumericsVector);

        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => new(-pos.AsNumericsVector);
        [Pure] public override string ToString() => AsNumericsVector.ToString();
        
        [Pure] public Length DistanceTo(GlobalPosition p2) => (p2 - this).Magnitude;
        
        /// <summary>
        /// Reflects this point off <see cref="on"/>. Has the same effect
        /// as rotating <see cref="this"/> around <see cref="on"/> by 180° around an axis
        /// perpendicular to the difference between the two.
        /// </summary>
        [Pure] public GlobalPosition PointReflect(GlobalPosition on)
            => on + (on - this);

        /// <summary>
        /// Reflects this vector on the plane defined by the <see cref="planeNormal"/> and a <see cref="pointOnPlane"/>.
        /// </summary>
        /// <param name="pointOnPlane">A point on the plane.</param>
        /// <param name="planeNormal">The normal of the plane.</param>
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