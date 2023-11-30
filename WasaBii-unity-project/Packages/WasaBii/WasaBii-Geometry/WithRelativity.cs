using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// The base for transform util types that have a built-in definition of what geometric
    /// space their values are given relative to. Specifically, an object is either encoded
    /// globally or locally relative to some parent.
    /// Given the relevant parent, such a type can be converted between global and local space.
    /// This means that every type with relativity should have a dual counterpart for the
    /// other version.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use one of the generic versions instead.
    /// </summary>
    public interface WithRelativity { }

    /// <summary>
    /// States that the implementing type works in global space.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    /// </summary>
    public interface IsGlobal : WithRelativity { }
    
    /// <summary>
    /// States that the implementing type works in some local space.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    /// </summary>
    public interface IsLocal : WithRelativity { }

    /// <summary>
    /// States that the implementing type works in global space and that the local variant is `TLocal`.
    /// Hence, it offers a method to transform it into local space given the parent.
    /// </summary>
    public interface IsGlobalVariant<TGlobal, out TLocal> : IsGlobal
    where TGlobal : IsGlobalVariant<TGlobal, TLocal>
    where TLocal : IsLocalVariant<TLocal, TGlobal> {
        /// <summary>
        /// Transforms this value into the local space relative to the <see cref="parent"/>
        /// </summary>
        [Pure] TLocal RelativeTo(TransformProvider parent);
    }

    /// <summary>
    /// States that the implementing type works in local space and that the global variant is `TGlobal`.
    /// Hence, it offers a method to transform it into global space given the parent.
    /// </summary>
    public interface IsLocalVariant<TLocal, out TGlobal> : IsLocal
    where TGlobal : IsGlobalVariant<TGlobal, TLocal>
    where TLocal : IsLocalVariant<TLocal, TGlobal> {
        /// <summary>
        /// Transforms this value into world space, assuming that it was
        /// previously defined in local space relative to the <see cref="parent"/>
        /// </summary>
        [Pure] TGlobal ToGlobalWith(TransformProvider parent);

        /// <summary>
        /// Transforms this value into another local space, assuming that the <see cref="offset"/>
        /// is defined relative to the same parent as <see cref="this"/>. The result is relative to
        /// <code>newParent = oldParent * offset</code>.
        /// </summary>
        [Pure] TLocal TransformBy(LocalPose offset);

    }

}