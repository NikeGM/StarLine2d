using UnityEditor;
using UnityEngine;

namespace StarLine2D.Libraries.Icon.Editor
{
    [CustomPropertyDrawer(typeof(IconAttribute))]
    public class IconAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            if (IconsLibrary.I.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var currentColor = property.objectReferenceValue as Sprite;
            var currentIndex = IconsLibrary.I.GetIndex(currentColor);
            if (currentIndex == -1)
            {
                currentIndex = 0;
            }

            var options = IconsLibrary.I.GetAllNames().ToArray();
            var selectedIndex = EditorGUI.Popup(
                position, 
                label.text, 
                currentIndex, 
                options
            );
            property.objectReferenceValue = IconsLibrary.I.GetSprite(selectedIndex);
        }
    }
}