using UnityEditor;
using UnityEngine;

namespace StarLine2D.Libraries.Palette.Editor
{
    [CustomPropertyDrawer(typeof(PaletteAttribute))]
    public class PaletteAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Color)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            if (PaletteLibrary.I.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var currentColor = property.colorValue;
            var currentIndex = PaletteLibrary.I.GetIndex(currentColor);
            if (currentIndex == -1)
            {
                currentIndex = PaletteLibrary.I.GetIndex(PaletteLibrary.I.GetDefaultColor());
            }

            var options = PaletteLibrary.I.GetAllNames().ToArray();
            var selectedIndex = EditorGUI.Popup(
                position, 
                label.text, 
                currentIndex, 
                options
            );
            property.colorValue = PaletteLibrary.I.GetColor(selectedIndex);
        }
    }
}