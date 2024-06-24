using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UsedTexturesFinder
{
    [MenuItem("Tools/Find Used Textures")]
    public static void FindUsedTextures()
    {
        HashSet<string> usedTextures = new HashSet<string>();
        string[] scenes = AssetDatabase.FindAssets("t:Scene");
        string[] prefabs = AssetDatabase.FindAssets("t:Prefab");

        // Process scenes
        foreach (string sceneGUID in scenes)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            EditorUtility.DisplayProgressBar("Processing Scenes", scenePath, 0.5f);
            FindTexturesInScene(scenePath, usedTextures);
        }

        // Process prefabs
        foreach (string prefabGUID in prefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            EditorUtility.DisplayProgressBar("Processing Prefabs", prefabPath, 0.5f);
            FindTexturesInPrefab(prefabPath, usedTextures);
        }

        EditorUtility.ClearProgressBar();

        // Output results
        Debug.Log("Used Textures:");
        foreach (string texturePath in usedTextures)
        {
            Debug.Log(texturePath);
        }
    }

    private static void FindTexturesInScene(string scenePath, HashSet<string> usedTextures)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
            return;

        var dependencies = AssetDatabase.GetDependencies(scenePath);
        foreach (var dependency in dependencies)
        {
            if (dependency.EndsWith(".png") || dependency.EndsWith(".jpg") || dependency.EndsWith(".jpeg"))
            {
                usedTextures.Add(dependency);
            }
        }
    }

    private static void FindTexturesInPrefab(string prefabPath, HashSet<string> usedTextures)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        var dependencies = AssetDatabase.GetDependencies(prefabPath);
        foreach (var dependency in dependencies)
        {
            if (dependency.EndsWith(".png") || dependency.EndsWith(".jpg") || dependency.EndsWith(".jpeg"))
            {
                usedTextures.Add(dependency);
            }
        }
    }
}
