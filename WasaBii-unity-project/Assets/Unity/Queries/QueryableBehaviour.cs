using UnityEngine;

namespace BII.WasaBii.Unity {

    /// <summary>
    /// Author: Cameron Reuschel <br/><br/>
    /// 
    /// Inheriting from this class instead of <see cref="MonoBehaviour"/>
    /// causes a <see cref="Queryable"/> component to be added so that the
    /// inheriting class can be queried in a scene using the static methods
    /// on the <see cref="Query"/> class.
    ///
    /// Call <see cref="RegisterAsQueryable"/> when you add a component of
    /// this type manually via <see cref="GameObject.AddComponent{T}"/>.
    /// </summary>
    [RequireComponent(typeof(Queryable))]
    public abstract class QueryableBehaviour : MonoBehaviour {
        private Queryable _queryable;

        public Queryable Queryable {
            get {
                if (_queryable != null) return _queryable;
                _queryable = GetComponent<Queryable>();
                return _queryable;
            }
        }

        /// <summary>
        /// Queryable behaviours that are later added to a GameObject that already has queryable behaviours
        /// are not registered automatically. In these cases, you manually need to call this in Awake().
        /// </summary>
        /// <remarks>
        /// Automatically finding all queryable behaviours added late would either include polling
        /// or overriding OnEnabled or Awake, turning them virtual. Both are not desirable defaults.
        /// </remarks>
        public void RegisterAsQueryable() {
            Queryable.Underlying.Add(this);
            Query.Instance.Register(this);
        }
    }
}