using System.Collections.Generic;
using StarLine2D.Controllers;
using UnityEngine;

namespace StarLine2D.Factories
{
    public class ObstacleFactory : MonoBehaviour
    {
        [SerializeField] private FieldController field;
        [SerializeField] private List<ObstaclePrefabData> prefabs;
        [SerializeField] private int numberOfObstacles = 3;

        [Header("Родитель в иерархии (для препятствий)")]
        [SerializeField] private Transform parentObstacles;

        private bool _isInitialized = false;

        private void Update()
        {
            if (_isInitialized) return;
            if (CheckReadyToSpawn())
            {
                _isInitialized = true;
                SpawnObstacles();
            }
        }

        private bool CheckReadyToSpawn()
        {
            if (field.Cells == null || field.Cells.Count == 0) return false;
            return true;
        }

        private void SpawnObstacles()
        {
            Debug.Log($"[{name}] ObstacleFactory: начинаем спавн препятствий.");
            var allCells = field.Cells;
            if (allCells.Count == 0 || prefabs.Count == 0)
            {
                Debug.LogWarning($"[{name}] Нет клеток в field или нет префабов!");
                return;
            }
            Shuffle(allCells);
            int obstaclesCreated = 0;
            int cellIndex = 0;
            while (obstaclesCreated < numberOfObstacles && cellIndex < allCells.Count)
            {
                var cell = allCells[cellIndex];
                cellIndex++;
                if (!IsCellFree(cell)) continue;
                var prefabIndex = Random.Range(0, prefabs.Count);
                var obstaclePrefab = prefabs[prefabIndex].prefab;
                var obstacleGO = Instantiate(
                    obstaclePrefab,
                    cell.transform.position,
                    Quaternion.identity,
                    parentObstacles // <-- родитель
                );
                var obstacleCtrl = obstacleGO.GetComponent<ObstacleController>();
                if (obstacleCtrl == null)
                {
                    obstacleCtrl = obstacleGO.AddComponent<ObstacleController>();
                }
                obstacleCtrl.PositionCell = cell;
                cell.SetObstacle(obstacleCtrl);
                obstaclesCreated++;
            }
            Debug.Log($"[{name}] Успешно заспавнено {obstaclesCreated} препятствий.");
        }

        private bool IsCellFree(CellController cell)
        {
            return !cell.HasObstacle;
        }

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
