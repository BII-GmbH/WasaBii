using System;

namespace BII.WasaBii.Unity {
    /// <summary>
    /// Thrown when calling <see cref="Singleton{T}.Instance"/>
    /// and there are either none or multiple instances of the
    /// component in a scene.
    /// </summary>
    public class WrongSingletonUsageException : Exception {
        internal WrongSingletonUsageException(string message) : base(message) {}
    }
}