using UnityEngine;
using System.Linq;

namespace StarLine2D.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        private FieldController _field;


        public void Initialize(CellController cell, FieldController fieldController)
        {
            _field = fieldController;
        }

        public void Move()
        {
            var ship = GetComponent<ShipController>();

            var enemyMoveTarget = GetMoveCell();
            ship.MoveCell = enemyMoveTarget;
        }

        public void Shot(ShipController playerShip)
        {
            var ship = GetComponent<ShipController>();
            foreach (var shipWeapon in ship.Weapons)
            {
                var enemyShotTarget = GetShotCell(playerShip, shipWeapon.Range);
                shipWeapon.ShootCell = enemyShotTarget;
            }
        }

        private CellController GetShotCell(ShipController playerShip, int distance)
        {
            var ship = GetComponent<ShipController>();
            var cell = ship.PositionCell;
            var canShootCells = _field.GetNeighbors(cell, distance);
            var enemyNewPositionCells = _field.GetNeighbors(playerShip.PositionCell, playerShip.MoveDistance);

            var intersectingCells = canShootCells.Intersect(enemyNewPositionCells).ToList();

            if (intersectingCells.Count == 0)
            {
                return null;
            }

            var randomIndex = Random.Range(0, intersectingCells.Count);
            return intersectingCells[randomIndex];
        }

        private CellController GetMoveCell()
        {
            var ship = GetComponent<ShipController>();
            var cell = ship.PositionCell;
            return GetRandomCellInRange(cell, ship.MoveDistance);
        }

        private CellController GetRandomCellInRange(CellController centerCell, int radius)
        {
            var neighbors = _field.GetNeighbors(centerCell, radius);
            if (neighbors.Count == 0)
            {
                return null;
            }

            var randomIndex = Random.Range(0, neighbors.Count);
            return neighbors[randomIndex];
        }
    }
}