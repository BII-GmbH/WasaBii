using System;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.DELETEME {
    public class EnsureGenerationRan : MonoBehaviour {

        public void Start() {
            if (!EnsureGenerationDidRun.DidRun) throw new Exception();
            Debug.Log(EnsureGenerationDidRun.ErrorMessage);
        }

    }
}