using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class PositionManager : MonoBehaviour
    {
        [SerializeField] private FieldController field;
        [SerializeField] private AsteroidFactory asteroidFactory;
        [SerializeField] private ObstacleFactory obstacleFactory;
        [SerializeField] private ShipFactory shipFactory;

        private void Awake()
        {
            if (!field) Debug.LogError($"[{name}] FieldController не назначен в PositionManager.");
            if (!asteroidFactory) Debug.LogError($"[{name}] AsteroidFactory не назначен в PositionManager.");
            if (!obstacleFactory) Debug.LogError($"[{name}] ObstacleFactory не назначен в PositionManager.");
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory не назначен в PositionManager.");
        }

        public bool IsCellFree(CellController cell)
        {
            if (!cell) return false;
            var obstacles = obstacleFactory.GetSpawnedObstacles();
            var asteroids = asteroidFactory.GetSpawnedAsteroids();
            var allShips = shipFactory.GetSpawnedShips();
            if (obstacles.Any(o => o.PositionCell == cell)) return false;
            if (asteroids.Any(a => a.PositionCell == cell)) return false;
            if (allShips.Any(s => s && s.ShipCellModels.Any(m => m.Q == cell.Q && m.R == cell.R && m.S == cell.S))) return false;
            return true;
        }

        public List<CellController> GetValidHeadCellsForShip(ShipController ship = null)
        {
            var result = new List<CellController>();
            var allCells = field.Cells;
            var obstacles = obstacleFactory.GetSpawnedObstacles();
            var asteroids = asteroidFactory.GetSpawnedAsteroids();
            var allShips = shipFactory.GetSpawnedShips();
            foreach (var candidateCell in allCells)
            {
                if (!IsCellFree(candidateCell)) continue;
                if (ship == null)
                {
                    result.Add(candidateCell);
                    continue;
                }
                var shapeOffsets = ship.GetRelativeShapeOffsets();
                var canPlaceHere = true;
                foreach (var offset in shapeOffsets)
                {
                    var newQ = candidateCell.Q + offset.Q;
                    var newR = candidateCell.R + offset.R;
                    var newS = candidateCell.S + offset.S;
                    var shapeCell = field.FindCellByModel(new CubeCellModel(newQ, newR, newS));
                    if (!shapeCell) { canPlaceHere = false; break; }
                    if (!IsCellFree(shapeCell))
                    {
                        canPlaceHere = false;
                        break;
                    }
                }
                if (canPlaceHere) result.Add(candidateCell);
            }
            return result;
        }

        public List<CellController> GetValidCellsForAsteroid()
        {
            var result = new List<CellController>();
            var allCells = field.Cells;
            foreach (var cell in allCells)
            {
                if (!IsCellFree(cell)) continue;
                result.Add(cell);
            }
            return result;
        }

        public List<CellController> GetValidCellsForObstacle()
        {
            var result = new List<CellController>();
            var allCells = field.Cells;
            foreach (var cell in allCells)
            {
                if (!IsCellFree(cell)) continue;
                result.Add(cell);
            }
            return result;
        }
    }
}
