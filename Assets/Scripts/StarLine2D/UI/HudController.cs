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

        [SerializeField] private CountdownWidget turnCountdown;

        [SerializeField] private GameObject uiBlocker;

        private GameController _game;
        private InputController _inputController;

        private Coroutine _turnCoroutine;

        private void Update()
        {
            var ships = FindObjectsOfType<ShipController>();

            var player = ships.FirstOrDefault(item => item.IsPlayer);
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

            if (turnCountdown != null) turnCountdown.StartCountdown();
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
            if (turnCountdown != null) turnCountdown.Flush();
            DisableUI();

            yield return _game.TurnFinished();

            EnableUI();
            if (turnCountdown != null) turnCountdown.StartCountdown();
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