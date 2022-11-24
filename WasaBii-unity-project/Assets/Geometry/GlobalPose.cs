using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    /// <see cref="GlobalPosition"/> and <see cref="GlobalRotation"/> combined.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct GlobalPose : IsGlobalVariant<GlobalPose, LocalPose>, IEquatable<GlobalPose> {
        public static implicit operator TransformProvider(GlobalPose location)
            => TransformProvider.From(location.Position.AsVector, location.Rotation.AsQuaternion, Vector3.one);
        
        public static implicit operator PositionProvider(GlobalPose location)
            => new PositionProvider(location.Position);
        public static readonly GlobalPose Identity = new GlobalPose(Vector3.zero, Quaternion.identity);

        public readonly GlobalPosition Position;
        public readonly GlobalRotation Rotation;
        public readonly GlobalDirection Forward => Rotation * GlobalDirection.Forward;

        public Vector3 GlobalPosition => Position.AsVector;
        public Quaternion GlobalRotation => Rotation.AsQuaternion;
        public Vector3 GlobalForward => Forward.AsVector;

        public GlobalPose(GlobalPosition position, GlobalRotation rotation) => (Position, Rotation) = (position, rotation);

        public GlobalPose(Vector3 position, Quaternion rotation) : this(position.AsGlobalPosition(), rotation.AsGlobalRotation()) {}

        public GlobalPose(System.Numerics.Vector3 position, System.Numerics.Quaternion rotation)
            : this(position.ToUnityVector(), rotation.ToUnityQuaternion()) { }

        public GlobalPose(Transform transform) : this(transform.GlobalPosition(), transform.GlobalRotation()) { }

        // Note DS: If `value` is a multitude of `Vector3.forward`, `Quaternion.FromToRotation` has an
        // ambiguous result which can lead to unexpected rotations, e.g. upside-down rails.
        // In this case, `LookRotation` yields a better result. However, this approach has
        // similar issues if `value` is a multitude of `Vector3.up`. In every other case,
        // the results are approximately equal. Thus, we switch between the two methods
        // depending on the angle between `value` and `forward`.

        public GlobalPose(Vector3 position, Vector3 forward) : this(
            position,
            forward.normalized.Dot(Vector3.forward).Abs() > 0.5f
                ? Quaternion.LookRotation(forward)
                : Quaternion.FromToRotation(Vector3.forward, forward)
        ) { }

        public GlobalPose(GlobalPosition position, GlobalDirection forward) : this(position.AsVector, forward.AsVector) { }

        [Pure] public bool Equals(GlobalPose other) => Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
        [Pure] public override bool Equals(object obj) => obj is GlobalPose other && Equals(other);

        [Pure] public override int GetHashCode() {
            unchecked {
                return (Position.GetHashCode() * 397) ^ Rotation.GetHashCode();
            }
        }

        [Pure] public static bool operator ==(GlobalPose left, GlobalPose right) => left.Equals(right);
        [Pure] public static bool operator !=(GlobalPose left, GlobalPose right) => !left.Equals(right);

        /// Transforms the global pose into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalPose.ToGlobalWith"/>
        [Pure] public LocalPose RelativeTo(TransformProvider parent) => new LocalPose(this, parent);

        [Pure] public bool IsNearly(GlobalPose other, float equalityThreshold = 1E-30f) =>
            GlobalPosition.IsNearly(other.GlobalPosition, equalityThreshold) &&
            GlobalRotation.eulerAngles.IsNearly(other.GlobalRotation.eulerAngles);

        [Pure] public void Deconstruct(out GlobalPosition position, out GlobalRotation rotation) =>
            (position, rotation) = (Position, Rotation);

        [Pure] public override string ToString() => "{" + Position + " | " + Rotation + "}";

        [Pure] public GlobalPose LerpTo(GlobalPose target, double progress, bool shouldClamp) => new GlobalPose(
            Position.LerpTo(target.Position, progress, shouldClamp),
            Rotation.LerpTo(target.Rotation, progress, shouldClamp)
        );

        [Pure] public GlobalPose SlerpTo(GlobalPose target, double progress, bool shouldClamp) => new GlobalPose(
            Position.SlerpTo(target.Position, progress, shouldClamp),
            Rotation.SlerpTo(target.Rotation, progress, shouldClamp)
        );
    }

    public static partial class PoseUtils {
        
        [Pure] public static GlobalPose WithPosition(this GlobalPose source, Func<GlobalPosition, GlobalPosition> positionMapping) =>
            new GlobalPose(positionMapping(source.Position), source.Rotation);

        [Pure] public static GlobalPose WithRotation(this GlobalPose source, Func<GlobalRotation, GlobalRotation> rotationMapping) =>
            new GlobalPose(source.Position, rotationMapping(source.Rotation));

        [Pure] public static GlobalPose WithForward(this GlobalPose source, Func<GlobalDirection, GlobalDirection> forwardMapping) =>
            new GlobalPose(source.Position, forwardMapping(source.Forward));

        [Pure] public static GlobalPose ToGlobalPose(this (Vector3 position, Vector3 forward) tuple)
            => new GlobalPose(tuple.position, tuple.forward);

        [Pure] public static GlobalPose ToGlobalPoseWith(this GlobalPosition pos, GlobalRotation rot) =>
            new GlobalPose(pos, rot);
        
        [Pure] public static GlobalPose ToGlobalPoseWith(this GlobalRotation rot, GlobalPosition pos) =>
            new GlobalPose(pos, rot);
        
        [Pure] public static GlobalPose GlobalPose(this Component component) 
            => new GlobalPose(component.transform);
        [Pure] public static GlobalPose GlobalPose(this GameObject gameObject) 
            => new GlobalPose(gameObject.transform);

        [Pure] public static GlobalPose Average(this IEnumerable<GlobalPose> locations) {
            var vectorList = locations.ToList();
            return vectorList
                .Select(c => (pos: c.GlobalPosition, forward: c.GlobalForward))
                .Aggregate((p, c) => (p.pos + c.pos, p.forward + c.forward))
                .Let(t => new GlobalPose(t.pos / vectorList.Count, t.forward));
            // No need to divide the forward, it will be normalized anyways.
        }
        
        [Pure] public static GlobalPose LerpTo(this GlobalPose start, GlobalPose end, double perc, bool shouldClamp = true) =>
            new GlobalPose(
                start.Position.LerpTo(end.Position, perc, shouldClamp),
                start.Rotation.LerpTo(end.Rotation, perc, shouldClamp)
            );
        
        [Pure] public static GlobalPose SlerpTo(this GlobalPose start, GlobalPose end, double perc, bool shouldClamp = true) =>
            new GlobalPose(
                start.Position.SlerpTo(end.Position, perc, shouldClamp),
                start.Rotation.SlerpTo(end.Rotation, perc, shouldClamp)
            );

        /// <inheritdoc cref="GeometryUtils.PointReflect(Vector3, Vector3)"/>
        [Pure] public static GlobalPose Reflect(this GlobalPose self, GlobalPosition on) => new(
            position: self.Position.Reflect(on),
            forward: -self.Forward
        );

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3, Vector3)"/>
        [Pure] public static GlobalPose Reflect(
            this GlobalPose self, GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => new(
            position: self.Position.Reflect(pointOnPlane, planeNormal),
            forward: self.Forward.Reflect(planeNormal)
        );
        
        [Pure] public static GlobalPose Rotate(
            this GlobalPose self, GlobalPosition pivot, GlobalRotation rotation    
        ) => new(
            position: self.Position.Rotate(pivot, rotation),
            rotation: rotation * self.Rotation
        );

    }
}