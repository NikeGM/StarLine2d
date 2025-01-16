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
        [SerializeField] private ProgressBarWidget enemyHealthBar;

        private GameController _game;
        private ShipController _player;
        private ShipController _enemy;
        private Coroutine _turnCoroutine;
        
        private void Update()
        {
            if (_player && _enemy) return;
            var ships = FindObjectsOfType<ShipController>();

            var player = ships.FirstOrDefault(item => item.IsPlayer);
            if (player != null && playerHealthBar != null)
            {
                playerHealthBar.Watch(player.Health, 0, player.MaxHealth);
            }
            else
            {
                Debug.LogWarning("Player ship or playerHealthBar not found.");
            }
            _player = player;

            var enemy = ships.FirstOrDefault(item => !item.IsPlayer);
            if (enemy != null && enemyHealthBar != null)
            {
                enemyHealthBar.Watch(enemy.Health, 0, enemy.MaxHealth);
            }
            else
            {
                Debug.LogWarning("Enemy ship or enemyHealthBar not found.");
            }
            _enemy = enemy;

            _game = FindObjectOfType<GameController>();
            if (_game == null)
            {
                Debug.LogError("GameController not found in the scene.");
            }
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
        
        private IEnumerator TurnFinished()
        {
            yield return _game.TurnFinished();
            _turnCoroutine = null;
        }
    }
}