using UnityEngine.SceneManagement;

namespace StarLine2D.Utils
{
    public class Utils
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
    }
}