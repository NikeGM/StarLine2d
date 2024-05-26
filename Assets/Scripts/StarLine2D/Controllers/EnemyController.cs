using UnityEngine;

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
        
        public void Shot()
        {
            var ship = GetComponent<ShipController>();
            
            var enemyShotTarget = GetShotCell();
            ship.ShotCell = enemyShotTarget;
        }
        
        private CellController GetShotCell()
        {
            var ship = GetComponent<ShipController>();
            var cell = ship.PositionCell;
            return GetRandomCellInRange(cell, ship.ShootDistance);
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