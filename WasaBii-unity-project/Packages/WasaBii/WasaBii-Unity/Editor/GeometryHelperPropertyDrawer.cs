using BII.WasaBii.Core;
using BII.WasaBii.Geometry;
using BII.WasaBii.Unity.Geometry;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace BII.WasaBii.Unity
{
    /// <summary>
    /// Designed to draw a type that wraps a single value (e.g. a <see cref="GlobalPosition"/> which wraps a
    /// vector) by dropping the field's name so that you get <c>{Pos: {x, y, z}}</c> instead
    /// of <c>{Pos: {AsUnityVector: {x, y, z}}}</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(LocalVelocity))] 
    [CustomPropertyDrawer(typeof(GlobalVelocity))]
    [CustomPropertyDrawer(typeof(LocalOffset))] 
    [CustomPropertyDrawer(typeof(GlobalOffset))]
    [CustomPropertyDrawer(typeof(LocalPosition))] 
    [CustomPropertyDrawer(typeof(GlobalPosition))]
    [CustomPropertyDrawer(typeof(LocalRotation))] 
    [CustomPropertyDrawer(typeof(GlobalRotation))]
    public class GeometryHelperPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            property.Next(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            property.Next(true);
            var ret = EditorGUI.GetPropertyHeight(property, label);
            property.Reset();
            return ret;
        }

        /// <summary>
        /// Like a <see cref="GeometryHelperPropertyDrawer"/> for a vector type where the vector should
        /// be normalized. Since this cannot be easily enforced with the default <see cref="EditorGUI.PropertyField(Rect,SerializedProperty,GUIContent)"/>
        /// and automatically normalizing the value on every change would be bad UX, we supply a button
        /// to manually do it when needed.
        /// </summary>
        [CustomPropertyDrawer(typeof(LocalDirection))] 
        [CustomPropertyDrawer(typeof(GlobalDirection))]
        public sealed class Direction : PropertyDrawer
        {
            private const string buttonLabel = "⚠️Normalize⚠️";
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                EditorGUI.BeginProperty(position, label, property);
                property.Next(true);
                var isNormalized = property.vector3Value.magnitude.IsNearly(1);
                EditorGUI.PropertyField(position, property, label);
                EditorStyles.label.CalcMinMaxWidth(new GUIContent(buttonLabel), out var buttonWidth, out _);
                GUI.contentColor = Color.yellow;
                if (!isNormalized && GUI.Button(new Rect(position) {
                    width = buttonWidth + 10,
                    height = EditorGUIUtility.singleLineHeight,
                    center = position.center.WithY(y => y + EditorGUI.GetPropertyHeight(property, label))
                }, buttonLabel)) {
                    Undo.RecordObject(property.serializedObject.targetObject, "Normalize");
                    property.vector3Value = property.vector3Value.normalized;
                }
                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
                property.Next(true);
                var ret = EditorGUI.GetPropertyHeight(property, label);
                if (!property.vector3Value.magnitude.IsNearly(1)) ret += EditorGUIUtility.singleLineHeight * 1.5f;
                property.Reset();
                return ret;
            }
        }
    }
    
}

#endif