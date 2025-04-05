using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Managers;
using UnityEngine;
using Random = UnityEngine.Random;
using StarLine2D.Models;

namespace StarLine2D.Factories
{
    public class AsteroidFactory : MonoBehaviour
    {
        [SerializeField] private GameObject bigAsteroidPrefab;
        [SerializeField] private GameObject smallAsteroidPrefab;
        [SerializeField] private FieldController field;
        [SerializeField] private int numberOfAsteroids = 5;
        [SerializeField] private int minAsteroidHp = 5;
        [SerializeField] private int maxAsteroidHp = 20;
        [SerializeField] private float minAsteroidMass = 0.5f;
        [SerializeField] private float maxAsteroidMass = 3.0f;
        [SerializeField] private Transform parentAsteroids;
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private PositionManager positionManager;

        private readonly List<AsteroidController> spawnedAsteroids = new();
        private bool isInitialized;

        private void Awake()
        {
            if (!field) Debug.LogError($"[{name}] FieldController не назначен в AsteroidFactory.");
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory не назначен в AsteroidFactory.");
            if (!positionManager) Debug.LogError($"[{name}] PositionManager не назначен в AsteroidFactory.");
        }

        private void Update()
        {
            if (isInitialized) return;
            if (!CheckReadyToSpawn()) return;
            isInitialized = true;
            SpawnAsteroids();
        }

        public List<AsteroidController> GetSpawnedAsteroids()
        {
            spawnedAsteroids.RemoveAll(asteroid => !asteroid);
            return spawnedAsteroids;
        }

        private bool CheckReadyToSpawn()
        {
            if (!field)
            {
                Debug.LogError($"[{name}] FieldController отсутствует.");
                return false;
            }

            if (field.Cells == null)
            {
                Debug.LogError($"[{name}] Список клеток поля не инициализирован.");
                return false;
            }

            if (field.Cells.Count == 0)
            {
                Debug.LogWarning($"[{name}] Список клеток поля пуст, поле ещё не сгенерировано?");
                return false;
            }

            return true;
        }

        private void SpawnAsteroids()
        {
            if (!bigAsteroidPrefab)
            {
                Debug.LogError($"[{name}] Префаб большого астероида не задан.");
                return;
            }

            if (!parentAsteroids)
            {
                Debug.LogWarning($"[{name}] Родитель для астероидов не задан, создаём в корне.");
            }

            var freeCells = positionManager.GetValidCellsForAsteroid();
            if (freeCells.Count == 0)
            {
                Debug.LogWarning($"[{name}] Нет свободных клеток для спавна астероидов.");
                return;
            }

            var spawnCount = Mathf.Min(numberOfAsteroids, freeCells.Count);
            for (var i = 0; i < spawnCount; i++)
            {
                var cell = freeCells[i];
                var asteroidGo = Instantiate(bigAsteroidPrefab, cell.transform.position, Quaternion.identity, parentAsteroids);
                var asteroidCtrl = asteroidGo.GetComponent<AsteroidController>() ?? asteroidGo.AddComponent<AsteroidController>();

                var randomHp = Random.Range(minAsteroidHp, maxAsteroidHp + 1);
                var randomMass = Random.Range(minAsteroidMass, maxAsteroidMass);

                var randomDir = GetRandomDirection(cell);
                asteroidCtrl.Initialize(AsteroidSize.Big, randomHp, randomMass, cell, randomDir);

                asteroidCtrl.Subscribe(() => spawnedAsteroids.Remove(asteroidCtrl));
                spawnedAsteroids.Add(asteroidCtrl);
            }

            Debug.Log($"[{name}] Успешно заспавнено {spawnCount} больших астероидов.");
        }

        public void SpawnSmallAsteroids(AsteroidController bigAsteroid)
        {
            if (!smallAsteroidPrefab)
            {
                Debug.LogError($"[{name}] Префаб маленького астероида не задан.");
                return;
            }

            if (!bigAsteroid)
            {
                Debug.LogError($"[{name}] Не передан контроллер большого астероида.");
                return;
            }

            if (bigAsteroid.Size != AsteroidSize.Big)
            {
                Debug.LogWarning($"[{name}] Попытка деления астероида, который не является большим.");
                return;
            }

            var neighbors = field.GetNeighbors(bigAsteroid.PositionCell, 1)
                .Where(c => positionManager.IsCellFree(c))
                .ToList();

            if (neighbors.Count == 0)
            {
                Debug.LogWarning($"[{name}] Нет свободных соседних клеток для деления большого астероида.");
                return;
            }

            var spawnCount = Random.Range(1, 7);
            spawnCount = Mathf.Min(spawnCount, neighbors.Count);
            if (spawnCount <= 0) return;

            neighbors = neighbors.OrderBy(_ => Random.value).Take(spawnCount).ToList();

            foreach (var cell in neighbors)
            {
                var direction = new CubeCellModel(
                    cell.Q - bigAsteroid.PositionCell.Q,
                    cell.R - bigAsteroid.PositionCell.R,
                    cell.S - bigAsteroid.PositionCell.S
                );

                var smallGo = Instantiate(smallAsteroidPrefab, cell.transform.position, Quaternion.identity, parentAsteroids);
                var smallCtrl = smallGo.GetComponent<AsteroidController>() ?? smallGo.AddComponent<AsteroidController>();

                var smallHp = Mathf.Max(1, bigAsteroid.Hp / 10);
                var smallMass = Mathf.Max(0.1f, bigAsteroid.Mass / 10f);

                smallCtrl.Initialize(AsteroidSize.Small, smallHp, smallMass, cell, direction);
                spawnedAsteroids.Add(smallCtrl);
            }
        }

        private CubeCellModel GetRandomDirection(CellController asteroidCell)
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

            var validDirections = new List<CubeCellModel>();

            foreach (var direction in possibleDirections)
            {
                var newQ = asteroidCell.Q + direction.Q;
                var newR = asteroidCell.R + direction.R;
                var newS = asteroidCell.S + direction.S;

                var cellModel = field.CubeGridModel.FindCellModel(newQ, newR, newS);
                if (cellModel != null)
                {
                    validDirections.Add(direction);
                }
            }

            if (validDirections.Count == 0)
            {
                return new CubeCellModel(0, 0, 0);
            }

            var index = Random.Range(0, validDirections.Count);
            return validDirections[index];
        }
    }
}
