﻿using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A global transformation without scale, which simply is a <see cref="LocalPosition"/> and <see cref="LocalRotation"/> combined.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: false, hasOrientation: false)]
    public partial struct LocalPose : IsLocalVariant<LocalPose, GlobalPose> {
        
        public static readonly LocalPose Identity = new(LocalPosition.Zero, LocalRotation.Identity);
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public LocalPosition Position { get; private set; }
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public LocalRotation Rotation { get; private set; }

        public readonly LocalDirection Forward => Rotation * LocalDirection.Forward;

        public LocalPose(LocalPosition position, LocalRotation rotation) => (Position, Rotation) = (position, rotation);

        public LocalPose(LocalPosition position, LocalDirection forward) : this(
            position,
            LocalRotation.From(LocalDirection.Forward).To(forward)
        ) { }

        public LocalPose Inverse {
            get {
                var invRot = Rotation.Inverse;
                return new LocalPose((-Position.AsOffset * invRot).AsPosition, invRot);
            }
        }

        /// <summary>
        /// Transforms the <paramref name="local"/> pose into the local space <paramref name="parent"/>
        /// is defined relative to. Only applicable if <see cref="local"/> is defined relative to the
        /// given <paramref name="parent"/>!
        /// </summary>
        public static LocalPose operator *(LocalPose parent, LocalPose local) => new(
            parent.Position + parent.Rotation * local.Position.AsOffset,
            parent.Rotation * local.Rotation
        );

        /// <summary>
        /// Combines the two poses by staking both the offset from local zero and the rotation independently.
        /// Only applicable if <paramref name="a"/> and <paramref name="b"/> are defined relative to the
        /// same parent!
        /// </summary>
        public static LocalPose operator +(LocalPose a, LocalPose b) => new(
            a.Position + b.Position.AsOffset,
            b.Rotation * a.Rotation
        );

        /// <summary>
        /// Transforms the local pose into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalPose.RelativeTo"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public GlobalPose ToGlobalWith(TransformProvider parent) => 
            new GlobalPose(Position.ToGlobalWith(parent), Rotation.ToGlobalWith(parent));

        public GlobalPose ToGlobalWithWorldZero => new(Position.ToGlobalWithWorldZero, Rotation.ToGlobalWithWorldZero);

        /// <summary>
        /// This is another counterpart to <see cref="GlobalPose.RelativeTo"/> and
        /// <see cref="LocalPose.ToGlobalWith"/>. It returns the pose the parent must
        /// be in to transform this into <see cref="global"/>.
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ParentPoseFor(global) == parent</code> </example>
        [Pure] public GlobalPose ParentPoseFor(GlobalPose global) => Inverse.ToGlobalWith(global);

        [Pure] public LocalPose TransformBy(LocalPose offset) => new(
            Position.TransformBy(offset),
            Rotation.TransformBy(offset)
        );
        
        [Pure] public LocalPose LerpTo(LocalPose target, double progress, bool shouldClamp) => new LocalPose(
            Position.LerpTo(target.Position, progress, shouldClamp),
            Rotation.SlerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure] public LocalPose SlerpTo(LocalPose target, double progress, bool shouldClamp) => new LocalPose(
            Position.AsOffset.SlerpTo(target.Position.AsOffset, progress, shouldClamp).AsPosition,
            Rotation.SlerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure] public override string ToString() => "{" + Position + " | " + Rotation + "}";
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(System.Numerics.Vector3,System.Numerics.Vector3)"/>
        [Pure] public LocalPose PointReflect(LocalPosition on) => new(
            position: Position.PointReflect(on),
            forward: -Forward
        );

        /// <inheritdoc cref="GeometryUtils.Reflect(System.Numerics.Vector3,System.Numerics.Vector3,System.Numerics.Vector3)"/>
        [Pure] public LocalPose Reflect(
            LocalPosition pointOnPlane, LocalDirection planeNormal
        ) => new(
            position: Position.Reflect(pointOnPlane, planeNormal),
            forward: Forward.Reflect(planeNormal)
        );
        
        [Pure] public LocalPose Rotate(
            LocalPosition pivot, LocalRotation rotation    
        ) => new(
            position: Position.Rotate(pivot, rotation),
            rotation: rotation * Rotation
        );

    }

    public static partial class LocalPoseExtensions {
        
        [Pure] public static LocalPose ToLocalPose(this (LocalPosition position, LocalRotation rotation) tuple) => 
            new(tuple.position, tuple.rotation);

        [Pure] public static LocalPose ToLocalPoseWith(this LocalPosition pos, LocalRotation rot) =>
            new(pos, rot);
        
        [Pure] public static LocalPose ToLocalPoseWith(this LocalRotation rot, LocalPosition pos) =>
            new(pos, rot);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalPose LocalPose(this UnityEngine.Component component) 
            => new(component.LocalPosition(), component.LocalRotation());
        [Pure] public static LocalPose LocalPose(this UnityEngine.GameObject gameObject) 
            => new(gameObject.LocalPosition(), gameObject.LocalRotation());
        #endif

    }

}