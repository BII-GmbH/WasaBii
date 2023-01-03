using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {
    
    /// <see cref="GlobalPosition"/> and <see cref="GlobalRotation"/> combined.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent:true, hasMagnitude:false, hasOrientation:false)]
    public partial struct GlobalPose : IsGlobalVariant<GlobalPose, LocalPose> {
        
        public static readonly GlobalPose Identity = new(GlobalPosition.Zero, GlobalRotation.Identity);

        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        #endif
        private GlobalPosition _position;
        
        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        #endif
        private GlobalRotation _rotation;
        
        public readonly GlobalPosition Position => _position;
        public readonly GlobalRotation Rotation => _rotation;
        public readonly GlobalDirection Forward => Rotation * GlobalDirection.Forward;

        public GlobalPose(GlobalPosition position, GlobalRotation rotation) => (_position, _rotation) = (position, rotation);

        public GlobalPose(GlobalPosition position, GlobalDirection forward) : this(
            position,
            GlobalRotation.From(GlobalDirection.Forward).To(forward)
        ) { }

        /// Transforms the global pose into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalPose.ToGlobalWith"/>
        [Pure] public LocalPose RelativeTo(TransformProvider parent) => new(Position.RelativeTo(parent), Rotation.RelativeTo(parent));

        public LocalPose RelativeToWorldZero => new(Position.RelativeToWorldZero, Rotation.RelativeToWorldZero);

        [Pure] public void Deconstruct(out GlobalPosition position, out GlobalRotation rotation) =>
            (position, rotation) = (Position, Rotation);

        [Pure] public override string ToString() => $"{{{Position} | {Rotation}}}";

        [Pure] public GlobalPose LerpTo(GlobalPose target, double progress, bool shouldClamp = true) => new(
            Position.LerpTo(target.Position, progress, shouldClamp),
            Rotation.SlerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure]
        public static GlobalPose Lerp(GlobalPose from, GlobalPose to, double progress, bool shouldClamp = true) =>
            from.LerpTo(to, progress, shouldClamp);
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(System.Numerics.Vector3,System.Numerics.Vector3)"/>
        [Pure] public GlobalPose PointReflect(GlobalPosition on) => new(
            position: Position.PointReflect(on),
            forward: -Forward
        );

        /// <inheritdoc cref="GeometryUtils.Reflect(System.Numerics.Vector3,System.Numerics.Vector3,System.Numerics.Vector3)"/>
        [Pure] public GlobalPose Reflect(
            GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => new(
            position: Position.Reflect(pointOnPlane, planeNormal),
            forward: Forward.Reflect(planeNormal)
        );
        
        [Pure] public GlobalPose Rotate(
            GlobalPosition pivot, GlobalRotation rotation    
        ) => new(
            position: Position.Rotate(pivot, rotation),
            rotation: rotation * Rotation
        );

    }

    public static partial class GlobalPoseExtensions {
        
        [Pure] public static GlobalPose ToGlobalPose(this (GlobalPosition position, GlobalRotation rotation) tuple) => 
            new(tuple.position, tuple.rotation);

        [Pure] public static GlobalPose ToGlobalPoseWith(this GlobalPosition pos, GlobalRotation rot) =>
            new(pos, rot);
        
        [Pure] public static GlobalPose ToGlobalPoseWith(this GlobalRotation rot, GlobalPosition pos) =>
            new(pos, rot);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalPose GlobalPose(this UnityEngine.Component component) 
            => new(component.GlobalPosition(), component.GlobalRotation());
        [Pure] public static GlobalPose GlobalPose(this UnityEngine.GameObject gameObject) 
            => new(gameObject.GlobalPosition(), gameObject.GlobalRotation());
        #endif

    }
}