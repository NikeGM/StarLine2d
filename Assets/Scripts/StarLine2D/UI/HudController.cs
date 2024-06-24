using System.Collections;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.UI.Widgets;
using UnityEngine;

namespace StarLine2D.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ProgressBarWidget playerHealthBar;
        [SerializeField] private TextWidget playerScore;
        
        [SerializeField] private ProgressBarWidget enemyHealthBar;
        [SerializeField] private TextWidget enemyScore;

        [SerializeField] private GameObject uiBlocker;

        private GameController _game;
        private InputController _inputController;
        
        private Coroutine _turnCoroutine;
        
        private void Start()
        {
            var ships = FindObjectsOfType<ShipController>();

            var player = ships.First(item => item.IsPlayer);
            if (player != null && playerHealthBar != null)
            {
                playerHealthBar.Watch(player.Health, 0, player.MaxHealth);
            }

            if (player != null && playerScore != null)
            {
                playerScore.Watch(player.Score);
            }
            
            var enemy = ships.First(item => !item.IsPlayer);
            if (enemy != null && enemyHealthBar != null)
            {
                enemyHealthBar.Watch(enemy.Health, 0, enemy.MaxHealth);
            }

            if (enemy != null && enemyScore != null)
            {
                enemyScore.Watch(enemy.Score);
            }

            _game = FindObjectOfType<GameController>();
            _inputController = FindObjectOfType<InputController>();
        }
        
        public void OnPositionClicked()
        {
            _game.OnPositionClicked();
        }
        
        public void OnAttackClicked()
        {
            _game.OnAttackClicked();
        }

        public void OnFinishClicked()
        {
            _turnCoroutine ??= StartCoroutine(TurnFinished());
        }
        
        private IEnumerator TurnFinished()
        {
            DisableUI();
            
            yield return _game.TurnFinished();
            
            EnableUI();
            _turnCoroutine = null;
        }

        private void DisableUI()
        {
            if (uiBlocker != null) uiBlocker.SetActive(true);
            if (_inputController != null) _inputController.gameObject.SetActive(false);
        }

        private void EnableUI()
        {
            if (uiBlocker != null) uiBlocker.SetActive(false);
            if (_inputController != null) _inputController.gameObject.SetActive(true);
        }
    }
}