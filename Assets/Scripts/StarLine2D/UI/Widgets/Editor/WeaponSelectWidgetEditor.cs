using StarLine2D.UI.Widgets.WeaponSelect;
using UnityEditor;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Editor
{
    [CustomEditor(typeof(WeaponSelectWidget))]
    public class WeaponSelectWidgetEditor : UnityEditor.Editor 
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var widget = (WeaponSelectWidget)target;

            if (GUILayout.Button("Generate"))
            {
                WeaponSelectWidgetWindow.ShowWindow(widget.gameObject);
            }
        }
    }
    
    public class WeaponSelectWidgetWindow : EditorWindow
    {
        private GameObject target;
        private GameObject weaponOptionPrefab;
        
        private int count = 5;
        private float startAngle = -7.5f;
        private float stepAngle = -15;
        private float radius = 275f;

        public static void ShowWindow(GameObject target)
        {
            var window = GetWindow<WeaponSelectWidgetWindow>("Weapon Select Generator");
            window.target = target;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            target = (GameObject)EditorGUILayout.ObjectField(
                "Target",
                target,
                typeof(GameObject),
                false
            );

            weaponOptionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/WeaponSelect/WeaponOption.prefab");
            weaponOptionPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Weapon Prefab", 
                weaponOptionPrefab, 
                typeof(GameObject), 
                false
            );
            
            count = EditorGUILayout.IntField("Count", count);
            startAngle = EditorGUILayout.FloatField("Start Angle (Z)", startAngle);
            stepAngle = EditorGUILayout.FloatField("Start Angle (Z)", stepAngle);
            radius = EditorGUILayout.FloatField("Start Angle (Z)", radius);

            if (GUILayout.Button("Generate"))
            {
                Generate();
                Close();
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }

        private void Generate()
        {
            if (!weaponOptionPrefab) return;
            
            for (var i = target.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(target.transform.GetChild(i).gameObject);
            }
            
            var angle = startAngle;
            for (var i = 0; i < count; i++)
            {
                var item = PrefabUtility.InstantiatePrefab(
                    weaponOptionPrefab.gameObject, 
                    target.transform
                ) as GameObject;
                
                if (!item) continue;
                
                var offset = Quaternion.Euler(0, 0, angle) * (Vector3.up * radius);
                item.transform.position = target.transform.position + offset;
                item.transform.rotation = Quaternion.Euler(0, 0, angle);
                angle += stepAngle;
            }
        }
    }
}