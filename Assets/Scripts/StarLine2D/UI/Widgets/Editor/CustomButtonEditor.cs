using StarLine2D.UI.Widgets.Button;
using UnityEditor;
using UnityEditor.UI;

namespace StarLine2D.UI.Widgets.Editor
{
    [CustomEditor(typeof(CustomButton), true)]
    [CanEditMultipleObjects]
    public class CustomButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("normal"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pressed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disabled"));

            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }
    }
}