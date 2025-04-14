using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;

namespace StarLine2D.Utils
{
    /// <summary>
    /// Менеджер позиций (через Inspector задаём ссылки на фабрики и поле).
    /// Позволяет находить клетки, куда можно поставить объект определённой формы
    /// (учитывая препятствия, астероиды, другие корабли и границы поля).
    /// </summary>
    public class PositionManager : MonoBehaviour
    {
        [Header("Зависимости (указать в Inspector)")]
        [SerializeField] private FieldController field;
        [SerializeField] private AsteroidFactory asteroidFactory;
        [SerializeField] private ObstacleFactory obstacleFactory;
        [SerializeField] private ShipFactory shipFactory;

        /// <summary>
        /// Возвращает список клеток, которые могут служить "головой" корабля, учитывая его форму.
        /// В расчёт берутся:
        /// - краевые клетки (если для формы не хватает места, клетка не подходит)
        /// - препятствия
        /// - астероиды
        /// - другие корабли (ShipCellModels)
        /// </summary>
        /// <param name="ship">Корабль (или шаблон корабля), у которого берём форму (ShipShape).</param>
        /// <returns>Список свободных клеток для «головы» такого корабля.</returns>
        public List<CellController> GetValidHeadCellsForShip(ShipController ship)
        {
            // Получаем живые объекты в сцене
            var obstacles = obstacleFactory.GetSpawnedObstacles();  // препятствия
            var asteroids = asteroidFactory.GetSpawnedAsteroids();  // астероиды
            var allShips = shipFactory.GetSpawnedShips();           // все корабли

            var result = new List<CellController>();
            var allCells = field.Cells;

            // Извлекаем относительные смещения формы (GetRelativeShapeOffsets — теперь в ShipController)
            var shapeOffsets = ship.GetRelativeShapeOffsets();

            // Перебираем все клетки поля как потенциальную "голову"
            foreach (var candidateCell in allCells)
            {
                bool canPlaceHere = true;

                // Проверяем каждую клетку формы
                foreach (var offset in shapeOffsets)
                {
                    // Вычисляем координаты ячейки формы
                    int newQ = candidateCell.Q + offset.Q;
                    int newR = candidateCell.R + offset.R;
                    int newS = candidateCell.S + offset.S;

                    // Ищем клетку на поле
                    var shapeCell = field.FindCellByModel(new CubeCellModel(newQ, newR, newS));

                    // Если такой клетки нет — выходим за границы
                    if (shapeCell == null)
                    {
                        canPlaceHere = false;
                        break;
                    }

                    // 1) Проверяем препятствия
                    if (shapeCell.HasObstacle)
                    {
                        canPlaceHere = false;
                        break;
                    }

                    // 2) Проверяем астероиды (сравниваем с PositionCell)
                    if (asteroids.Any(a => a.PositionCell == shapeCell))
                    {
                        canPlaceHere = false;
                        break;
                    }

                    // 3) Проверяем другие корабли
                    //    Если "ship" уже в сцене, и мы просто хотим найти клетки для его перемещения,
                    //    то можно исключить из проверки сам ship (чтобы не мешал себе).
                    //    Но здесь будем считать, что сравниваем со всеми.
                    if (allShips.Any(s =>
                        s != null &&
                        s.ShipCellModels.Any(cellModel =>
                            cellModel.Q == newQ && cellModel.R == newR && cellModel.S == newS)))
                    {
                        canPlaceHere = false;
                        break;
                    }
                }

                if (canPlaceHere)
                {
                    result.Add(candidateCell);
                }
            }

            return result;
        }
    }
}
