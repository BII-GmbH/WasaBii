using System;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Vector3"/> that represents a the difference between two positions in the same local space.
    /// Can also be viewed as a <see cref="LocalDirection"/> with a length.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalOffset : 
        LocalDirectionLike<LocalOffset>, 
        HasMagnitude<LocalOffset>, 
        IsLocalVariant<LocalOffset, GlobalOffset>,
        IEquatable<LocalOffset> {
        
        public Vector3 AsVector { get; }

        public LocalDirection Normalized => (LocalDirection) this;

        public LocalPosition AsPosition => LocalPosition.FromLocal(AsVector);

        private LocalOffset(Vector3 local) => this.AsVector = local;

        [Pure] public static LocalOffset FromGlobal(TransformProvider parent, Vector3 global)
            => FromLocal(parent.InverseTransformVector(global));

        [Pure] public static LocalOffset FromLocal(Vector3 local)
            => new LocalOffset(local);

        [Pure] public static LocalOffset FromLocal(Length x, Length y, Length z) 
            => FromLocal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static LocalOffset FromLocal(float x, float y, float z) 
            => FromLocal(new Vector3(x, y, z));

        [Pure] public static Builder From(LocalPosition origin) => new Builder(origin);

        /// Transforms the local offset into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalOffset.RelativeTo"/>
        [Pure] public GlobalOffset ToGlobalWith(TransformProvider parent)
            => GlobalOffset.FromLocal(parent, AsVector);

        /// <inheritdoc cref="Vector3.Project"/>
        [Pure] public LocalOffset Project(LocalDirection onNormal)
            => Vector3.Project(AsVector, onNormal.AsVector).AsLocalOffset();

        /// <inheritdoc cref="Vector3.ProjectOnPlane"/>
        [Pure] public LocalOffset ProjectOnPlane(LocalDirection planeNormal)
            => Vector3.ProjectOnPlane(AsVector, planeNormal.AsVector).AsLocalOffset();

        public Length Length => AsVector.magnitude.Meters();

        [Pure] public static LocalOffset operator +(LocalOffset left, LocalOffset right) =>
            new LocalOffset(left.AsVector + right.AsVector);

        [Pure] public static LocalOffset operator -(LocalOffset left, LocalOffset right) =>
            new LocalOffset(left.AsVector - right.AsVector);

        [Pure] public static LocalOffset operator -(LocalOffset offset) => new LocalOffset(-offset.AsVector);

        [Pure] public static LocalOffset operator *(float scalar, LocalOffset offset) =>
            (scalar * offset.AsVector).AsLocalOffset();
        [Pure] public static LocalOffset operator /(LocalOffset offset, float scalar) =>
            (offset.AsVector / scalar).AsLocalOffset();

        [Pure] public static LocalOffset operator *(LocalOffset offset, float scalar) => scalar * offset;

        [Pure] public static LocalOffset operator *(double scalar, LocalOffset offset) => (float)scalar * offset;
        [Pure] public static LocalOffset operator *(LocalOffset offset, double scalar) => (float)scalar * offset;
        [Pure] public static LocalOffset operator /(LocalOffset offset, double scalar) => offset / (float)scalar;
        
        [Pure] public static bool operator ==(LocalOffset a, LocalOffset b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(LocalOffset a, LocalOffset b) => a.AsVector != b.AsVector;

        [Pure] public bool Equals(LocalOffset other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is LocalOffset dir && this == dir;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();

        public readonly struct Builder {
            private readonly LocalPosition origin;
            public Builder(LocalPosition origin) => this.origin = origin;
            [Pure] public LocalOffset To(LocalPosition destination) => destination - origin;
        }

        public static LocalOffset Up => Vector3.up.AsLocalOffset();
        public static LocalOffset Down => Vector3.down.AsLocalOffset();
        public static LocalOffset Left => Vector3.left.AsLocalOffset();
        public static LocalOffset Right => Vector3.right.AsLocalOffset();
        public static LocalOffset Forward => Vector3.forward.AsLocalOffset();
        public static LocalOffset Back => Vector3.back.AsLocalOffset();
        public static LocalOffset One => Vector3.one.AsLocalOffset();
        public static LocalOffset Zero => Vector3.zero.AsLocalOffset();
        
        [Pure] public static LocalOffset Lerp(
            LocalOffset start, LocalOffset end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalOffset Slerp(
            LocalOffset start, LocalOffset end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public LocalOffset CopyWithDifferentValue(Vector3 newValue) => FromLocal(newValue);
    }
    
    public static partial class OffsetExtensions {
        
        [Pure] public static LocalOffset AsLocalOffset(this Vector3 localOffset)
            => LocalOffset.FromLocal(localOffset);

        [Pure] public static LocalOffset AsLocalOffset(this System.Numerics.Vector3 localOffset)
            => localOffset.ToUnityVector().AsLocalOffset();

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3)"/>
        [Pure] public static LocalOffset Reflect(
            this LocalOffset self, LocalDirection planeNormal
        ) => self.AsVector.Reflect(planeNormal.AsVector).AsLocalOffset();

    }

}
