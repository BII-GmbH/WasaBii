using System;
using BII.WasaBii.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BII.WasaBii.Unity {
    
    /// Contains functions to avoid error-prone compiler conditions 
    /// when programming optional behavior in non-editor code that 
    /// should affect the editor and to reduce repetition.
    public static class EditorRuntimeHelper {
        
        /// Can be called in non-editor code to mark the scene dirty
        /// if in edit mode. This exists to avoid the error-prone 
        /// compiler conditions in normal code and to avoid repetition.
        public static void IfInEditorMarkScenesDirty() {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorSceneManager.MarkAllScenesDirty();
            #endif
        }

        /// Can be called in non-editor code to mark any number of objects dirty
        /// if in edit mode. 
        /// This is especially important on instances of prefabs,
        /// because otherwise the prefab may override some of the objects values.
        /// This exists to avoid the error-prone 
        /// compiler conditions in normal code and to avoid repetition.
        public static void IfInEditorMarkObjectsDirty(params UnityEngine.Object[] objects) {
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                objects.ForEach(EditorUtility.SetDirty);
            }
            #endif
        }

        public static void DoOnlyInPlayMode(Action action) {
            #if !UNITY_EDITOR
            action();
            #else
            if (Application.isPlaying)
                action();
            #endif
        }
        
        public static void DoOnlyInEditMode(Action action) {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                action();
            #endif
        }

        public static void IfInPlayMode(Action then, Action elseAction) {
            #if !UNITY_EDITOR
            then();
            #else
            if (Application.isPlaying)
                then();
            else 
                elseAction();
            #endif
        }
    }
}