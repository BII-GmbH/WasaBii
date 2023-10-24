using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using UnityEngine;

namespace BII.WasaBii.Unity {

    /// <summary>
    /// Automatically updates the line renderer every frame so that its positions are those of the
    /// transforms specified in <see cref="_positionProviders"/> (in the same order).
    /// </summary>
    /// <remarks>If the position providers are not expected to move every frame, it would be much more efficient
    /// to update the line manually on an event-basis. However, there is no general event that fires when an object
    /// moves, so this class is here for doing the polling when you have no events,
    /// e.g. for physics-controlled rigidbodys that move every frame anyways.</remarks>
    [RequireComponent(typeof(LineRenderer))]
    public class DynamicLineRenderer : MonoBehaviour {
        
        [SerializeField] private List<Transform> _positionProviders = new ();

        private LineRenderer? __renderer;
        private new LineRenderer renderer => gameObject.GetOrAssignIfAbsent(ref __renderer)!;

        public void SetPositionProviders(IEnumerable<Transform> positionProviders) {
            _positionProviders.Clear();
            _positionProviders.AddRange(positionProviders);
            updatePositionCount();
        }

        private void OnValidate() => updatePositionCount();

        [ExecuteAlways]
        private void Update() {
            if (_positionProviders.Count < 2) return;
            renderer.SetPositions(_positionProviders.WithoutNull().Select(t => t.position).ToArray());
        }

        private void updatePositionCount() => renderer.positionCount = _positionProviders.Count.Max(2);

    }

}