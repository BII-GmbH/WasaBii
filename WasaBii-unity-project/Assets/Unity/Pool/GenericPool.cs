#nullable enable

using System;
using System.Collections.Generic;
using BII.WasaBii.Unity.Exceptions;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary>
    /// <para>
    /// A generic pool for pre-loading a certain number
    /// of copies of a single prefab in <see cref="Template"/>.
    /// This is especially useful when using a large amount 
    /// of game objects such as physically accurate bullets.
    /// Instead of creating a bullet every time it is fired,
    /// which might cause a drop in fps, it is more efficient
    /// to load a number of bullets at the beginning of the
    /// scene and reuse already fired bullets.
    /// </para><para>
    /// In order to reuse items the passed prefab must have a
    /// component that extends <see cref="Reusable"/>. When an
    /// item is ready to be reused, you must manually call
    /// <see cref="Reusable.FreeForReuse()"/>. When acquiring
    /// an item from the pool, items for that
    /// <code>gameObject.activeSelf</code> holds true are
    /// preferred to objects that are still active. The reason
    /// for this is that for example fired bullets may stay on
    /// the ground and only be despawned and reused on demand.
    /// The fact that marking for reuse and resetting the
    /// object's state have their entirely own mechanisms
    /// makes this pool implementation as generic as possible.
    /// </para><para>
    /// You can set the <see cref="Template"/> and <see cref="Capacity"/>
    /// either in the inspector or in a script. By default, a pool is
    /// <b>lazily instantiated</b>. No items are instantiated until
    /// either <see cref="Init"/> is called manually, or the first
    /// item is requested. <i>You can override this behaviour by
    /// setting <see cref="InitOnSceneStart"/> to true in the
    /// inspector.</i> Any changes to either <see cref="Template"/>
    /// or <see cref="Capacity"/> will be rejected with a warning
    /// after initialization. 
    /// </para><para>
    /// To ensure working conditions even under extreme and
    /// unexpected circumstances, the pool behaves similar
    /// to <see cref="List{T}"/> in that it's capacity
    /// increases as soon as there are no more items available.
    /// </para><para>
    /// When no items are currently available for reuse, the pool
    /// calls <see cref="Reusable.ReuseRequested"/> on every managed
    /// item until one frees itself for reuse. When no items are
    /// found, the pool's capacity increases and a new item is instantiated.
    /// </para><para>
    /// The <see cref="GrowRate"/> field describes the percentage
    /// with which the pool's <see cref="Capacity"/> increases in
    /// size every time new items are required. When the
    /// <see cref="GrowRate"/> is set to 0, an exception is thrown
    /// instead. Once the pool's capacity increases, not all free
    /// spots are immediately filled by new items. Instead, items
    /// are created as soon as they are immediately required or else
    /// once per frame until the total number of items are equal
    /// to the pool's <see cref="Capacity"/>.
    /// </para>
    /// </summary>
    public partial class GenericPool : MonoBehaviour {
        
        // ReSharper disable all InconsistentNaming
        
        [Range(0, 1)]
        [Tooltip("When no items are available the pool Capacity increases by (Capacity * (GrowRate + 1)). " +
                 "\nWhen this equals zero the pool throws an error instead of growing on demand.")]
        [SerializeField] private float GrowRate = 0.3f;
        
        [Tooltip(
            "The game object or prefab that is duplicated for use in this pool. Must have a 'Reusable' component.")]
        [SerializeField] private Reusable? Template;

        [Tooltip("When set, instantiates this pool at the start of the scene.")]
        [SerializeField] private  bool InitOnSceneStart = false;

        [Tooltip("The maximum number of objects in this pool.")]
        [SerializeField] private int Capacity;

        private List<Reusable> _buffer = default!; // initialized in Init()
        private int _lastIndex = 0;

        private void OnValidate() {
            if (_didInit) {
                Capacity = _buffer!.Capacity;
                Debug.LogWarning("You cannot change a pool's template or capacity after it has been initialized.");
            }
        }

        private bool _didInit;

        /// <summary>
        /// Call to initialize the pool manually.
        /// This is automatically called in `Start()`.
        /// </summary>
        /// <exception cref="NoTemplateException">
        /// When <see cref="Template"/> is null.
        /// </exception>
        public void Init() {
            if (_didInit) return;
            if (Capacity < 0) {
                Debug.LogWarning(this + ": Capacity must be at least zero but was " + Capacity);
                Capacity = 0;
            }

            if (Template == null)
                throw new NoTemplateException(this);
            _didInit = true;
            _buffer = new List<Reusable>(Capacity);
            for (var i = 0; i < Capacity; ++i) AddItem();
        }

        private void Start() {
            if (InitOnSceneStart) Init();
        }

        private void Update() {
            if (!_didInit) return;
            if (_buffer.Count < Capacity) AddItem();
        }

        private int _numAddedItems;

        private Reusable AddItem()
        {
            if (Template == null) throw new InvalidOperationException(
                $"Cannot add item to pool {gameObject.name}: {nameof(Template)} is not assigned.");
            var curr = Instantiate(Template);
            curr.gameObject.name = Template.name + " (Pool Item #" + ++_numAddedItems + ")";
            curr.FreeForReuse();
            _buffer.Add(curr);
            curr.gameObject.SetActive(false);
            return curr;
        }

        /// <returns>An instance of the <see cref="Template"/> prefab, active and at the origin.</returns>
        /// <exception cref="PoolOutOfItemsException">
        /// If no items are available and GrowRate == 0
        /// </exception>
        /// <seealso cref="RequestItem(Vector3)"/>
        /// <seealso cref="RequestItem(Vector3, Quaternion)"/>
        public GameObject RequestItem() => RequestItem(Vector3.zero, Quaternion.identity);

        /// <returns>An instance of the <see cref="Template"/> prefab, active and at the specified position.</returns>
        /// <exception cref="PoolOutOfItemsException">
        /// If no items are available and GrowRate == 0
        /// </exception>
        /// <seealso cref="RequestItem()"/>
        /// <seealso cref="RequestItem(Vector3, Quaternion)"/>
        public GameObject RequestItem(Vector3 position) => RequestItem(position, Quaternion.identity);

        /// <returns>
        /// An instance of the <see cref="Template"/> prefab,
        /// active and with the specified position and rotation.</returns>
        /// <exception cref="PoolOutOfItemsException">
        /// If no items are available and GrowRate == 0
        /// </exception>
        /// <seealso cref="RequestItem()"/>
        /// <seealso cref="RequestItem(Vector3)"/>
        public GameObject RequestItem(Vector3 position, Quaternion rotation) {
            if (!_didInit) {
                Debug.LogWarning(
                    this + ": Did init on first RequestItem call. This might have caused a freeze. " +
                    "Make sure to manually call .Init() or tick 'Init On Scene Start' in the inspector.");
                Init();
            }

            if (Template == null)
                throw new InvalidOperationException("Cannot request item from pool: no Template set.");

            // first pass: find an inactive object
            for (var i = 0; i < _buffer.Count; ++i) {
                var ii = (i + _lastIndex + 1) % _buffer.Count;
                var t = _buffer[ii];
                if (t.CanBeReused && !t.gameObject.activeSelf) {
                    _lastIndex = ii;
                    return Reuse(t, position, rotation);
                }
            }

            // second pass: find any object marked as reusable
            for (var i = 0; i < _buffer.Count; ++i) {
                var ii = (i + _lastIndex + 1) % _buffer.Count;
                var t = _buffer[ii];
                if (t.CanBeReused) {
                    _lastIndex = ii;
                    return Reuse(t, position, rotation);
                }
            }

            // if no available item found...

            // ... try to request a reuse ...
            for (var i = 0; i < _buffer.Count; ++i) {
                var ii = (i + _lastIndex + 1) % _buffer.Count;
                var t = _buffer[ii];
                t.ReuseRequested();
                if (t.CanBeReused) {
                    _lastIndex = ii;
                    return Reuse(t, position, rotation);
                }
            }

            // ... else if it is not supposed to grow, fail ...
            if (Math.Abs(GrowRate) < Mathf.Epsilon)
                throw new PoolOutOfItemsException(
                    this + ": Requesting a " + Template.gameObject + " with buffer capacity " + Capacity +
                    " failed and GrowRate is set to 0. No more items available.");

            // ... and if not still growing to capacity ... 
            if (Capacity == _buffer.Count) { // ... extend the buffer capacity and ...
                var origCapacity = Capacity;
                Capacity = Capacity + Math.Max((int)(Capacity * GrowRate), 1);
                _buffer.Capacity = Capacity;

                Debug.LogWarning(this + ": Requesting a " + Template.GetType() +
                                 " with buffer capacity " + origCapacity +
                                 " failed! No more items available. Increasing capacity to " + Capacity, this);
            }

            // ... return a newly added item
            return Reuse(AddItem(), position, rotation);
        }

        private static GameObject Reuse(Reusable r, Vector3 position, Quaternion rotation) {
            r.ResetForReuse();
            r.LockForReuse();
            var res = r.gameObject;
            res.transform.position = position;
            res.transform.rotation = rotation;
            res.SetActive(true);
            r.AfterReuse();
            return res;
        }
    }
}