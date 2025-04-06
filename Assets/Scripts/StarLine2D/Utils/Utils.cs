using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarLine2D.Utils
{
    public static class Utils
    {
        public static bool IsSceneLoaded(string name)
        {
            var count = SceneManager.sceneCount;
            for (var i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == name) return true;
            }

            return false;
        }

        public static bool LoadSceneIfNotLoaded(string name, LoadSceneMode mode)
        {
            if (IsSceneLoaded(name)) return false;
            
            SceneManager.LoadScene(name, mode);
            return true;
        }

        public static void AddScene(string name, bool unique = true)
        {
            if (unique) LoadSceneIfNotLoaded(name, LoadSceneMode.Additive);
            else SceneManager.LoadScene(name, LoadSceneMode.Additive);
        }
        
        public static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }
    }
}