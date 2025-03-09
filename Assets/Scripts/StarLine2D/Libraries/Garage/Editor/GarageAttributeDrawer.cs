using StarLine2D.Controllers;
using UnityEditor;
using UnityEngine;

namespace StarLine2D.Libraries.Garage.Editor
{
    [CustomPropertyDrawer(typeof(GarageAttribute))]
    public class GarageAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (GarageLibrary.I.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            var garageAttribute = (GarageAttribute)attribute;
            
            var currentShip = property.objectReferenceValue as ShipController;
            var currentIndex = GarageLibrary.I.GetIndex(currentShip);
            if (garageAttribute.HasFilter) currentIndex = GarageLibrary.I.GetIndex(currentShip, garageAttribute.Filter);
            
            if (currentIndex == -1) currentIndex = 0;

            var list = GarageLibrary.I.GetAll();
            if (garageAttribute.HasFilter) list = GarageLibrary.I.GetAll(garageAttribute.Filter);

            var options = list.ConvertAll(item => item.Name).ToArray();

            var selectedIndex = EditorGUI.Popup(
                position, 
                label.text, 
                currentIndex, 
                options
            );
            
            property.objectReferenceValue = list[selectedIndex].Prefab;
        }
    }
}