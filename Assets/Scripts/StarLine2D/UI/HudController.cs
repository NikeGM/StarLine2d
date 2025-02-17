using System.Collections;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.UI.Widgets;
using StarLine2D.UI.Widgets.ProgressBar;
using UnityEngine;

namespace StarLine2D.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ProgressBarWidget playerHealthBar;
        [SerializeField] private TextWidget playerScore;

        [SerializeField] private ProgressBarWidget enemyHealthBar;
        [SerializeField] private TextWidget enemyScore;

        [SerializeField] private CountdownWidget turnCountdown;

        [SerializeField] private GameObject uiBlocker;

        private GameController _game;
        private InputController _inputController;

        private Coroutine _turnCoroutine;

        private void Update()
        {
            var ships = FindObjectsOfType<ShipController>();

            var player = ships.FirstOrDefault(item => item.IsPlayer);
            if (player && playerHealthBar)
            {
                playerHealthBar.Watch(player.Health);
            }

            if (player && playerScore)
            {
                playerScore.Watch(player.Score);
            }

            var enemy = ships.First(item => !item.IsPlayer);
            if (enemy && enemyHealthBar)
            {
                enemyHealthBar.Watch(enemy.Health);
            }

            if (enemy && enemyScore)
            {
                enemyScore.Watch(enemy.Score);
            }

            if (!_game)
            {
                _game = FindObjectOfType<GameController>();
            }

            if (!_inputController)
            {
                _inputController = FindObjectOfType<InputController>();
            }

            if (turnCountdown) turnCountdown.StartCountdown();
        }

        public void OnPositionClicked()
        {
            _game.OnPositionClicked();
        }

        public void OnWeaponClicked_1()
        {
            _game.OnAttackClicked(0);
        }

        public void OnWeaponClicked_2()
        {
            _game.OnAttackClicked(1);
        }

        public void OnFinishClicked()
        {
            _turnCoroutine ??= StartCoroutine(TurnFinished());
        }

        public void OnPauseClicked()
        {
            if (AnimatedWindow.IsOpen("UI/PauseMenuWindow")) return;

            DisableUI();
            var pauseWindow = AnimatedWindow.OpenUnique("UI/PauseMenuWindow");
            pauseWindow.Subscribe(EnableUI);
        }

        private IEnumerator TurnFinished()
        {
            if (turnCountdown) turnCountdown.Flush();
            DisableUI();

            yield return _game.TurnFinished();

            EnableUI();
            if (turnCountdown) turnCountdown.StartCountdown();
            _turnCoroutine = null;
        }

        private void DisableUI()
        {
            if (uiBlocker) uiBlocker.SetActive(true);
            if (_inputController) _inputController.gameObject.SetActive(false);
        }

        private void EnableUI()
        {
            if (uiBlocker) uiBlocker.SetActive(false);
            if (_inputController) _inputController.gameObject.SetActive(true);
        }
    }
}