using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLibrary
{
    /// <summary>
    /// Author: Cameron Reuschel
    /// <br/><br/>
    /// This class serves as a namespace for every
    /// non-specific utility function, such as 
    /// <see cref="Mod"/> and <see cref="IsNull{T}"/>,
    /// as well as <see cref="VectorProxy"/>.
    /// </summary>
    public static class Util
    {

        /// <summary>
        /// Constructs an IEnumerable from a variable number of arguments.
        /// This is an alternative to writing <code>new [] { a, b, c }</code>
        /// when the number of arguments for an IEnumerable parameter is known.
        /// </summary>
        /// <returns>An array consisting of all the passed values.</returns>
        [JetBrains.Annotations.Pure] public static T[] Seq<T>(params T[] args) { return args; }
        
        /// <summary>
        /// Mathematically correct modulus.
        /// Always positive for any x as long as m > 0.
        /// </summary>
        [JetBrains.Annotations.Pure] public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
        
        /// <summary>
        /// A null check that works for any generic type T. This works
        /// works for unity components as well as every other type.
        /// <br/>
        /// See <a href="https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/">
        /// this blog post</a> for more details about Unity's custom null handling. 
        /// </summary>
        [JetBrains.Annotations.Pure] public static bool IsNull<T>(T value)
        {
            // a `where T : class` constraint is not possible, since that would disallow nullables.
            return value == null || value is UnityEngine.Object && value as UnityEngine.Object == null;
        }
        
        /// <summary>
        /// A field is not assigned if its value is equal to either its
        /// default value, null, or Unity's definition of equal to null.
        /// <br/>
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter and true is returned.
        /// <br/>
        /// Otherwise the field remains unchanged, the getter
        /// is never called and the operation returns false.
        /// </summary>
        public static bool IfAbsentCompute<T>(ref T field, [NotNull] Func<T> getter)
        {
            if (IsNull(field) || EqualityComparer<T>.Default.Equals(field, default(T)))      
            {
                field = getter();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// A field is not assigned if its value is equal to either its
        /// default value, null, or Unity's definition of equal to null.
        /// <br/>
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter.
        /// <br/>
        /// Regardless if it was or wasn't assigned the field is returned afterwards.
        /// If the getter produces a non-null or non-default value result,
        /// the returned field will always be assigned.
        /// </summary>
        public static T IfAbsentComputeThenReturn<T>(ref T field, [NotNull] Func<T> getter) {
            IfAbsentCompute(ref field, getter);
            return field;
        }
        
        /// <summary>
        /// A nullable field is not assigned if its value is equal to null.
        /// <br/>
        /// If the specified field is not assigned yet, it is assigned the
        /// result of calling the specified getter.
        /// <br/>
        /// Regardless if it was or wasn't assigned the field is returned afterwards.
        /// Since the getter must produce a non-null result,
        /// the returned field will always be non-null.
        /// </summary>
        public static T IfAbsentComputeThenReturn<T>(ref T? field, [NotNull] Func<T> getter) where T : struct {
            IfAbsentCompute(ref field, () => getter());
            Contract.Assert(field != null);
            return field.Value;
        }

        /// If the field is null, it will be assigned the given value.
        /// Otherwise, the <paramref name="whenNotNull"/> action is executed.
        public static void AssignValueIfNull<T>(ref T field, [NotNull] T value, Action<T> whenNotNull) where T : class {
            if (field == null)
                field = value;
            else whenNotNull(field);
        }
        
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
        public class VectorProxy
        {
            private Func<Vector3> _getter;
            private Action<Vector3> _setter;
            private bool _wrapsVectorDirectly = false;
            
            public VectorProxy([NotNull] Func<Vector3> getter, [NotNull] Action<Vector3> setter)
            {
                _getter = getter;
                _setter = setter;
            }

            private Vector3 _vectorRef;
            public VectorProxy(Vector3 vectorRef = default(Vector3))
            {
                _vectorRef = vectorRef;
                _wrapsVectorDirectly = true;
            }
			
            /// <summary>
            /// The x coordinate of this position.
            /// </summary>
            public float x
            {
                get
                {
                    return _wrapsVectorDirectly ? _vectorRef.x : _getter().x; }
                set
                {
                    if (_wrapsVectorDirectly) _vectorRef.x = value;
                    else _setter(_getter().WithX(value));
                }
            }
			
            /// <summary>
            /// The y coordinate of this position.
            /// </summary>
            public float y
            {
                get
                {
                    return _wrapsVectorDirectly ? _vectorRef.y : _getter().y; }
                set
                {
                    if (_wrapsVectorDirectly) _vectorRef.y = value;
                    else _setter(_getter().WithY(value));
                }
            }
			
            /// <summary>
            /// The z coordinate of this position.
            /// </summary>
            public float z
            {
                get
                {
                    return _wrapsVectorDirectly ? _vectorRef.z : _getter().z; }
                set
                {
                    if (_wrapsVectorDirectly) _vectorRef.z = value;
                    else _setter(_getter().WithZ(value));
                }
            }
			
            public static implicit operator Vector3(VectorProxy proxy)
            {
                return proxy._wrapsVectorDirectly ? proxy._vectorRef : proxy._getter();
            }
            
            public static implicit operator VectorProxy(Vector3 vec)
            {
                return new VectorProxy(vec);
            }
        }

    }
}