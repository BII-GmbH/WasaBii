using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry {

    /// The base for transform util types that have a built-in definition of what geometric
    /// space their values are given relative to. Specifically, an object is either encoded
    /// globally or locally relative to some parent.
    /// Given the relevant parent, such a type can be converted between global and local space.
    /// This means that every type with relativity should have a dual counterpart for the
    /// other version.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use one of the generic versions instead.
    public interface WithRelativity { }

    /// States that the implementing type works in global space.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    public interface IsGlobal : WithRelativity { }
    
    /// States that the implementing type works in some local space.
    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    public interface IsLocal : WithRelativity { }

    /// States that the implementing type works in global space and that the local variant is `TLocal`.
    /// Hence, it offers a method to transform it into local space given the parent.
    public interface IsGlobalVariant<TGlobal, out TLocal> : IsGlobal, GeometryHelper<TGlobal>
    where TGlobal : IsGlobalVariant<TGlobal, TLocal>
    where TLocal : IsLocalVariant<TLocal, TGlobal> {
        [Pure] TLocal RelativeTo(TransformProvider parent);
    }

    /// States that the implementing type works in local space and that the global variant is `TGlobal`.
    /// Hence, it offers a method to transform it into global space given the parent.
    public interface IsLocalVariant<TLocal, out TGlobal> : IsLocal, GeometryHelper<TLocal>
    where TGlobal : IsGlobalVariant<TGlobal, TLocal>
    where TLocal : IsLocalVariant<TLocal, TGlobal> {
        [Pure] TGlobal ToGlobalWith(TransformProvider parent);
        [Pure] TLocal TransformBy(LocalPose offset);

    }

}