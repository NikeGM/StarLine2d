using System.Linq;
using StarLine.Controllers;
using StarLine.UI.Widgets;
using UnityEngine;

namespace StarLine.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ProgressBarWidget playerHealthBar;
        [SerializeField] private ProgressBarWidget enemyHealthBar;

        private GameController _game;
        private ShipController _player;
        private ShipController _enemy;
        private void Start()
        {
            var ships = FindObjectsOfType<ShipController>();

            var player = ships.First(item => item.IsPlayer);
            if (player != null && playerHealthBar != null)
            {
                playerHealthBar.Watch(player.Health);
            }
            _player = player;
            
            var enemy = ships.First(item => !item.IsPlayer);
            if (enemy != null && enemyHealthBar != null)
            {
                enemyHealthBar.Watch(enemy.Health);
            }
            _enemy = enemy;

            _game = FindObjectOfType<GameController>();
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
            StartCoroutine(_game.TurnFinished());

        }
    }
}