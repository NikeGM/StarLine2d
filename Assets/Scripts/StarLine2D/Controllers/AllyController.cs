using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Models;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Пример простого контроллера для союзного корабля (бота).
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class AllyController : MonoBehaviour
    {
        private ShipController ship;
        private FieldController field;
        private ShipController playerShip;

        /// <summary>
        /// Инициализатор, чтобы передать ссылки на ShipController, FieldController
        /// и при желании корабль игрока (если нужна логика слежения/защиты).
        /// </summary>
        public void Initialize(ShipController ship, FieldController field, ShipController playerShip)
        {
            this.ship = ship;
            this.field = field;
            this.playerShip = playerShip;
        }

        /// <summary>
        /// Вызываем в GameController при завершении хода,
        /// чтобы союзник выбрал, куда перемещаться.
        /// </summary>
        public void Move()
        {
            if (ship == null || field == null) 
            {
                Debug.LogWarning($"AllyController on {gameObject.name} не инициализирован!");
                return;
            }

            // Пример: идём случайно на соседнюю клетку (радиус 1), без препятствий
            var targetCell = GetMoveCell();
            if (targetCell != null)
            {
                ship.MoveCell = targetCell;
            }
        }

        /// <summary>
        /// Выстрел в одного из врагов. Для упрощения берём ближайшего,
        /// затем для каждого оружия выбирается случайная точка в зоне,
        /// куда может переместиться этот противник.
        /// </summary>
        public void Shot(List<ShipController> enemies)
        {
            if (ship == null || field == null || enemies == null || enemies.Count == 0) 
                return;

            var closestEnemy = GetClosestEnemy(enemies);
            if (closestEnemy == null) return;

            if (ship.Weapons.Count == 0) 
                return;

            // Все клетки, куда враг может переместиться
            var potentialMoveCells = field.GetCellsInRange(closestEnemy.PositionCell, closestEnemy.MoveDistance);

            // Для каждого оружия выбираем случайную точку из potentialMoveCells,
            // которая находится в радиусе оружия
            foreach (var weapon in ship.Weapons)
            {
                var inRangeCells = potentialMoveCells
                    .Where(c => field.GetDistance(ship.PositionCell, c) <= weapon.Range)
                    .ToList();

                if (inRangeCells.Count > 0)
                {
                    int randomIndex = Random.Range(0, inRangeCells.Count);
                    var chosenCell = inRangeCells[randomIndex];
                    weapon.ShootCell = chosenCell;
                }
            }
        }

        /// <summary>
        /// Получаем клетку для перемещения. Пример: идём случайно на одну клетку (радиус 1).
        /// </summary>
        private CellController GetMoveCell()
        {
            if (ship == null || ship.PositionCell == null)
                return null;
            if (field == null)
                return null;

            var neighbors = field.GetNeighbors(ship.PositionCell, 1);
            if (neighbors.Count == 0)
                return ship.PositionCell;

            // ФИЛЬТРУЕМ препятствия
            neighbors = neighbors
                .Where(n => !n.HasObstacle)
                .ToList();

            if (neighbors.Count == 0)
            {
                // Нет доступных соседей, остаёмся на месте
                return ship.PositionCell;
            }

            var randomIndex = Random.Range(0, neighbors.Count);
            return neighbors[randomIndex];
        }

        /// <summary>
        /// Находим ближайшего врага.
        /// </summary>
        private ShipController GetClosestEnemy(List<ShipController> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;
            if (ship == null || field == null || ship.PositionCell == null)
                return null;

            ShipController closest = null;
            int minDist = int.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || e.PositionCell == null) 
                    continue;
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
