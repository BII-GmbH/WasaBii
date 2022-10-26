using System;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    public interface VectorLike {
        Vector3 AsVector { get; }
    }
    
    /// Supertype for all transform utils that essentially wrap a vector.
    public interface VectorLike<TSelf> : VectorLike, GeometryHelper<TSelf>
    where TSelf : struct, VectorLike<TSelf> {
        
        /// Constructs a new instance of this type with the given value.
        /// Needed by utilities like `Map`.
        TSelf CopyWithDifferentValue(Vector3 newValue);

        [Pure] TSelf GeometryHelper<TSelf>.LerpTo(TSelf target, double progress, bool shouldClamp) =>
            CopyWithDifferentValue(shouldClamp 
                ? Vector3.Lerp(AsVector, target.AsVector, (float)progress) 
                : Vector3.LerpUnclamped(AsVector, target.AsVector, (float)progress)
            );

        [Pure] TSelf GeometryHelper<TSelf>.SlerpTo(TSelf target, double progress, bool shouldClamp)  =>
            CopyWithDifferentValue(shouldClamp 
                ? Vector3.Slerp(AsVector, target.AsVector, (float)progress) 
                : Vector3.SlerpUnclamped(AsVector, target.AsVector, (float)progress)
            );
    }
    
    /// Signals that the wrapped vector's magnitude is actually meaningful.
    /// Essentially ensures that some utilities like `Min` and 'Max' are not
    /// available for types without a magnitude, e.g. <see cref="LocalDirection"/>.
    /// Be sure to implement this if you want these.
    public interface HasMagnitude<TSelf>
    where TSelf : struct, HasMagnitude<TSelf>, VectorLike<TSelf> { }
    
}
