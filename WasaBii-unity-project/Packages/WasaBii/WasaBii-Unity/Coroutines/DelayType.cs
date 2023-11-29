#nullable enable

using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary> Used in coroutine delay techniques to define when the coroutine should resume. </summary>
    public enum DelayType {
        /// <summary> After `Update` and before `LateUpdate`. Corresponds to `yield return null`. </summary>
        Default,
        /// <summary> After every `FixedUpdate`. Corresponds to `yield return new WaitForFixedUpdate()`. </summary>
        Fixed,
        /// <summary> At the very end of the frame, after rendering has finished. Corresponds to `yield return new WaitForEndOfFrame()`. </summary>
        EndOfFrame
    }

    public static class DelayTypeExtensions {
        public static YieldInstruction? ToYieldInstruction(this DelayType delayType) => 
            delayType switch {
                DelayType.Default => null,
                DelayType.Fixed => new WaitForFixedUpdate(),
                DelayType.EndOfFrame => new WaitForEndOfFrame(),
                _ => throw new UnsupportedEnumValueException(delayType)
            };
    }

}