using System;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Libraries.Garage;
using StarLine2D.Managers;
using StarLine2D.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StarLine2D.Factories
{
    public class ShipFactory : MonoBehaviour
    {
        [SerializeField] private FieldController field;
        [SerializeField] private List<ShipController> playerPrefabs = new();
        [SerializeField] private int selectedPlayerIndex;
        [SerializeField] private int numberOfAllies = 2;
        [SerializeField] private int numberOfEnemies = 3;

        [Header("Родитель в иерархии (для кораблей)")]
        [SerializeField] private Transform parentShips;

        [Header("PositionManager для проверки формы корабля")]
        [SerializeField] private PositionManager positionManager;

        private readonly List<ShipController> spawnedShips = new();
        private bool isInitialized;

        public ShipController GetPlayerShip()
        {
            spawnedShips.RemoveAll(ship => !ship);
            return spawnedShips.FirstOrDefault(ship => ship.GetComponent<PlayerController>());
        }

        public List<ShipController> GetSpawnedShips()
        {
            spawnedShips.RemoveAll(ship => !ship);
            return spawnedShips;
        }

        public List<ShipController> GetAllies()
        {
            spawnedShips.RemoveAll(ship => !ship);
            return spawnedShips
                .Where(ship => ship.GetComponent<AllyController>())
                .ToList();
        }

        public List<ShipController> GetEnemies()
        {
            spawnedShips.RemoveAll(ship => !ship);
            return spawnedShips
                .Where(ship => ship.GetComponent<EnemyController>())
                .ToList();
        }

        private void Update()
        {
            if (isInitialized) return;
            if (!CheckReadyToSpawn()) return;

            isInitialized = true;
            SpawnAllShips();
        }

        private bool CheckReadyToSpawn()
        {
            var ready = true;

            if (!field)
            {
                Debug.LogError($"[{name}] FieldController (field) не назначен в инспекторе!");
                ready = false;
            }
            else if (field.Cells == null || field.Cells.Count == 0)
            {
                Debug.LogError($"[{name}] FieldController (field) не содержит клеток или список пуст!");
                ready = false;
            }

            if (!positionManager)
            {
                Debug.LogError($"[{name}] PositionManager не назначен в инспекторе!");
                ready = false;
            }

            if (playerPrefabs == null || playerPrefabs.Count == 0)
            {
                Debug.LogError($"[{name}] Список префабов игрока пуст или не назначен!");
                ready = false;
            }

            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerPrefabs.Count)
            {
                Debug.LogError($"[{name}] Индекс выбранного игрока ({selectedPlayerIndex}) вне диапазона!");
                ready = false;
            }

            if (!parentShips)
            {
                Debug.LogError($"[{name}] Родительский Transform для кораблей (parentShips) не назначен!");
                ready = false;
            }

            return ready;
        }

        private void SpawnAllShips()
        {
            SpawnPlayerShip();
            SpawnAllies();
            SpawnEnemies();
        }

        private void SpawnPlayerShip()
        {
            var selectedPrefab = playerPrefabs[selectedPlayerIndex];
            if (!selectedPrefab)
            {
                Debug.LogError($"[{name}] Префаб игрока с индексом {selectedPlayerIndex} не задан.");
                return;
            }

            SpawnOneShip(selectedPrefab, ShipSide.Player);
        }

        private void SpawnAllies()
        {
            for (var i = 0; i < numberOfAllies; i++)
            {
                var allyItem = GarageLibrary.I.GetRandom(GarageItem.ShipType.Ally);
                if (allyItem == null || !allyItem.Prefab)
                {
                    Debug.LogError($"[{name}] Не удалось получить префаб союзника из GarageLibrary!");
                    continue;
                }

                SpawnOneShip(allyItem.Prefab, ShipSide.Ally);
            }
        }

        private void SpawnEnemies()
        {
            for (var i = 0; i < numberOfEnemies; i++)
            {
                var enemyItem = GarageLibrary.I.GetRandom(GarageItem.ShipType.Enemy);
                if (enemyItem == null || !enemyItem.Prefab)
                {
                    Debug.LogError($"[{name}] Не удалось получить префаб врага из GarageLibrary!");
                    continue;
                }

                SpawnOneShip(enemyItem.Prefab, ShipSide.Enemy);
            }
        }

        private void SpawnOneShip(ShipController prefab, ShipSide side)
        {
            if (!prefab)
            {
                Debug.LogError($"[{name}] Не задан префаб корабля для {side}!");
                return;
            }

            var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentShips);
            var shipCtrl = instance.GetComponent<ShipController>();
            if (!shipCtrl)
            {
                shipCtrl = instance.gameObject.AddComponent<ShipController>();
            }

            var possibleCells = positionManager.GetValidHeadCellsForShip(shipCtrl);
            if (possibleCells == null || possibleCells.Count == 0)
            {
                Debug.LogError($"[{name}] Невозможно найти подходящую клетку для корабля ({side}), форма: {shipCtrl.ShipShape}!");
                Destroy(instance);
                return;
            }

            var chosenCell = possibleCells[Random.Range(0, possibleCells.Count)];

            instance.transform.position = chosenCell.transform.position;
            shipCtrl.PositionCell = chosenCell;

            switch (side)
            {
                case ShipSide.Player:
                {
                    var playerCtrl = instance.gameObject.AddComponent<PlayerController>();
                    playerCtrl.Initialize(shipCtrl);
                    break;
                }
                case ShipSide.Ally:
                {
                    var allyCtrl = instance.gameObject.AddComponent<AllyController>();
                    allyCtrl.Initialize(shipCtrl, field);
                    break;
                }
                case ShipSide.Enemy:
                {
                    var enemyCtrl = instance.gameObject.AddComponent<EnemyController>();
                    enemyCtrl.Initialize(shipCtrl, field);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            shipCtrl.Subscribe(() => spawnedShips.Remove(shipCtrl));
            spawnedShips.Add(shipCtrl);
        }

        private enum ShipSide
        {
            Player,
            Ally,
            Enemy
        }
    }
}
