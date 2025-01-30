using StarLine2D.Singletons;
using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarLine2D.Controllers
{
    public class SceneController : MonoBehaviour
    {
        private SceneLoaderSingleton _levelLoader;

        private void Start()
        {
            _levelLoader = SingletonMonoBehaviour.GetInstance<SceneLoaderSingleton>();
        }
        
        public void Reload()
        {
            var scene = SceneManager.GetActiveScene();
            _levelLoader.Show(scene.name);
        }

        public void LoadScene(string sceneName)
        {
            _levelLoader.Show(sceneName);
        }
    }
}