using System.Collections.Generic;
using StarLine2D.Controllers;
using StarLine2D.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StarLine2D.Factories
{
    public class ObstacleFactory : MonoBehaviour
    {
        [SerializeField] private FieldController field;
        [SerializeField] private List<ObstaclePrefabData> prefabs;
        [SerializeField] private int numberOfObstacles = 3;
        [SerializeField] private Transform parentObstacles;
        [SerializeField] private PositionManager positionManager;

        private readonly List<ObstacleController> _spawnedObstacles = new();
        private bool _isInitialized;

        private void Awake()
        {
            if (!field) Debug.LogError($"[{name}] FieldController не назначен.");
            if (prefabs == null || prefabs.Count == 0) Debug.LogError($"[{name}] Список префабов препятствий пуст.");
            if (!parentObstacles) Debug.LogError($"[{name}] Transform parentObstacles не назначен.");
            if (!positionManager) Debug.LogError($"[{name}] PositionManager не назначен.");
        }

        private void Update()
        {
            if (_isInitialized || !CheckReadyToSpawn()) return;
            _isInitialized = true;
            SpawnObstacles();
        }

        public List<ObstacleController> GetSpawnedObstacles()
        {
            _spawnedObstacles.RemoveAll(obstacle => !obstacle);
            return _spawnedObstacles;
        }

        private bool CheckReadyToSpawn()
        {
            return field && field.Cells != null && field.Cells.Count != 0;
        }

        private void SpawnObstacles()
        {
            var allCells = positionManager.GetValidCellsForObstacle();
            if (allCells.Count == 0 || prefabs.Count == 0) return;

            Shuffle(allCells);
            int created = 0;
            for (int i = 0; i < allCells.Count && created < numberOfObstacles; i++)
            {
                var cell = allCells[i];
                var index = Random.Range(0, prefabs.Count);
                var obstacle = Instantiate(prefabs[index].prefab, cell.transform.position, Quaternion.identity, parentObstacles);
                var ctrl = obstacle.GetComponent<ObstacleController>() ?? obstacle.AddComponent<ObstacleController>();
                ctrl.PositionCell = cell;
                ctrl.Subscribe(() => _spawnedObstacles.Remove(ctrl));
                _spawnedObstacles.Add(ctrl);
                created++;
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }
    }
}
