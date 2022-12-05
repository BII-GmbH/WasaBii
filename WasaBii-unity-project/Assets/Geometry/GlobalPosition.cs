using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a world-space position.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalPosition : IsGlobalVariant<GlobalPosition, LocalPosition> {
        
        public static readonly GlobalPosition Zero = FromGlobal(Length.Zero, Length.Zero, Length.Zero);

        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }
        
        [Pure] public static GlobalPosition FromGlobal(System.Numerics.Vector3 global)
            => new() {X = global.X.Meters(), Y = global.Y.Meters(), Z = global.Z.Meters()};

        [Pure] public static GlobalPosition FromGlobal(Length x, Length y, Length z) 
            => new() {X = x, Y = y, Z = z};

        [Pure] public static GlobalPosition FromGlobal(double x, double y, double z) 
            => new() {X = x.Meters(), Y = y.Meters(), Z = z.Meters()};

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalPosition FromGlobal(UnityEngine.Vector3 global)
            => new() {X = global.x.Meters(), Y = global.y.Meters(), Z = global.z.Meters()};
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformPoint"/>
        /// This is the inverse of <see cref="LocalPosition.ToGlobalWith"/>
        [Pure] public LocalPosition RelativeTo(TransformProvider parent) 
            => parent.InverseTransformPoint(this);

        public LocalPosition RelativeToWorldZero => new() {X = X, Y = Y, Z = Z};

        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => FromGlobal(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => FromGlobal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => GlobalOffset.FromGlobal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => FromGlobal(-pos.X, -pos.Y, -pos.Z);
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
            => Geometry.GlobalPosition.FromGlobal(globalPosition);

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalPosition AsGlobalPosition(this UnityEngine.Vector3 globalPosition) 
            => Geometry.GlobalPosition.FromGlobal(globalPosition);

        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.Component component) 
            => Geometry.GlobalPosition.FromGlobal(component.transform.position);
        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.GameObject gameObject) 
            => Geometry.GlobalPosition.FromGlobal(gameObject.transform.position);
        
        #endif

    }
    
}