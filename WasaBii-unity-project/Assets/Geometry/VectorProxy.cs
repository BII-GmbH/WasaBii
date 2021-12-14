using System;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Geometry {
    
    /// <summary>
    /// Author: Cameron Reuschel
    /// <br/><br/>
    /// A proxy that enables setting x, y and z coordinates of something holding a vector by reference.
    /// Provides implicit conversions from and to <see cref="Vector3"/>. 
    /// </summary>
    /// <example><code>
    /// var proxy = new VectorProxy(() => transform.position, vec => transform.position = vec);
    /// </code></example>
    // ReSharper disable all InconsistentNaming
    public sealed class VectorProxy {
        
        private Func<Vector3> _getter;
        private Action<Vector3> _setter;
        private bool _wrapsVectorDirectly = false;

        public VectorProxy([NotNull] Func<Vector3> getter, [NotNull] Action<Vector3> setter) {
            _getter = getter;
            _setter = setter;
        }

        private Vector3 _vectorRef;

        public VectorProxy(Vector3 vectorRef = default(Vector3)) {
            _vectorRef = vectorRef;
            _wrapsVectorDirectly = true;
        }

        /// <summary>
        /// The x coordinate of this position.
        /// </summary>
        public float x {
            get => _wrapsVectorDirectly ? _vectorRef.x : _getter().x;
            set {
                if (_wrapsVectorDirectly) _vectorRef.x = value;
                else _setter(_getter().WithX(value));
            }
        }

        /// <summary>
        /// The y coordinate of this position.
        /// </summary>
        public float y {
            get => _wrapsVectorDirectly ? _vectorRef.y : _getter().y;
            set {
                if (_wrapsVectorDirectly) _vectorRef.y = value;
                else _setter(_getter().WithY(value));
            }
        }

        /// <summary>
        /// The z coordinate of this position.
        /// </summary>
        public float z {
            get => _wrapsVectorDirectly ? _vectorRef.z : _getter().z;
            set {
                if (_wrapsVectorDirectly) _vectorRef.z = value;
                else _setter(_getter().WithZ(value));
            }
        }

        public static implicit operator Vector3(VectorProxy proxy) => 
            proxy._wrapsVectorDirectly ? proxy._vectorRef : proxy._getter();

        public static implicit operator VectorProxy(Vector3 vec) => new VectorProxy(vec);
    }
}