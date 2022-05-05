using System;
using BII.WasaBii.UnitSystem;
using UnityEngine;

namespace DefaultNamespace {
    public static class aaDELETEME {
        static aaDELETEME() {
            if (!EnsureGenerationDidRun.DidRun) throw new Exception();
            Debug.Log(EnsureGenerationDidRun.ErrorMessage);
        }
    }
}