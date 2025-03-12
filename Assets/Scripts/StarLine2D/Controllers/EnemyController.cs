using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Models;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Пример простого AI для вражеского корабля.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class EnemyController : MonoBehaviour
    {
        private ShipController ship;
        private FieldController field;

        /// <summary>
        /// Инициализатор, чтобы передать ссылки на ShipController и FieldController.
        /// </summary>
        public void Initialize(ShipController ship, FieldController field)
        {
            this.ship = ship;
            this.field = field;
        }

        /// <summary>
        /// Вызываем этот метод в GameController при завершении хода,
        /// чтобы враг выбрал, куда перемещаться.
        /// </summary>
        public void Move()
        {
            if (ship == null || field == null) 
            {
                Debug.LogWarning($"EnemyController on {gameObject.name} не инициализирован!");
                return;
            }

            var targetCell = GetMoveCell();
            if (targetCell != null)
            {
                ship.MoveCell = targetCell;
            }
        }

        /// <summary>
        /// Выстрел в корабль игрока (или другую цель) таким образом, что для каждого оружия
        /// выбирается случайная точка в зоне, куда может переместиться этот противник.
        /// </summary>
        public void Shot(ShipController targetShip)
        {
            if (ship == null || field == null || targetShip == null) 
                return;

            if (ship.Weapons.Count == 0) 
                return;

            // Получаем все клетки в радиусе перемещения цели (targetShip.MoveDistance)
            // из которых цель может начать следующий ход
            var potentialMoveCells = field.GetCellsInRange(targetShip.PositionCell, targetShip.MoveDistance);

            // Для каждого оружия выбираем случайную клетку из potentialMoveCells,
            // которая находится в пределах дальности выстрела оружия
            foreach (var weapon in ship.Weapons)
            {
                // Фильтруем клетки по радиусу действия оружия
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
        /// Ищем клетку для перемещения: к примеру, выбираем любую соседнюю клетку (радиус 1),
        /// но без препятствий.
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
                // Нет доступных соседних клеток, остаёмся на месте
                return ship.PositionCell;
            }

            var randomIndex = Random.Range(0, neighbors.Count);
            return neighbors[randomIndex];
        }
    }
}
