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

        private GameController _game;
        private ShipController _player;
        private ShipController _enemy;
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
            _player = player;
            
            var enemy = ships.First(item => !item.IsPlayer);
            if (enemy != null && enemyHealthBar != null)
            {
                enemyHealthBar.Watch(enemy.Health, 0, enemy.MaxHealth);
            }

            if (enemy != null && enemyScore != null)
            {
                enemyScore.Watch(enemy.Score);
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
            _turnCoroutine ??= StartCoroutine(TurnFinished());
        }
        
        private IEnumerator TurnFinished()
        {
            yield return _game.TurnFinished();
            _turnCoroutine = null;
        }
    }
}