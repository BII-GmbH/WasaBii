#nullable enable

using UnityEngine;

namespace BII.WasaBii.Unity {
    
    /// <summary> Non-generic marker interface for type safety. </summary>
    public interface LazySingleton : Singleton { }

    /// <summary>
    /// Any MonoBehaviour that is a lazy singleton should only be found at most once per scene.
    /// A lazy singleton creates an instance of itself in the scene if there is none.
    /// Enables static access to this single object by using <code>Classname.Instance</code>.
    /// </summary>
    /// <remarks>
    /// Using these is convenient early on, but as a project gets larger,
    ///  you will require a <b>proper, custom dependency system instead</b>.
    /// </remarks>
    /// <typeparam name="T">The implementing type itself</typeparam>
    public abstract class LazySingleton<T> : MonoBehaviour, LazySingleton where T : LazySingleton<T> {
        private static T? _instance;

        public static bool HasInstance => _instance != null;

        /// <summary>
        /// Returns the instance of this singleton.
        /// </summary>
        public static T Instance {
            get {
                if (_instance != null) return _instance;

                var tmp = FindObjectsOfType<T>();

                if (tmp == null || tmp.Length == 0) {
                    var newInstance = new GameObject();
                    _instance = newInstance.AddComponent<T>();
                    newInstance.name = typeof(T).Name + " - Lazy Singleton";
#if UNITY_EDITOR
                    // Using a LazySingleton in an editor script causes an object
                    // to be added to any open scene. Therefore we need to mark all
                    // scenes as dirty, so that they are registered as having unsaved changes.
                    if (!Application.isPlaying)
                        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
#endif
                } else _instance = tmp[0];

                if (tmp != null && tmp.Length > 1)
                    throw new WrongSingletonUsageException(
                        "Singleton: There is more than one instance of " +
                        typeof(T) + " in the scene.");

                return _instance;
            }
            set {
                Debug.LogWarning("Explicitly setting the singleton instance of " + typeof(T).Name + " to " + value);
                _instance = value;
            }
        }
    }
}