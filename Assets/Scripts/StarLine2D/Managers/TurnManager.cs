using System.Collections;
using StarLine2D.Controllers;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private MovementManager movement;
        [SerializeField] private AttackManager attack;
        [SerializeField] private CollisionManager collision;
        [SerializeField] private FieldController fieldController;
        [SerializeField] private ShipFactory shipFactory;

        public IEnumerator TurnFinished()
        {
            // 1) Ход врагов
            var enemyShips = shipFactory.GetEnemies();
            var playerShip = shipFactory.GetPlayerShip();

            // (если у вас гарантированно всегда есть PlayerShip, не нужно проверять его на null)
            foreach (var eship in enemyShips)
            {
                var eCtrl = eship.GetComponent<EnemyController>();
                if (eCtrl)
                {
                    eCtrl.Move();
                    eCtrl.Shot(playerShip);
                }
            }

            // 2) Ход союзников
            var allyShips = shipFactory.GetAllies();
            foreach (var aShip in allyShips)
            {
                var aCtrl = aShip.GetComponent<AllyController>();
                if (aCtrl)
                {
                    aCtrl.Move();
                    // Союзники атакуют выбранного врага (или список врагов)
                    var enemiesForAllies = shipFactory.GetEnemies();
                    aCtrl.Shot(enemiesForAllies);
                }
            }

            // 3) Движение (корутина MovementManager)
            if (movement)
            {
                yield return StartCoroutine(movement.MoveAllShipsAndAsteroids());
            }

            // 4) Атака для всех кораблей (теперь действительно «для всех»)
            if (attack)
            {
                // Собираем всех (игрока, союзников, врагов)
                var allShips = shipFactory.GetSpawnedShips();
                foreach (var s in allShips)
                {
                    if (s)
                    {
                        attack.Shot(s);
                    }
                }
            }

            // 5) Проверяем коллизии
            if (collision)
            {
                collision.CheckShipCollisions();
            }

            // 6) Убираем «мертвые» объекты (если надо)
            if (collision)
            {
                collision.CleanupAsteroids();
                collision.CleanupShips();
            }

            // Сбрасываем статику подсветки клеток (если это нужно)
            var cellsStateManager = fieldController.CellStateManager;
            cellsStateManager.ClearStaticCells();

            // Сбрасываем MoveCell у игрока, чтобы при новом ходе он мог заново выбрать точку перемещения
            if (playerShip != null)
            {
                playerShip.MoveCell = null;
            }
        }
    }
}
