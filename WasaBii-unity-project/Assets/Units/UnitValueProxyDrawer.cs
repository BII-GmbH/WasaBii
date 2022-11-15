// Cannot be in an editor assembly because the generated editors in WasaBii-Units need to access this.
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using UnityEditor;
using UnityEngine;

namespace BII.WasaBii.UnitSystem {

    /// <summary>
    /// The base class for all <see cref="TUnitValue"/> <see cref="PropertyDrawer"/>s. The
    /// drawers for auto-generated unit value types are also auto-generated to inherit this.
    /// </summary>
    public abstract class ValueWithUnitEditor<TUnitValue, TUnit> : PropertyDrawer 
    where TUnit : IUnit
    where TUnitValue : struct, IUnitValue<TUnitValue, TUnit> {
        
        // For each unit type, the last selected unit (eg m vs km) is cached and displayed the next time an
        // editor of this unit type ist drawn
        private static readonly Dictionary<Type, int> _unitIndices = new();

        protected abstract IUnitDescription<TUnit> description { get; }

        private int unitIndex { 
            get => _unitIndices.GetOrAdd(
                typeof(TUnit),
                () => description.AllUnits.ToList().IndexOf(description.SiUnit)
            );
            set => _unitIndices[typeof(TUnit)] = value; 
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