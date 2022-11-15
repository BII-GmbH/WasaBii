#nullable enable

using System.ComponentModel;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// Used in coroutine delay techniques to define when the coroutine should resume-
    public enum DelayType {
        /// After `Update` and before `LateUpdate`. Corresponds to `yield return null`.
        Default,
        /// After every `FixedUpdate`. Corresponds to `yield return new WaitForFixedUpdate()`.
        Fixed,
        /// At the very end of the frame, after rendering has finished. Corresponds to `yield return new WaitForEndOfFrame()`.
        EndOfFrame
    }

    public static class DelayTypeExtensions {
        public static YieldInstruction? ToYieldInstruction(this DelayType delayType) => 
            delayType switch {
                DelayType.Default => null,
                DelayType.Fixed => new WaitForFixedUpdate(),
                DelayType.EndOfFrame => new WaitForEndOfFrame(),
                _ => throw new InvalidEnumArgumentException()
            };
    }

}