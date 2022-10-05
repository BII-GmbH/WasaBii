﻿using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity {

    /// Automatically updates the line renderer every frame so that its positions are those of the
    /// transforms specified in <see cref="_targets"/> (in the same order).
    [RequireComponent(typeof(LineRenderer))]
    public class DynamicLineRenderer : MonoBehaviour {
        
        [SerializeField] private List<Transform> _targets = new ();

        private LineRenderer? __renderer;
        private new LineRenderer renderer => gameObject.GetOrAssignIfAbsent(ref __renderer)!;

        public void SetTargets(IEnumerable<Transform> targets) {
            _targets.Clear();
            _targets.AddRange(targets);
            updatePositionCount();
        }

        private void OnValidate() => updatePositionCount();

        [ExecuteAlways]
        private void Update() {
            if (_targets.Count < 2) return;
            renderer.SetPositions(_targets.WithoutNull().Select(t => t.position).ToArray());
        }

        private void updatePositionCount() => renderer.positionCount = _targets.Count.Max(2);

    }

}