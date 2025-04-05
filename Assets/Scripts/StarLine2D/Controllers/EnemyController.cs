using StarLine2D.Managers;
using UnityEngine;
using StarLine2D.Models;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(ShipController))]
    public class EnemyController : MonoBehaviour
    {
        private ShipController ship;
        private FieldController field;
        private PositionManager positionManager;

        private void Awake()
        {
            positionManager = FindObjectOfType<PositionManager>();
            if (!positionManager)
            {
                Debug.LogError($"[{name}] Не удалось найти PositionManager в сцене.");
            }
        }

        public void Initialize(ShipController ship, FieldController field)
        {
            if (!ship) Debug.LogError($"[{name}] ShipController не передан в EnemyController.Initialize.");
            if (!field) Debug.LogError($"[{name}] FieldController не передан в EnemyController.Initialize.");
            this.ship = ship;
            this.field = field;
        }

        public void Move()
        {
            if (!ship || !field || !positionManager)
            {
                Debug.LogError($"[{name}] EnemyController не инициализирован корректно.");
                return;
            }
            var targetCell = GetMoveCell();
            if (targetCell != null) ship.MoveCell = targetCell;
        }

        public void Shot(ShipController targetShip)
        {
            if (!ship || !field || !targetShip) return;
            if (ship.Weapons.Count == 0) return;
            var potentialMoveCells = field.GetCellsInRange(targetShip.PositionCell, targetShip.MoveDistance);
            foreach (var weapon in ship.Weapons)
            {
                var inRangeCells = potentialMoveCells
                    .FindAll(c => field.GetDistance(ship.PositionCell, c) <= weapon.Range);
                if (inRangeCells.Count > 0)
                {
                    var randomIndex = Random.Range(0, inRangeCells.Count);
                    weapon.ShootCell = inRangeCells[randomIndex];
                }
            }
        }

        private CellController GetMoveCell()
        {
            if (!ship || !ship.PositionCell) return null;
            if (!field || !positionManager) return null;
            var neighbors = field.GetNeighbors(ship.PositionCell, 1);
            if (neighbors.Count == 0) return ship.PositionCell;
            neighbors = neighbors.FindAll(n => positionManager.IsCellFree(n));
            if (neighbors.Count == 0) return ship.PositionCell;
            var randomIndex = Random.Range(0, neighbors.Count);
            return neighbors[randomIndex];
        }
    }
}
