using System.Collections.Generic;
using System.Linq;
using StarLine2D.Managers;
using UnityEngine;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(ShipController))]
    public class AllyController : MonoBehaviour
    {
        private ShipController ship;
        private FieldController field;

        public void Initialize(ShipController shipController, FieldController fieldController)
        {
            ship = shipController;
            field = fieldController;
        }

        public void Move()
        {
            if (!ship || !field)
            {
                Debug.LogError($"AllyController on {gameObject.name} не инициализирован!");
                return;
            }

            var targetCell = GetMoveCell();
            if (targetCell) ship.MoveCell = targetCell;
        }

        public void Shot(List<ShipController> enemies)
        {
            if (enemies.Count == 0) return;
            var closestEnemy = GetClosestEnemy(enemies);
            if (!closestEnemy) return;
            if (ship.Weapons.Count == 0) return;

            var potentialMoveCells = field.GetCellsInRange(
                closestEnemy.PositionCell,
                closestEnemy.MoveDistance
            );

            foreach (var weapon in ship.Weapons)
            {
                var inRangeCells = potentialMoveCells
                    .Where(c => field.GetDistance(ship.PositionCell, c) <= weapon.Range)
                    .ToList();

                if (inRangeCells.Count == 0) continue;
                var randomIndex = Random.Range(0, inRangeCells.Count);
                var chosenCell = inRangeCells[randomIndex];
                weapon.ShootCell = chosenCell;
            }
        }

        private CellController GetMoveCell()
        {
            if (!ship.PositionCell) return null;

            var positionManager = FindObjectOfType<PositionManager>();
            if (!positionManager)
            {
                Debug.LogError("Не удалось найти PositionManager в сцене!");
                return ship.PositionCell;
            }

            var neighbors = field.GetNeighbors(ship.PositionCell, ship.MoveDistance);
            if (neighbors.Count == 0) return ship.PositionCell;

            var freeNeighbors = neighbors.Where(c => positionManager.IsCellFree(c)).ToList();
            if (freeNeighbors.Count == 0)
            {
                Debug.Log($"[AllyController] Нет свободных соседних клеток для {ship.name}.");
                return ship.PositionCell;
            }

            var randomIndex = Random.Range(0, freeNeighbors.Count);
            return freeNeighbors[randomIndex];
        }

        private ShipController GetClosestEnemy(List<ShipController> enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;
            if (!ship.PositionCell) return null;

            ShipController closest = null;
            int minDist = int.MaxValue;

            foreach (var e in enemies)
            {
                if (!e || !e.PositionCell) continue;
                var dist = field.GetDistance(ship.PositionCell, e.PositionCell);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = e;
                }
            }

            return closest;
        }
    }
}
