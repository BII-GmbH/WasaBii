using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Vector3"/> that represents a the difference between two world-space positions.
    /// Can also be viewed as a <see cref="GlobalDirection"/> with a length.
    [MustBeImmutable]
    [Serializable]
    public readonly struct GlobalOffset : 
        GlobalDirectionLike<GlobalOffset>,
        HasMagnitude<GlobalOffset>, 
        IsGlobalVariant<GlobalOffset, LocalOffset>,
        IEquatable<GlobalOffset> {
    
        public Vector3 AsVector { get; }

        public GlobalDirection Normalized => (GlobalDirection)this;

        private GlobalOffset(Vector3 global) => this.AsVector = global;

        [Pure] public static GlobalOffset FromGlobal(Vector3 global) => new GlobalOffset(global);

        [Pure] public static GlobalOffset FromGlobal(Length x, Length y, Length z) 
            => FromGlobal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static GlobalOffset FromGlobal(float x, float y, float z) 
            => FromGlobal(new Vector3(x, y, z));

        [Pure] public static GlobalOffset FromLocal(TransformProvider parent, Vector3 local)
            => FromGlobal(parent.TransformVector(local));

        [Pure] public static Builder From(PositionProvider origin) => new Builder(origin);

        /// Transforms the global offset into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalOffset.ToGlobalWith"/>
        [Pure] public LocalOffset RelativeTo(TransformProvider parent)
            => LocalOffset.FromGlobal(parent, AsVector);

        /// <inheritdoc cref="Vector3.Project"/>
        [Pure] public GlobalOffset Project(GlobalDirection onNormal)
            => Vector3.Project(AsVector, onNormal.AsVector).AsGlobalOffset();

        /// <inheritdoc cref="Vector3.ProjectOnPlane"/>
        [Pure] public GlobalOffset ProjectOnPlane(GlobalDirection planeNormal)
            => Vector3.ProjectOnPlane(AsVector, planeNormal.AsVector).AsGlobalOffset();

        public Length Length => AsVector.magnitude.Meters();

        [Pure] public static GlobalOffset operator +(GlobalOffset left, GlobalOffset right) =>
            new GlobalOffset(left.AsVector + right.AsVector);

        [Pure] public static GlobalOffset operator -(GlobalOffset left, GlobalOffset right) =>
            new GlobalOffset(left.AsVector - right.AsVector);

        [Pure] public static GlobalOffset operator -(GlobalOffset offset) => new GlobalOffset(-offset.AsVector);

        [Pure] public static GlobalOffset operator *(float scalar, GlobalOffset offset) =>
            (scalar * offset.AsVector).AsGlobalOffset();
        [Pure] public static GlobalOffset operator /(GlobalOffset offset, float scalar) =>
            (offset.AsVector / scalar).AsGlobalOffset();

        [Pure] public static GlobalOffset operator *(GlobalOffset offset, float scalar) => scalar * offset;
        
        [Pure] public static GlobalOffset operator *(double scalar, GlobalOffset offset) => (float)scalar * offset;
        [Pure] public static GlobalOffset operator *(GlobalOffset offset, double scalar) => (float)scalar * offset;
        [Pure] public static GlobalOffset operator /(GlobalOffset offset, double scalar) => offset / (float)scalar;

        [Pure] public static bool operator ==(GlobalOffset a, GlobalOffset b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(GlobalOffset a, GlobalOffset b) => a.AsVector != b.AsVector;

        [Pure] public bool Equals(GlobalOffset other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is GlobalOffset dir && this == dir;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();

        public readonly struct Builder {
            private readonly PositionProvider origin;
            public Builder(PositionProvider origin) => this.origin = origin;
            [Pure] public GlobalOffset To(PositionProvider destination) => destination.Wrapped - origin.Wrapped;
        }

        public static GlobalOffset Up => Vector3.up.AsGlobalOffset();
        public static GlobalOffset Down => Vector3.down.AsGlobalOffset();
        public static GlobalOffset Left => Vector3.left.AsGlobalOffset();
        public static GlobalOffset Right => Vector3.right.AsGlobalOffset();
        public static GlobalOffset Forward => Vector3.forward.AsGlobalOffset();
        public static GlobalOffset Back => Vector3.back.AsGlobalOffset();
        public static GlobalOffset One => Vector3.one.AsGlobalOffset();
        public static GlobalOffset Zero => Vector3.zero.AsGlobalOffset();
        
        [Pure] public static GlobalOffset Lerp(
            GlobalOffset start, GlobalOffset end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static GlobalOffset Slerp(
            GlobalOffset start, GlobalOffset end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public GlobalOffset CopyWithDifferentValue(Vector3 newValue) => FromGlobal(newValue);
    }

    public static partial class OffsetExtensions {
        
        [Pure] public static GlobalOffset AsGlobalOffset(this Vector3 globalOffset)
            => GlobalOffset.FromGlobal(globalOffset);

        [Pure] public static GlobalOffset AsGlobalOffset(this System.Numerics.Vector3 globalOffset)
            => globalOffset.ToUnityVector().AsGlobalOffset();

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3)"/>
        [Pure] public static GlobalOffset Reflect(
            this GlobalOffset self, GlobalDirection planeNormal
        ) => self.AsVector.Reflect(planeNormal.AsVector).AsGlobalOffset();
        
    }
    
}