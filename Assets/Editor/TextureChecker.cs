using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TextureChecker : EditorWindow
    {
        [MenuItem("Tools/Check Textures")]
        public static void ShowWindow()
        {
            GetWindow<TextureChecker>("Texture Checker");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check All Textures"))
            {
                CheckAllTextures();
            }
        }
        
        private void CheckAllTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                
                if (texture)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer)
                    {
                        TextureImporterFormat format = importer.GetPlatformTextureSettings("Android").format;
                        Debug.Log($"Texture: {texture.name}, Format: {format}, Path: {path}");
                        
                        // Добавьте проверку на поддерживаемый формат
                        if (format != TextureImporterFormat.RGBA32 && format != TextureImporterFormat.RGB24 && format != TextureImporterFormat.ARGB32)
                        {
                            Debug.LogWarning($"Unsupported texture format: {format} in texture {texture.name} at path {path}");
                        }
                    }
                }
            }
        }
    }
}