using BII.WasaBii.Unity.Exceptions;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// Non-generic marker interface.
    public interface Singleton { }

    /// <summary>
    /// Author: Cameron Reuschel
    /// <br/><br/>
    /// Any BaseBehaviour that is a singleton should only be found once.
    /// Enables static access to this single object by using <code>Classname.Instance</code>.
    /// </summary>
    /// <typeparam name="T">The implementing type itself</typeparam>
    public abstract class Singleton<T> : MonoBehaviour, Singleton where T : Singleton<T> {
        private static T _instance;

        public static bool HasInstance => _instance != null;

        /// <summary>
        /// Returns the instance of this singleton.
        /// </summary>
        [NotNull]
        public static T Instance {
            get {
                if (_instance != null) return _instance;

                var tmp = FindObjectsOfType<T>();

                if (tmp == null || tmp.Length == 0)
                    throw new WrongSingletonUsageException(
                        "Singleton: An instance of " + typeof(T).Name +
                        " is needed in the scene, but there is none.");
                if (tmp.Length > 1)
                    throw new WrongSingletonUsageException(
                        "Singleton: There is more than one instance of " +
                        typeof(T).Name + " in the scene.");
                _instance = tmp[0];

                return _instance;
            }
            set {
                Debug.LogWarning("Explicitly setting the singleton instance of " + typeof(T).Name + " to " + value);
                _instance = value;
            }
        }

        /// <summary>
        /// Tries to retrieve the singleton instance from the scene or cache.
        /// Returns true and provides the instance as an out parameter if exactly one is retrieved.
        /// </summary>
        public static bool TryGetInstance(out T instance) {
            try {
                instance = Instance;
                return true;
            } catch (WrongSingletonUsageException) {
                instance = null;
                return false;
            }
        }
    }
}