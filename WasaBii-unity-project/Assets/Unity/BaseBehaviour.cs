using System;
using BII.WasaBii.Geometry;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary>
    /// Author: Cameron Reuschel
    /// <br/><br/>
    /// Base class for all behaviours that depend on CoreLibrary utilities.
    /// Adds no additional overhead compared to extending from MonoBehaviour directly.
    /// </summary>
    public abstract class BaseBehaviour : MonoBehaviour {

        /// <inheritdoc cref="ComponentQueryExtensions.AssignComponent{T}(GameObject, out T, Search, bool)"/>
        /// <seealso cref="ComponentQueryExtensions.AssignComponent{T}(GameObject, out T, Search, bool)"/>
        protected void AssignComponent<T>(out T variable, Search where = Search.InObjectOnly) 
            where T : class => 
            gameObject.AssignComponent(out variable, @where);

        /// <inheritdoc cref="ComponentQueryExtensions.AssignIfAbsent{T}(GameObject, ref T, Search, bool)"/>
        /// <seealso cref="ComponentQueryExtensions.AssignIfAbsent{T}(GameObject, ref T, Search, bool)"/>
        protected bool AssignIfAbsent<T>(ref T variable, Search where = Search.InObjectOnly) 
            where T : class => 
            gameObject.AssignIfAbsent(ref variable, @where);

        /// <inheritdoc cref="ComponentQueryExtensions.AssignComponentOrAdd{T}(GameObject, out T, Search, bool)"/>
        /// <seealso cref="ComponentQueryExtensions.AssignComponentOrAdd{T}(GameObject, out T, Search, bool)"/>
        protected void AssignComponentOrAdd<T>(out T variable, Search where = Search.InObjectOnly) 
            where T : Component => 
            gameObject.AssignComponentOrAdd(out variable, @where);

        /// <inheritdoc cref="ComponentQueryExtensions.AssignIfAbsentOrAdd{T}(GameObject, ref T, Search, bool)"/>
        /// <seealso cref="ComponentQueryExtensions.AssignIfAbsentOrAdd{T}(GameObject, ref T, Search, bool)"/>
        protected bool AssignIfAbsentOrAdd<T>(ref T variable, Search where = Search.InObjectOnly)
            where T : Component => 
            gameObject.AssignIfAbsentOrAdd(ref variable, @where);

        /// <inheritdoc cref="Util.IfAbsentCompute{T}(ref T, Func{T})"/>
        /// <seealso cref="Util.IfAbsentCompute{T}(ref T, Func{T})"/>
        protected bool IfAbsentCompute<T>(ref T field, Func<T> getter) => 
            Util.IfAbsentCompute(ref field, getter);
    }
}