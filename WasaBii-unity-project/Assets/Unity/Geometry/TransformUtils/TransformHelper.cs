using JetBrains.Annotations;

namespace BII.WasaBii.Unity.Geometry {
    
    /// Supertype for all transform utils.
    public interface TransformHelper<TSelf>
        where TSelf : TransformHelper<TSelf> {

        [Pure] TSelf LerpTo(TSelf target, double progress, bool shouldClamp = true);
        [Pure] TSelf SlerpTo(TSelf target, double progress, bool shouldClamp = true);
    }

}