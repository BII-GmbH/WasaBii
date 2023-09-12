﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a the difference between two world-space positions.
    /// Can also be viewed as a <see cref="GlobalDirection"/> with a length.
    /// </summary>
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: true)]
    public partial struct GlobalVelocity : 
        GlobalDirectionLike<GlobalVelocity>,
        IsGlobalVariant<GlobalVelocity, LocalVelocity>
    {

        public static readonly GlobalVelocity Zero = new(System.Numerics.Vector3.Zero);
    
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public GlobalVelocity(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public GlobalVelocity(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public GlobalVelocity(float x, float y, float z) => AsUnityVector = new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public GlobalVelocity(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public GlobalVelocity(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public GlobalDirection Direction => new(AsNumericsVector);
        public Speed Magnitude => AsNumericsVector.Length().MetersPerSecond();

        public GlobalVelocity(Length x, Length y, Length z) : this((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()) { }

        [Pure] public static Builder From(GlobalPosition origin) => new Builder(origin);

        /// <summary>
        /// <inheritdoc cref="TransformProvider.InverseTransformVelocity"/>
        /// This is the inverse of <see cref="LocalVelocity.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalVelocity RelativeTo(TransformProvider parent)
            => new(parent.InverseTransformOffset(new(this.AsNumericsVector)).AsNumericsVector);

        /// Projects this Velocity onto the given direction.
        [Pure] public GlobalVelocity Project(GlobalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this Velocity onto the plane defined by its normal.
        [Pure] public GlobalVelocity ProjectOnPlane(GlobalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this Velocity off the plane defined by the given normal
        [Pure]
        public GlobalVelocity Reflect(GlobalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        public Speed Dot(GlobalDirection normal) => System.Numerics.Vector3.Dot(AsNumericsVector, normal.AsNumericsVector).MetersPerSecond();

        public GlobalVelocity Cross(GlobalVelocity other) =>
            new(System.Numerics.Vector3.Cross(AsNumericsVector, other.AsNumericsVector));

        public Angle SignedAngleTo(GlobalVelocity other, GlobalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        public Angle SignedAngleOnPlaneTo(GlobalVelocity other, GlobalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleOnPlaneTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        [Pure] public static GlobalVelocity operator +(GlobalVelocity left, GlobalVelocity right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalVelocity operator -(GlobalVelocity left, GlobalVelocity right) => new(left.AsNumericsVector - right.AsNumericsVector);

        [Pure] public static GlobalVelocity operator -(GlobalVelocity Velocity) => new(-Velocity.AsNumericsVector);

        [Pure]
        public static GlobalOffset operator *(GlobalVelocity velocity, Duration duration) => 
            new(velocity.AsNumericsVector * (float)duration.AsSeconds());

        [Pure] public static GlobalOffset operator *(Duration duration, GlobalVelocity velocity) => velocity * duration;
        
        public readonly struct Builder {
            private readonly GlobalPosition origin;
            public Builder(GlobalPosition origin) => this.origin = origin;
            [Pure] public WithDestination To(GlobalPosition destination) => new (origin, destination);
            
            public readonly struct WithDestination {
                private readonly GlobalPosition origin;
                private readonly GlobalPosition destination;
                public WithDestination(GlobalPosition origin, GlobalPosition destination) {
                    this.origin = origin;
                    this.destination = destination;
                }
                [Pure] public GlobalVelocity In(Duration duration) => (destination - origin) / duration;

            }
        }

    }

    public static partial class GlobalVelocityExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalVelocity AsGlobalVelocity(this UnityEngine.Vector3 globalVelocity)
            => new(globalVelocity);
        #endif
        
        [Pure] public static GlobalVelocity AsGlobalVelocity(this System.Numerics.Vector3 globalVelocity)
            => new(globalVelocity);

        [Pure]
        public static GlobalVelocity Sum(this IEnumerable<GlobalVelocity> Velocitys) => 
            Velocitys.Select(o => o.AsNumericsVector).Aggregate(System.Numerics.Vector3.Add).AsGlobalVelocity();

    }
    
}