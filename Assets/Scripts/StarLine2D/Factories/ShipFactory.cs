using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using StarLine2D.Components;
using StarLine2D.Libraries.Garage;
using StarLine2D.Models;
using StarLine2D.Utils;
using StarLine2D.Utils.Disposable;

namespace StarLine2D.Controllers
{
    public class ShipFactory : MonoBehaviour
    {
        [SerializeField] private FieldController field;
        [SerializeField] private ShipController playerPrefab;
        [SerializeField] private int numberOfAllies = 2;
        [SerializeField] private int numberOfEnemies = 3;

        [Header("Родитель в иерархии (для кораблей)")]
        [SerializeField] private Transform parentShips;

        [Header("PositionManager для проверки формы корабля")]
        [SerializeField] private PositionManager positionManager;

        private readonly List<ShipController> spawnedShips = new();
        private bool isInitialized = false;

        public ShipController GetPlayerShip()
        {
            // Удаляем «пустые» ссылки
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips.FirstOrDefault(ship => ship.GetComponent<PlayerController>() != null);
        }
        
        public List<ShipController> GetSpawnedShips()
        {
            // Удаляем «пустые» ссылки
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips;
        }

        public List<ShipController> GetAllies()
        {
            // Удаляем «пустые» ссылки
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips
                .Where(ship => ship.GetComponent<AllyController>() != null)
                .ToList();
        }

        public List<ShipController> GetEnemies()
        {
            // Удаляем «пустые» ссылки
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips
                .Where(ship => ship.GetComponent<EnemyController>() != null)
                .ToList();
        }

        private void Update()
        {
            if (isInitialized) return;
            if (CheckReadyToSpawn())
            {
                isInitialized = true;
                SpawnAllShips();
            }
        }

        private bool CheckReadyToSpawn()
        {
            if (field == null) return false;
            if (field.Cells == null || field.Cells.Count == 0) return false;
            if (positionManager == null)
            {
                Debug.LogWarning($"[{name}] ShipFactory: не назначен PositionManager! Нельзя проверить форму корабля.");
                return false;
            }
            return true;
        }

        private void SpawnAllShips()
        {
            Debug.Log($"[{name}] ShipFactory: начинаем спавн кораблей.");

            if (playerPrefab == null)
            {
                Debug.LogWarning($"{name}: Префаб игрока не задан!");
                return;
            }

            // --- Спавн игрока ---
            SpawnOneShip(playerPrefab, ShipSide.Player);

            // --- Спавн союзников ---
            for (int i = 0; i < numberOfAllies; i++)
            {
                // Берём случайный префаб союзного корабля
                var allyPrefab = GarageLibrary.I.GetRandom(GarageItem.ShipType.Ally).Prefab;
                SpawnOneShip(allyPrefab, ShipSide.Ally);
            }

            // --- Спавн врагов ---
            for (int i = 0; i < numberOfEnemies; i++)
            {
                // Берём случайный префаб вражеского корабля
                var enemyPrefab = GarageLibrary.I.GetRandom(GarageItem.ShipType.Enemy).Prefab;
                SpawnOneShip(enemyPrefab, ShipSide.Enemy);
            }

            Debug.Log($"[{name}] Успешно заспавнено {spawnedShips.Count} кораблей.");
        }

        /// <summary>
        /// Вспомогательный метод, который спавнит один корабль (игрок, союзник или враг),
        /// с учётом того, что корабль может занимать две клетки.
        /// </summary>
        private void SpawnOneShip(ShipController prefab, ShipSide side)
        {
            if (prefab == null)
            {
                Debug.LogError($"[{name}] Не задан префаб корабля для {side}!");
                return;
            }

            // 1) Сначала создаём объект (пока ставим позицию (0,0,0))
            var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, parentShips);
            var shipCtrl = instance.GetComponent<ShipController>();
            if (shipCtrl == null)
            {
                shipCtrl = instance.gameObject.AddComponent<ShipController>();
            }

            // 2) С помощью PositionManager ищем все валидные клетки для «головной» клетки этого корабля.
            var possibleCells = positionManager.GetValidHeadCellsForShip(shipCtrl);
            if (possibleCells == null || possibleCells.Count == 0)
            {
                Debug.LogError($"[{name}] Невозможно найти подходящую клетку для корабля ({side}), форма: {shipCtrl.ShipShape}!");
                Destroy(instance);
                return;
            }

            // 3) Случайно выбираем одну из валидных клеток
            var chosenCell = possibleCells[Random.Range(0, possibleCells.Count)];

            // 4) Ставим корабль в эту клетку
            instance.transform.position = chosenCell.transform.position;
            shipCtrl.PositionCell = chosenCell;

            // 5) Дополнительные действия инициализации в зависимости от типа корабля
            switch (side)
            {
                case ShipSide.Player:
                {
                    var playerCtrl = instance.GetComponent<PlayerController>();
                    if (playerCtrl == null)
                    {
                        playerCtrl = instance.gameObject.AddComponent<PlayerController>();
                    }
                    playerCtrl.Initialize(shipCtrl);
                    break;
                }
                case ShipSide.Ally:
                {
                    var allyCtrl = instance.GetComponent<AllyController>();
                    if (allyCtrl == null)
                    {
                        allyCtrl = instance.gameObject.AddComponent<AllyController>();
                    }
                    // Предположим, что союзникам нужно знать поле и корабль-игрок.
                    // Для упрощения возьмём первый попавшийся корабль игрока из списка.
                    var playerShip = GetPlayerShip();
                    allyCtrl.Initialize(shipCtrl, field, playerShip);
                    break;
                }
                case ShipSide.Enemy:
                {
                    var enemyCtrl = instance.GetComponent<EnemyController>();
                    if (enemyCtrl == null)
                    {
                        enemyCtrl = instance.gameObject.AddComponent<EnemyController>();
                    }
                    enemyCtrl.Initialize(shipCtrl, field);
                    break;
                }
            }

            // 6) Подписываемся на уничтожение, чтобы убрать его из списка
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
