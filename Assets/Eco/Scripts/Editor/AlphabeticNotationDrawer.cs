using System;
using LargeNumbers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Shows a money value as one number (e.g. 2500) instead of its coefficient and magnitude.</summary>
    [CustomPropertyDrawer(typeof(AlphabeticNotation))]
    public class AlphabeticNotationDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var coefficient = property.FindPropertyRelative("coefficient");
            var magnitude = property.FindPropertyRelative("magnitude");

            var field = new DoubleField(property.displayName) { isDelayed = true };
            field.AddToClassList(BaseField<double>.alignedFieldUssClassName);
            field.tooltip = property.tooltip;
            field.SetValueWithoutNotify(ToDouble(coefficient, magnitude));

            field.RegisterValueChangedCallback(evt =>
            {
                var value = new AlphabeticNotation(Math.Max(0d, evt.newValue));
                coefficient.doubleValue = value.coefficient;
                magnitude.intValue = value.magnitude;
                property.serializedObject.ApplyModifiedProperties();
            });

            // Undo and other editors change it too
            field.TrackPropertyValue(coefficient, _ => field.SetValueWithoutNotify(ToDouble(coefficient, magnitude)));
            field.TrackPropertyValue(magnitude, _ => field.SetValueWithoutNotify(ToDouble(coefficient, magnitude)));
            return field;
        }

        private static double ToDouble(SerializedProperty coefficient, SerializedProperty magnitude)
        {
            return coefficient.doubleValue * Math.Pow(1000d, magnitude.intValue);
        }
    }
}
