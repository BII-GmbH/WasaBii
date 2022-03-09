using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BII.WasaBii.Unity {
    /// <summary>
    /// Author: Cameron Reuschel <br/><br/>
    /// Added automatically by inheriting from <see cref="QueryableBehaviour"/>.
    /// <br/><br/>
    /// DO NOT ADD DIRECTLY.
    /// </summary>
    public sealed class Queryable : MonoBehaviour {
        // TODO CR for maintainer: figure out a way to register secondary queryables which are added later
        // => we do not want to override methods in the QueryableBehaviour, so we'd have to *poll* here (ew)

        private Query _q;

        internal readonly List<QueryableBehaviour> Underlying = new();

        private void OnEnable() => _q.SetEnabled(this);

        private void OnDisable() => _q.SetDisabled(this);

        private void Awake() => _q = Query.Instance;

        private void Start() {
            Underlying.AddRange(GetComponents<QueryableBehaviour>());
            Underlying.ForEach(u => _q.Register(u));
        }

        private void OnDestroy() =>
            Underlying?.ForEach(u => {
                if (_q != null) _q.Deregister(u);
            });
    }
}