using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Models;
using Random = UnityEngine.Random;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Класс-фабрика для создания больших и малых астероидов.
    /// </summary>
    public class AsteroidFactory
    {
        private readonly GameObject _bigAsteroidPrefab;
        private readonly GameObject _smallAsteroidPrefab;
        private readonly int _numberOfAsteroids;
        private readonly int _minAsteroidHp;
        private readonly int _maxAsteroidHp;
        private readonly float _minAsteroidMass;
        private readonly float _maxAsteroidMass;

        public AsteroidFactory(
            GameObject bigAsteroidPrefab,
            GameObject smallAsteroidPrefab,
            int numberOfAsteroids,
            int minAsteroidHp,
            int maxAsteroidHp,
            float minAsteroidMass,
            float maxAsteroidMass
        )
        {
            _bigAsteroidPrefab = bigAsteroidPrefab;
            _smallAsteroidPrefab = smallAsteroidPrefab;
            _numberOfAsteroids = numberOfAsteroids;
            _minAsteroidHp = minAsteroidHp;
            _maxAsteroidHp = maxAsteroidHp;
            _minAsteroidMass = minAsteroidMass;
            _maxAsteroidMass = maxAsteroidMass;
        }

        /// <summary>
        /// Спауним большие астероиды и возвращаем список AsteroidController.
        /// </summary>
        public List<AsteroidController> SpawnAsteroids(FieldController field, List<ShipController> ships)
        {
            var asteroids = new List<AsteroidController>();

            if (_bigAsteroidPrefab == null)
            {
                Debug.LogWarning("Не задан префаб большого астероида!");
                return asteroids;
            }

            // Список ячеек, где нет препятствий И там нет кораблей
            var freeCells = field.Cells
                .Where(cell => !cell.HasObstacle && !IsCellOccupiedByAnyShip(cell, ships))
                .OrderBy(_ => Random.value)
                .ToList();

            int spawnCount = _numberOfAsteroids;
            if (freeCells.Count < spawnCount)
                spawnCount = freeCells.Count;

            for (int i = 0; i < spawnCount; i++)
            {
                var startCell = freeCells[i];
                var asteroidGo = Object.Instantiate(
                    _bigAsteroidPrefab, 
                    startCell.transform.position, 
                    Quaternion.identity
                );
                var asteroidCtrl = asteroidGo.GetComponent<AsteroidController>();
                if (!asteroidCtrl)
                {
                    Debug.LogWarning("Префаб большого астероида не содержит AsteroidController!");
                    continue;
                }

                // Генерируем случайные параметры
                var randomSize = AsteroidSize.Big;
                var randomHp = Random.Range(_minAsteroidHp, _maxAsteroidHp + 1);
                var randomMass = Random.Range(_minAsteroidMass, _maxAsteroidMass);
                var randomDirection = GetRandomDirection();

                asteroidCtrl.Initialize(
                    randomSize,
                    randomHp,
                    randomMass,
                    startCell,
                    randomDirection
                );

                asteroids.Add(asteroidCtrl);
            }

            return asteroids;
        }

        /// <summary>
        /// Спауним малые астероиды вокруг расколовшегося большого (вызывается из GameController).
        /// </summary>
        public void SpawnSmallAsteroids(
            AsteroidController bigAsteroid,
            FieldController field,
            List<AsteroidController> allAsteroids,
            List<ShipController> ships
        )
        {
            if (_smallAsteroidPrefab == null || bigAsteroid == null) 
                return;
            if (bigAsteroid.Size != AsteroidSize.Big) 
                return;

            var neighbors = field.GetNeighbors(bigAsteroid.PositionCell, 1);
            // Фильтруем, чтобы там не было препятствия, других астероидов и кораблей
            neighbors = neighbors.Where(cell =>
                !cell.HasObstacle &&
                !allAsteroids.Any(a => a && a.PositionCell == cell) &&
                !IsCellOccupiedByAnyShip(cell, ships)
            ).ToList();

            int spawnCount = Random.Range(1, 7);
            if (spawnCount > neighbors.Count)
                spawnCount = neighbors.Count;
            if (spawnCount <= 0) 
                return;

            var randomNeighbors = neighbors
                .OrderBy(_ => Random.value)
                .Take(spawnCount)
                .ToList();

            foreach (var cell in randomNeighbors)
            {
                // Направление = вектор (newCell - oldCell)
                var direction = new CubeCellModel(
                    cell.Q - bigAsteroid.PositionCell.Q,
                    cell.R - bigAsteroid.PositionCell.R,
                    cell.S - bigAsteroid.PositionCell.S
                );

                var smallGo = Object.Instantiate(
                    _smallAsteroidPrefab,
                    cell.transform.position,
                    Quaternion.identity
                );
                var smallCtrl = smallGo.GetComponent<AsteroidController>();

                // Расчёт HP и массы «осколка»
                int smallHp = Mathf.Max(1, bigAsteroid.HP / 10);
                float smallMass = Mathf.Max(0.1f, bigAsteroid.Mass / 10f);

                smallCtrl.Initialize(
                    AsteroidSize.Small,
                    smallHp,
                    smallMass,
                    cell,
                    direction
                );

                allAsteroids.Add(smallCtrl);
            }
        }

        /// <summary>
        /// Возвращает случайное направление (одно из шести) или (0,0,0).
        /// </summary>
        private CubeCellModel GetRandomDirection()
        {
            var possibleDirections = new List<CubeCellModel>
            {
                new CubeCellModel(0, 0, 0),
                new CubeCellModel(1, -1, 0),
                new CubeCellModel(1, 0, -1),
                new CubeCellModel(0, 1, -1),
                new CubeCellModel(-1, 1, 0),
                new CubeCellModel(-1, 0, 1),
                new CubeCellModel(0, -1, 1)
            };

            int index = Random.Range(0, possibleDirections.Count);
            return possibleDirections[index];
        }

        /// <summary>
        /// Проверяет, занята ли клетка `cell` кем-либо из кораблей (Q,R,S).
        /// </summary>
        private bool IsCellOccupiedByAnyShip(CellController cell, List<ShipController> ships)
        {
            foreach (var ship in ships)
            {
                if (ship == null) continue;
                // Перебираем все клеточные модели этого корабля
                foreach (var model in ship.ShipCellModels)
                {
                    if (model.Q == cell.Q && model.R == cell.R && model.S == cell.S)
                        return true;
                }
            }
            return false;
        }
    }
}
