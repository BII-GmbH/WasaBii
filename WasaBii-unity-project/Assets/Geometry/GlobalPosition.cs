using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a world-space position.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalPosition : IsGlobalVariant<GlobalPosition, LocalPosition> {
        
        public static readonly GlobalPosition Zero = new(Length.Zero, Length.Zero, Length.Zero);

        public readonly System.Numerics.Vector3 AsNumericsVector;

        public GlobalPosition(System.Numerics.Vector3 asNumericsVector) => AsNumericsVector = asNumericsVector;
        public GlobalPosition(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        public GlobalPosition(Length x, Length y, Length z) => AsNumericsVector = new((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters());

        #if UNITY_2022_1_OR_NEWER
        public GlobalPosition(UnityEngine.Vector3 global) => AsNumericsVector = global.ToSystemVector();
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformPoint"/>
        /// This is the inverse of <see cref="LocalPosition.ToGlobalWith"/>
        [Pure] public LocalPosition RelativeTo(TransformProvider parent) 
            => parent.InverseTransformPoint(this);

        public LocalPosition RelativeToWorldZero => new(AsNumericsVector);

        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => new(-pos.AsNumericsVector);
        [Pure] public override string ToString() => AsNumericsVector.ToString();
        
        [Pure] public static GlobalPosition Lerp(
            GlobalPosition start, GlobalPosition end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);

        [Pure] public Length DistanceTo(GlobalPosition p2) => (p2 - this).Magnitude;
        
        /// Reflects this point off <see cref="on"/>. Has the same effect
        /// as rotating <see cref="this"/> around <see cref="on"/> by 180° around an axis
        /// perpendicular to the difference between the two.
        [Pure] public GlobalPosition PointReflect(GlobalPosition on)
            => on + (on - this);

        /// Reflects this vector on the plane defined by the <see cref="planeNormal"/> and a <see cref="pointOnPlane"/>.
        /// <param name="pointOnPlane">A point on the plane.</param>
        /// <param name="planeNormal">The normal of the plane.</param>
        [Pure] public GlobalPosition Reflect(
            GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => pointOnPlane - (this - pointOnPlane).Reflect(planeNormal);
        
        [Pure] public GlobalPosition Rotate(
            GlobalPosition pivot, GlobalRotation rotation    
        ) => pivot + (this - pivot) * rotation;

    }
    
    public static partial class PositionExtensions {
        
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