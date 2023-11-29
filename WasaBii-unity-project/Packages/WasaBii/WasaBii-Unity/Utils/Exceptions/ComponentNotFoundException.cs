using System;

namespace BII.WasaBii.Unity {
    /// <summary>
    /// Thrown when a component could not be found
    /// during an <see cref="ComponentQueryExtensions.AssignComponent{T}"/> call.
    /// </summary>
    public class ComponentNotFoundException : Exception {
        internal ComponentNotFoundException(string message) : base(message) { }
    }
}