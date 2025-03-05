using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarLine2D.Components.Editor
{
    [CustomEditor(typeof(SpriteCompoundComponent))]
    public class SpriteCompoundComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var component = (SpriteCompoundComponent)target;
            serializedObject.Update();
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("sprites"), 
                new GUIContent("Sprites"), 
                true
            );
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Update Sprites"))
            {
                component.UpdateSprites();
                serializedObject.Update();
            }
            
            var currentProfileProp = serializedObject.FindProperty("current");
            var profilesProp = serializedObject.FindProperty("profiles");
            var profileNames = new List<string>();
            for (var i = 0; i < profilesProp.arraySize; i++)
            {
                var item = profilesProp.GetArrayElementAtIndex(i);
                profileNames.Add(item.FindPropertyRelative("id").stringValue);
            }
            if (profileNames.Count == 0) EditorGUILayout.LabelField("No profiles found!");
            else
            {
                var currentIndex = profileNames.IndexOf(currentProfileProp.stringValue);
                if (currentIndex < 0) currentIndex = 0;
                var selectedIndex = EditorGUILayout.Popup(
                    "Current Profile", 
                    currentIndex, 
                    profileNames.ToArray()
                );
                
                currentProfileProp.stringValue = profileNames[selectedIndex];
            }

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("profiles"),
                new GUIContent("Profiles"),
                true
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}