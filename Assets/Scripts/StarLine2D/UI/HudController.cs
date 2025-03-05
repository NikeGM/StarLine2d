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
            // Ищем все корабли на сцене
            var ships = FindObjectsOfType<ShipController>();

            // Ищем корабль игрока по наличию PlayerController
            var playerShip = ships.FirstOrDefault(s => s.GetComponent<PlayerController>() != null);
            if (playerShip && playerHealthBar)
            {
                playerHealthBar.Watch(playerShip.Health);
            }
            if (playerShip && playerScore)
            {
                playerScore.Watch(playerShip.Score);
            }

            // Ищем первый попавшийся вражеский корабль (EnemyController)
            var enemyShip = ships.FirstOrDefault(s => s.GetComponent<EnemyController>() != null);
            if (enemyShip && enemyHealthBar)
            {
                enemyHealthBar.Watch(enemyShip.Health);
            }
            if (enemyShip && enemyScore)
            {
                enemyScore.Watch(enemyShip.Score);
            }

            // Ищем GameController, если ещё не нашли
            if (!_game)
            {
                _game = FindObjectOfType<GameController>();
            }

            // Ищем InputController, если ещё не нашли
            if (!_inputController)
            {
                _inputController = FindObjectOfType<InputController>();
            }

            // Запускаем анимацию обратного отсчёта (если есть)
            if (turnCountdown) turnCountdown.StartCountdown();
        }

        public void OnPositionClicked()
        {
            if (_game) _game.OnPositionClicked();
        }

        public void OnWeaponClicked_1()
        {
            if (_game) _game.OnAttackClicked(0);
        }

        public void OnWeaponClicked_2()
        {
            if (_game) _game.OnAttackClicked(1);
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

            // Дожидаемся окончания «хода»
            if (_game)
            {
                yield return _game.TurnFinished();
            }

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
