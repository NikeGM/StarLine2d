using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Components; // Для CellController

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Фабрика, отвечающая за спаун статических препятствий.
    /// </summary>
    public class ObstacleFactory
    {
        private readonly List<ObstaclePrefabData> _prefabs;
        private readonly int _numberOfObstacles;

        public ObstacleFactory(List<ObstaclePrefabData> prefabs, int numberOfObstacles)
        {
            _prefabs = prefabs;
            _numberOfObstacles = numberOfObstacles;
        }

        /// <summary>
        /// Создаём нужное кол-во препятствий на поле (в случайных свободных клетках).
        /// </summary>
        public List<ObstacleController> SpawnObstacles(FieldController field)
        {
            var result = new List<ObstacleController>();

            // Получаем все клетки поля (или напишите свой метод поиска случайных клеток).
            var allCells = field.Cells;
            if (allCells.Count == 0 || _prefabs.Count == 0)
                return result;

            // Перемешаем список клеток (чтобы брать из него рандомно) 
            // или можно просто многократно random pick по индексу.
            Shuffle(allCells);

            int obstaclesCreated = 0;
            int cellIndex = 0;

            // Пока не поставим нужное число препятствий или не кончатся клетки
            while (obstaclesCreated < _numberOfObstacles && cellIndex < allCells.Count)
            {
                var cell = allCells[cellIndex];
                cellIndex++;

                // Если клетка уже занята (или рядом и т.д.) — пропускаем.
                // Но поскольку это самое первое, можно считать, что ещё не занята ничем, кроме резерва на будущее.
                // Если у вас есть логика "IsCellFree", можно проверить её.
                if (!IsCellFree(cell)) 
                {
                    continue;
                }

                // Выбираем случайный префаб.
                var prefabIndex = Random.Range(0, _prefabs.Count);
                var obstaclePrefab = _prefabs[prefabIndex].prefab;

                // Создаём объект
                var obstacleGO = Object.Instantiate(obstaclePrefab, cell.transform.position, Quaternion.identity);
                var obstacleCtrl = obstacleGO.GetComponent<ObstacleController>();
                if (obstacleCtrl == null)
                {
                    obstacleCtrl = obstacleGO.AddComponent<ObstacleController>();
                }

                obstacleCtrl.PositionCell = cell;
                // Помечаем клетку, что в ней есть препятствие
                cell.SetObstacle(obstacleCtrl);

                result.Add(obstacleCtrl);
                obstaclesCreated++;
            }

            return result;
        }

        // Проверка, свободна ли клетка — примитивная (если хотите, допишите сложную).
        private bool IsCellFree(CellController cell)
        {
            // Если у CellController есть флаг/ссылка "Obstacle", "Ship" или т.д., 
            // проверяем, что там ничего не занято
            if (cell.HasObstacle) return false;
            // Если вы хотите не ставить препятствие, где, например, уже корабль,
            // или чтобы корабли/астероиды потом не вставали туда —
            // можно расширить логику, например: if (cell.HasShip) return false;
            return true;
        }

        // Служебный метод перемешивания (Fisher-Yates)
        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }
    }
}
