﻿// Cannot be in an editor assembly because the generated editors in WasaBii-Units need to access this.
#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BII.WasaBii.UnitSystem {

    /// <summary>
    /// The base class for all <see cref="TUnitValue"/> <see cref="PropertyDrawer"/>s. The
    /// drawers for auto-generated unit value types are also auto-generated to inherit this.
    /// </summary>
    public abstract class ValueWithUnitEditor<TUnitValue> : PropertyDrawer 
    where TUnitValue : struct, IUnitValue<TUnitValue, IUnit<TUnitValue>> {
        
        // For each unit type, the last selected unit (eg m vs km) is cached and displayed the next time an
        // editor of this unit type ist drawn
        // ReSharper disable once StaticMemberInGenericType // intentional, related to the generics
        private static int _unitIndex = -1;

        protected abstract IUnitDescription<IUnit<TUnitValue>> description { get; }

        private int unitIndex { 
            get => _unitIndex == -1 ? _unitIndex = description.AllUnits.ToList().IndexOf(description.SiUnit) : _unitIndex;
            set => _unitIndex = value;
        }

        private const string serializedPropertyName = "_siValue";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(position, label);

            property = property.FindPropertyRelative(serializedPropertyName);
            var value = new TUnitValue{SiValue = property.doubleValue};
            var newValue = unitField(label, value, position);
            if (Math.Abs(newValue.SiValue - value.SiValue) > double.Epsilon) {
                Undo.RecordObject(property.serializedObject.targetObject, property.name);
                property.doubleValue = newValue.SiValue;
            }

            EditorGUI.EndProperty();
        }

        private TUnitValue unitField(GUIContent label, TUnitValue value, Rect position) {
            position.height = EditorGUIUtility.singleLineHeight;
            var xMin = position.xMin;
            var popupWidth = position.width / 8;
            position.xMin = position.xMax - popupWidth;
            position.width = popupWidth;
            // Unity automatically treats slashes in the popup options as a sign to generate sub-menus, so we
            // replace it with another unicode fraction slash character. :mad_sob:
            unitIndex = EditorGUI.Popup(position, unitIndex, description.AllUnits.Select(u => u.ShortName.Replace("/", "∕")).ToArray());
            var xMax = position.xMin;
            position.xMin = xMin;
            position.xMax = xMax;
            var unit = description.AllUnits[unitIndex];
            var newValue = Math.Max(0, EditorGUI.DoubleField(position, label, value.As(unit)));
            return new TUnitValue{SiValue = newValue * unit.SiFactor};
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }
    
}

#endif