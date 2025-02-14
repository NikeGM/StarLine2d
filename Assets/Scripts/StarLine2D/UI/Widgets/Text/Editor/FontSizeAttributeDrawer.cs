using UnityEditor;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Text.Editor
{
    [CustomPropertyDrawer(typeof(FontSizeAttribute))]
    public class FontSizeAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            if (FontSizesLibrary.I.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var currentSize = property.intValue;
            var currentIndex = FontSizesLibrary.I.GetIndex(currentSize);
            if (currentIndex == -1)
            {
                currentIndex = FontSizesLibrary.I.GetIndex(FontSizesLibrary.I.GetDefaultSize());
            }

            var options = FontSizesLibrary.I.GetAllNames().ToArray();
            var selectedIndex = EditorGUI.Popup(
                position, 
                label.text, 
                currentIndex, 
                options
            );
            property.intValue = FontSizesLibrary.I.GetSize(selectedIndex);
        }
    }
}