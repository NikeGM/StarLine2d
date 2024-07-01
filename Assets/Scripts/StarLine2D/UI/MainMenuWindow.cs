using System;
using StarLine2D.Singletons;
using StarLine2D.Utils;
using UnityEngine;

namespace StarLine2D.UI
{
    public class MainMenuWindow : AnimatedWindow
    {
        private SceneLoaderSingleton _levelLoader;
        private Action _action;

        protected override void Start()
        {
            base.Start();

            _levelLoader = SingletonMonoBehaviour.GetInstance<SceneLoaderSingleton>();
        }

        // public void OnSettings()
        // {
        //     Open("UI/SettingsWindow");
        // }
        //
        // public void OnLanguage()
        // {
        //     Open("UI/LanguageWindow");
        // }

        public void OnStart()
        {
            _action = () => _levelLoader.Show("SampleScene");
            Close();
        }

        public void OnExit()
        {
            _action = Application.Quit;
#if UNITY_EDITOR                
            _action += () => UnityEditor.EditorApplication.isPlaying = false;
#endif

            Close();
        }

        public override void AnimationEventClose()
        {
            _action?.Invoke();
            base.AnimationEventClose();
        }
    }
}