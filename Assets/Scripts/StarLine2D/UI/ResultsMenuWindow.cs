using System;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Singletons;
using StarLine2D.UI.Widgets;
using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarLine2D.UI
{
    public class ResultsMenuWindow : AnimatedWindow
    {
        [SerializeField] private TextWidget scoreWidget;
        
        private SceneLoaderSingleton _levelLoader;
        private Action _action;

        private float _defaultTimeScale;

        protected override void Start()
        {
            base.Start();
            
            _defaultTimeScale = Time.timeScale;
            Time.timeScale = 0;
            
            _levelLoader = SingletonMonoBehaviour.GetInstance<SceneLoaderSingleton>();

            if (scoreWidget == null) return;
            
            var ships = FindObjectsOfType<ShipController>();
            var player = ships.First(item => item.IsPlayer);

            if (player == null) return;

            scoreWidget.Watch(player.Score);
        }

        public void OnRestart()
        {
            var scene = SceneManager.GetActiveScene();
            _action = () => _levelLoader.Show(scene.name);
            Close();
        }

        public void OnExit()
        {
            _action = () => _levelLoader.Show("Scenes/MainMenu");
            Close();
        }

        public override void AnimationEventClose()
        {
            _action?.Invoke();
            base.AnimationEventClose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Time.timeScale = _defaultTimeScale;
        }
    }
}