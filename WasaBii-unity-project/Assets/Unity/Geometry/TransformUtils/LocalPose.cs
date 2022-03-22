using System;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// <see cref="LocalPosition"/> and <see cref="LocalRotation"/> combined.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalPose : IsLocalVariant<LocalPose, GlobalPose>, IEquatable<LocalPose> {
        
        public static readonly LocalPose Identity = new LocalPose(Vector3.zero, Quaternion.identity);
        
        public readonly LocalPosition Position;
        public readonly LocalRotation Rotation;
        public LocalDirection Forward => Rotation * LocalDirection.Forward;

        public LocalPose(LocalPosition position, LocalRotation rotation) {
            Position = position;
            Rotation = rotation;
        }

        public LocalPose(Vector3 position, Quaternion rotation) :
            this(position.AsLocalPosition(), rotation.AsLocalRotation())
        { }

        public LocalPose(System.Numerics.Vector3 position, System.Numerics.Quaternion rotation) : 
            this(position.ToUnityVector(), rotation.ToUnityQuaternion()) {}

        public LocalPose(Transform transform) : this(transform.LocalPosition(), transform.LocalRotation()) { }

        public LocalPose(GlobalPose worldLocation, TransformProvider transform) : this(
            worldLocation.Position.RelativeTo(transform),
            worldLocation.Rotation.RelativeTo(transform)
        ) { }
        
        // Note DS: If `value` is a multitude of `Vector3.forward`, `Quaternion.FromToRotation` has an
        // ambiguous result which can lead to unexpected rotations, e.g. upside-down rails.
        // In this case, `LookRotation` yields a better result. However, this approach has
        // similar issues if `value` is a multitude of `Vector3.up`. In every other case,
        // the results are approximately equal. Thus, we switch between the two methods
        // depending on the angle between `value` and `forward`.

        public LocalPose(Vector3 position, Vector3 forward) : this(
            position,
            forward.normalized.Dot(Vector3.forward).Abs() > 0.5f
                ? Quaternion.LookRotation(forward)
                : Quaternion.FromToRotation(Vector3.forward, forward)
        ) { }

        public LocalPose(LocalPosition position, LocalDirection forward) : this(
            position.AsVector, forward.AsVector
        ) {}
        
        public LocalPose Inverse => new LocalPose(-Position, Rotation.Inverse());
        
        public static LocalPose operator +(LocalPose a, LocalPose b) => new LocalPose(
            a.Position + b.Position.AsOffset,
            b.Rotation * a.Rotation
        );
        
        public static LocalPose operator -(LocalPose a, LocalPose b) => new LocalPose(
            a.Position - b.Position.AsOffset,
            b.Rotation.Inverse() * a.Rotation
        );
        
        /// Transforms the local pose into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalPose.RelativeTo"/>
        [Pure] public GlobalPose ToGlobalWith(TransformProvider parent) => 
            new GlobalPose(Position.ToGlobalWith(parent), Rotation.ToGlobalWith(parent));
        
        /// This is another counterpart to <see cref="GlobalPose.RelativeTo"/> and
        /// <see cref="LocalPose.ToGlobalWith"/>. It returns the pose the parent must
        /// be in to transform this into <see cref="global"/>.
        /// <example>
        /// <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code>
        /// <code>global.RelativeTo(parent).ParentPoseFor(global) == parent</code>
        /// </example>
        [Pure] public GlobalPose ParentPoseFor(GlobalPose global) => Inverse.ToGlobalWith(global);

        [Pure] public bool Equals(LocalPose other) => Position.Equals(other.Position) && Rotation.Equals(other.Rotation);

        [Pure] public override bool Equals(object obj) => obj is LocalPose other && Equals(other);

        [Pure] public override int GetHashCode() {
            unchecked {
                return (Position.GetHashCode() * 397) ^ Rotation.GetHashCode();
            }
        }

        [Pure] public static bool operator ==(LocalPose left, LocalPose right) => left.Equals(right);
        [Pure] public static bool operator !=(LocalPose left, LocalPose right) => !left.Equals(right);

        [Pure] public LocalPose LerpTo(LocalPose target, double progress, bool shouldClamp) => new LocalPose(
            Position.LerpTo(target.Position, progress, shouldClamp),
            Rotation.LerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure] public LocalPose SlerpTo(LocalPose target, double progress, bool shouldClamp) => new LocalPose(
            Position.SlerpTo(target.Position, progress, shouldClamp),
            Rotation.SlerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure] public override string ToString() => "{" + Position + " | " + Rotation + "}";
    }

    public static partial class PoseUtils {
        
        [Pure] public static LocalPose WithPosition(this LocalPose source, Func<LocalPosition, LocalPosition> positionMapping) =>
            new LocalPose(positionMapping(source.Position), source.Rotation);
        
        [Pure] public static LocalPose WithRotation(this LocalPose source, Func<LocalRotation, LocalRotation> rotationMapping) =>
            new LocalPose(source.Position, rotationMapping(source.Rotation));
        
        [Pure] public static LocalPose WithForward(this LocalPose source, Func<LocalDirection, LocalDirection> forwardMapping) =>
            new LocalPose(source.Position, forwardMapping(source.Forward));
         
        [Pure] public static LocalPose ToLocalPoseWith(this LocalPosition pos, LocalRotation rot) =>
            new LocalPose(pos, rot);
        
        [Pure] public static LocalPose ToLocalPoseWith(this LocalRotation rot, LocalPosition pos) =>
            new LocalPose(pos, rot);

        [Pure] public static LocalPose LocalPose(this Component component) 
            => new LocalPose(component.transform);
        [Pure] public static LocalPose LocalPose(this GameObject gameObject) 
            => new LocalPose(gameObject.transform);

        [Pure] public static LocalPose LerpTo(this LocalPose start, LocalPose end, double perc, bool shouldClamp = true) =>
            new LocalPose(
                start.Position.LerpTo(end.Position, perc, shouldClamp),
                start.Rotation.LerpTo(end.Rotation, perc, shouldClamp)
            );
        
        [Pure] public static LocalPose SlerpTo(this LocalPose start, LocalPose end, double perc, bool shouldClamp = true) =>
            new LocalPose(
                start.Position.SlerpTo(end.Position, perc, shouldClamp),
                start.Rotation.SlerpTo(end.Rotation, perc, shouldClamp)
            );

        /// <inheritdoc cref="GeometryUtils.PointReflect(Vector3, Vector3)"/>
        [Pure] public static LocalPose Reflect(this LocalPose self, LocalPosition on) => new(
            position: self.Position.Reflect(on),
            forward: -self.Forward
        );

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3, Vector3)"/>
        [Pure] public static LocalPose Reflect(
            this LocalPose self, LocalPosition pointOnPlane, LocalDirection planeNormal
        ) => new(
            position: self.Position.Reflect(pointOnPlane, planeNormal),
            forward: self.Forward.Reflect(planeNormal)
        );
        
        [Pure] public static LocalPose Rotate(
            this LocalPose self, LocalPosition pivot, LocalRotation rotation    
        ) => new(
            position: self.Position.Rotate(pivot, rotation),
            rotation: rotation * self.Rotation
        );

    }

}