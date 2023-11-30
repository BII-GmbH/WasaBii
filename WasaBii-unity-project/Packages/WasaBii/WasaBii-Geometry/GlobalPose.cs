using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Geometry.Shared;

namespace BII.WasaBii.Geometry {
    
    /// <summary>
    /// A local transformation without scale, which simply is a <see cref="GlobalPosition"/> and <see cref="GlobalRotation"/> combined.
    /// </summary>
    [Serializable]
    [GeometryHelper(areFieldsIndependent:false, hasMagnitude:false, hasOrientation:false)]
    public partial struct GlobalPose : IsGlobalVariant<GlobalPose, LocalPose> {
        
        public static readonly GlobalPose Identity = new(GlobalPosition.Zero, GlobalRotation.Identity);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public GlobalPosition Position { get; private set; }
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public GlobalRotation Rotation { get; private set; }
        
        public readonly GlobalDirection Forward => Rotation * GlobalDirection.Forward;

        public GlobalPose(GlobalPosition position, GlobalRotation rotation) => (Position, Rotation) = (position, rotation);

        public GlobalPose(GlobalPosition position, GlobalDirection forward) : this(
            position,
            GlobalRotation.From(GlobalDirection.Forward).To(forward)
        ) { }

        /// <summary>
        /// Transforms the global pose into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalPose.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalPose RelativeTo(TransformProvider parent) => new(Position.RelativeTo(parent), Rotation.RelativeTo(parent));

        [Pure] public void Deconstruct(out GlobalPosition position, out GlobalRotation rotation) =>
            (position, rotation) = (Position, Rotation);

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