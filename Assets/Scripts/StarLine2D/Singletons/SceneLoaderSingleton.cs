using System.Collections;
using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarLine2D.Singletons
{
    public class SceneLoaderSingleton : SingletonMonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            InitLoader();
        }

        private static void InitLoader()
        {
            SceneManager.LoadScene("Loader", LoadSceneMode.Additive);
        }

        [SerializeField] private float transitionTime;

        private GameObject _background;
        
        protected override void Awake()
        {
            base.Awake();
            
            _background = GetComponentInChildren<Image>(true).gameObject;
            _background.SetActive(false);
            
            DontDestroyOnLoad(gameObject);
        }

        public void Show(string sceneName)
        {
            StartCoroutine(StartAnimation(sceneName));
        }

        private IEnumerator StartAnimation(string sceneName)
        {
            _background.SetActive(true);
            yield return new WaitForSeconds(transitionTime);
            SceneManager.LoadScene(sceneName);
            _background.SetActive(false);
        }
    }
}