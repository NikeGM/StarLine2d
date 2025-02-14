using TMPro;
using UnityEditor;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Text.Editor
{
    [CustomPropertyDrawer(typeof(FontAttribute))]
    public class FontAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            if (FontsLibrary.I.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var currentFont = property.objectReferenceValue as TMP_FontAsset;
            var currentIndex = FontsLibrary.I.GetIndex(currentFont);
            if (currentIndex == -1)
            {
                currentIndex = FontsLibrary.I.GetIndex(FontsLibrary.I.GetDefaultFont());
            }

            var options = FontsLibrary.I.GetAllNames().ToArray();
            var selectedIndex = EditorGUI.Popup(
                position, 
                label.text, 
                currentIndex, 
                options
            );
            property.objectReferenceValue = FontsLibrary.I.GetFont(selectedIndex);
        }
    }
}