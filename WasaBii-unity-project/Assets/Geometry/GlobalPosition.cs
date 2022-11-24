using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="System.Numerics.Vector3"/> that represents a world-space position.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalPosition : IsGlobalVariant<GlobalPosition, LocalPosition> {
        
        public static readonly GlobalPosition Zero = FromGlobal(Length.Zero, Length.Zero, Length.Zero);

        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }
        
        public System.Numerics.Vector3 AsVector => new((float)X.AsMeters(), (float)Y.AsMeters(), (float)Z.AsMeters());
        #if UNITY_2022_1_OR_NEWER
        public UnityEngine.Vector3 AsUnityVector => AsVector.ToUnityVector();

        #endif

        [Pure] public static GlobalPosition FromGlobal(PositionProvider global) => global.Wrapped;

        [Pure] public static GlobalPosition FromGlobal(Length x, Length y, Length z) 
            => new() {X = x, Y = y, Z = z};

        /// Transforms the global position into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalPosition.ToGlobalWith"/>
        [Pure] public LocalPosition RelativeTo(TransformProvider parent) 
            => parent.InverseTransformPoint(this);

        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => FromGlobal(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => FromGlobal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => GlobalOffset.FromGlobal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => FromGlobal(-pos.X, -pos.Y, -pos.Z);
        [Pure] public override string ToString() => AsVector.ToString();
        
        [Pure] public static GlobalPosition Lerp(
            GlobalPosition start, GlobalPosition end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);

        [Pure]
        public GlobalPosition SlerpTo(GlobalPosition end, double perc, bool shouldClamp = true) =>
            throw new NotImplementedException(); // TODO DS: this
        
        [Pure] public static GlobalPosition Slerp(
            GlobalPosition start, GlobalPosition end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

    }
    
    public static partial class PositionExtensions {
        
        [Pure] public static GlobalPosition AsGlobalPosition(this System.Numerics.Vector3 globalPosition)
            => Geometry.GlobalPosition.FromGlobal(globalPosition.X.Meters(), globalPosition.Y.Meters(), globalPosition.Z.Meters());

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalPosition AsGlobalPosition(this UnityEngine.Vector3 globalPosition) 
            => Geometry.GlobalPosition.FromGlobal(globalPosition.x.Meters(), globalPosition.y.Meters(), globalPosition.z.Meters());

        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.Component component) 
            => Geometry.GlobalPosition.FromGlobal(component.transform.position);
        [Pure] public static GlobalPosition GlobalPosition(this UnityEngine.GameObject gameObject) 
            => Geometry.GlobalPosition.FromGlobal(gameObject.transform.position);
        
        #endif

        [Pure] public static Length DistanceTo(this GlobalPosition p1, GlobalPosition p2) => (p2 - p1).Length;
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(UnityEngine.Vector3, UnityEngine.Vector3)"/>
        [Pure] public static GlobalPosition PointReflect(this GlobalPosition self, GlobalPosition on)
            => on + (on - self);

        /// <inheritdoc cref="GeometryUtils.Reflect(UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3)"/>
        [Pure] public static GlobalPosition Reflect(
            this GlobalPosition self, GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => pointOnPlane - (self - pointOnPlane).Reflect(planeNormal);
        
        [Pure] public static GlobalPosition Rotate(
            this GlobalPosition self, GlobalPosition pivot, GlobalRotation rotation    
        ) => pivot + (self - pivot) * rotation;

    }
    
}