using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Models;
using StarLine2D.Utils.Disposable;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StarLine2D.Factories
{
    /// <summary>
    /// Фабрика астероидов, которая инициализируется в Update (пока не готовы данные),
    /// а потом один раз спавнит большие астероиды.
    /// </summary>
    public class AsteroidFactory : MonoBehaviour
    {
        [Header("Префабы")]
        [SerializeField] private GameObject bigAsteroidPrefab;
        [SerializeField] private GameObject smallAsteroidPrefab;

        [Header("Настройки количества и параметров")]
        [SerializeField] private FieldController field;
        [SerializeField] private int numberOfAsteroids = 5;
        [SerializeField] private int minAsteroidHp = 5;
        [SerializeField] private int maxAsteroidHp = 20;
        [SerializeField] private float minAsteroidMass = 0.5f;
        [SerializeField] private float maxAsteroidMass = 3.0f;

        [Header("Родитель в иерархии (для астероидов)")]
        [SerializeField] private Transform parentAsteroids;

        private readonly List<AsteroidController> spawnedAsteroids = new();

        private bool isInitialized = false;

        private void Update()
        {
            if (isInitialized) return;

            if (CheckReadyToSpawn())
            {
                isInitialized = true;
                SpawnAsteroids();
            }
        }
        
        public List<AsteroidController> GetSpawnedAsteroids()
        {
            // 1) Удаляем все «пустые» ссылки из списка
            spawnedAsteroids.RemoveAll(asteroid => asteroid == null);

            // 2) Возвращаем «живые» астероиды
            return spawnedAsteroids;
        }


        /// <summary>
        /// Проверяем, "готовы" ли мы спавнить большие астероиды.
        /// </summary>
        private bool CheckReadyToSpawn()
        {
            if (field.Cells == null || field.Cells.Count == 0) return false;
            return true;
        }

        /// <summary>
        /// Основной метод, который создаёт большие астероиды на свободных клетках.
        /// Раньше был в Start(), теперь вызывается из Update() при условии готовности.
        /// </summary>
        private void SpawnAsteroids()
        {
            Debug.Log($"[{name}] AsteroidFactory: начинаем спавн больших астероидов.");

            // Ищем все корабли, если хотим избегать их клеток.
            var allShips = FindObjectsOfType<ShipController>();

            var freeCells = field.Cells
                .Where(cell => !cell.HasObstacle && !IsCellOccupiedByAnyShip(cell, allShips))
                .OrderBy(_ => Random.value)
                .ToList();

            int spawnCount = Mathf.Min(numberOfAsteroids, freeCells.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                var cell = freeCells[i];
                var asteroidGo = Instantiate(
                    bigAsteroidPrefab,
                    cell.transform.position,
                    Quaternion.identity,
                    parentAsteroids // <-- родитель
                );
                var asteroidCtrl = asteroidGo.GetComponent<AsteroidController>();
                if (!asteroidCtrl)
                {
                    // Если отсутствует AsteroidController, добавляем
                    asteroidCtrl = asteroidGo.AddComponent<AsteroidController>();
                }

                int randomHp = Random.Range(minAsteroidHp, maxAsteroidHp + 1);
                float randomMass = Random.Range(minAsteroidMass, maxAsteroidMass);
                var randomDir = GetRandomDirection();

                asteroidCtrl.Initialize(
                    AsteroidSize.Big,
                    randomHp,
                    randomMass,
                    cell,
                    randomDir
                );
                
                asteroidCtrl.Subscribe(() => spawnedAsteroids.Remove(asteroidCtrl));
                spawnedAsteroids.Add(asteroidCtrl);
            }

            Debug.Log($"[{name}] Успешно заспавнено {spawnedAsteroids.Count} больших астероидов.");
        }
        
        public void SpawnSmallAsteroids(AsteroidController bigAsteroid)
        {
            if (smallAsteroidPrefab == null || bigAsteroid == null) return;
            if (bigAsteroid.Size != AsteroidSize.Big) return;

            // Ищем все астероиды и корабли, чтобы не пересекаться с ними
            var allAsteroids = FindObjectsOfType<AsteroidController>();
            var allShips = FindObjectsOfType<ShipController>();

            var neighbors = field.GetNeighbors(bigAsteroid.PositionCell, 1)
                .Where(cell =>
                    !cell.HasObstacle &&
                    !allAsteroids.Any(a => a.PositionCell == cell) &&
                    !IsCellOccupiedByAnyShip(cell, allShips)
                )
                .ToList();

            int spawnCount = Random.Range(1, 7);
            spawnCount = Mathf.Min(spawnCount, neighbors.Count);
            if (spawnCount <= 0) return;

            neighbors = neighbors
                .OrderBy(_ => Random.value)
                .Take(spawnCount)
                .ToList();

            foreach (var cell in neighbors)
            {
                var direction = new CubeCellModel(
                    cell.Q - bigAsteroid.PositionCell.Q,
                    cell.R - bigAsteroid.PositionCell.R,
                    cell.S - bigAsteroid.PositionCell.S
                );

                var smallGo = Instantiate(
                    smallAsteroidPrefab,
                    cell.transform.position,
                    Quaternion.identity,
                    parentAsteroids // <-- родитель для мелких
                );
                var smallCtrl = smallGo.GetComponent<AsteroidController>();
                if (!smallCtrl)
                {
                    smallCtrl = smallGo.AddComponent<AsteroidController>();
                }

                int smallHp = Mathf.Max(1, bigAsteroid.Hp / 10);
                float smallMass = Mathf.Max(0.1f, bigAsteroid.Mass / 10f);

                smallCtrl.Initialize(
                    AsteroidSize.Small,
                    smallHp,
                    smallMass,
                    cell,
                    direction
                );

                spawnedAsteroids.Add(smallCtrl);
            }
        }

        /// <summary>
        /// Проверяем, занята ли клетка кем-либо из кораблей.
        /// </summary>
        private bool IsCellOccupiedByAnyShip(CellController cell, IEnumerable<ShipController> ships)
        {
            foreach (var ship in ships)
            {
                if (ship == null) continue;
                foreach (var model in ship.ShipCellModels)
                {
                    if (model.Q == cell.Q && model.R == cell.R && model.S == cell.S)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Случайное направление (шесть сторон + (0,0,0)).
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
    }
}
