using System.Collections;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Менеджер «конца хода» (TurnFinished) — осуществляет общий сценарий:
    /// 1) Ход врагов
    /// 2) Ход союзников
    /// 3) Движение
    /// 4) Атака
    /// 5) Коллизии
    /// 6) Очистка астероидов
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        private GameController _gc;
        private MovementManager _movement;
        private AttackManager _attack;
        private CollisionManager _collision;

        public void Init(GameController gameController)
        {
            _gc = gameController;

            // Допустим, найдём «соседние» менеджеры тут же (или вручную назначим).
            _movement = FindObjectOfType<MovementManager>();
            _attack = FindObjectOfType<AttackManager>();
            _collision = FindObjectOfType<CollisionManager>();
        }

        public IEnumerator TurnFinished()
        {
            if (_gc == null)
                yield break;

            // 1) Ход врагов
            var allEnemyShips = _gc.Ships
                .Where(s => s != null && s.GetComponent<EnemyController>() != null)
                .ToList();
            var playerShip = _gc.GetPlayerShip();
            foreach (var eship in allEnemyShips)
            {
                var eCtrl = eship.GetComponent<EnemyController>();
                if (eCtrl)
                {
                    eCtrl.Move();
                    eCtrl.Shot(playerShip);
                }
            }

            // 2) Ход союзников
            var allyShips = _gc.Ships
                .Where(s => s != null && s.GetComponent<AllyController>() != null)
                .ToList();
            foreach (var aShip in allyShips)
            {
                var aCtrl = aShip.GetComponent<AllyController>();
                if (aCtrl)
                {
                    aCtrl.Move();
                    var enemiesForAllies = _gc.Ships
                        .Where(s => s.GetComponent<EnemyController>() != null)
                        .ToList();
                    aCtrl.Shot(enemiesForAllies);
                }
            }

            // 3) Движение (запускаем корутину MovementManager)
            if (_movement)
            {
                yield return StartCoroutine(_movement.MoveAllShipsAndAsteroids());
            }

            // 4) Атака для всех кораблей
            if (_attack)
            {
                foreach (var ship in _gc.Ships)
                {
                    _attack.Shot(ship);
                }
            }

            // 5) Проверяем коллизии
            if (_collision)
            {
                _collision.CheckShipCollisions();
            }

            // 6) Убираем «мертвые» астероиды из списка
            if (_collision)
            {
                _collision.CleanupAsteroids();
            }

            // ... Можно добавить ещё логику, если нужно
        }
    }
}
