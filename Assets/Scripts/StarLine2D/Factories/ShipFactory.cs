using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using StarLine2D.Components;
using StarLine2D.Libraries.Garage;
using StarLine2D.Models;

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

        private readonly List<ShipController> spawnedShips = new();
        private bool isInitialized = false;

        public ShipController GetPlayerShip()
        {
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips.FirstOrDefault(ship => ship.GetComponent<PlayerController>() != null);
        }
        
        public List<ShipController> GetSpawnedShips()
        {
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips;
        }

        public List<ShipController> GetAllies()
        {
            spawnedShips.RemoveAll(ship => ship == null);
            return spawnedShips
                .Where(ship => ship.GetComponent<AllyController>() != null)
                .ToList();
        }

        public List<ShipController> GetEnemies()
        {
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
            int totalShipsCount = 1 + numberOfAllies + numberOfEnemies;
            var freeCells = field.Cells
                .Where(c => !c.HasObstacle)
                .OrderBy(_ => Random.value)
                .ToList();
            if (freeCells.Count < totalShipsCount)
            {
                Debug.LogError($"Не хватает свободных клеток для {totalShipsCount} кораблей!");
                return;
            }
            int cellIndex = 0;
            
            // --- Спавн игрока ---
            var playerCell = freeCells[cellIndex++];
            var playerInstance = Instantiate(
                playerPrefab,
                playerCell.transform.position,
                Quaternion.identity,
                parentShips // <-- родитель
            );
            var playerGo = playerInstance.gameObject;
            var playerShipCtrl = SetupShipController(playerGo, typeof(PlayerController), GarageItem.ShipType.Player);
            playerShipCtrl.PositionCell = playerCell;
            var playerCtrl = playerGo.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Initialize(playerShipCtrl);
            }
            spawnedShips.Add(playerShipCtrl);

            // --- Спавн союзников ---
            for (int i = 0; i < numberOfAllies; i++)
            {
                var allyCell = freeCells[cellIndex++];
                var allyPrefab = GarageLibrary.I.GetRandom(GarageItem.ShipType.Ally).Prefab;
                var allyInstance = Instantiate(
                    allyPrefab,
                    allyCell.transform.position,
                    Quaternion.identity,
                    parentShips // <-- родитель
                );
                var allyGo = allyInstance.gameObject;
                var allyShipCtrl = SetupShipController(allyGo, typeof(AllyController), GarageItem.ShipType.Ally);
                allyShipCtrl.PositionCell = allyCell;
                var allyCtrl = allyGo.GetComponent<AllyController>();
                if (allyCtrl != null)
                {
                    allyCtrl.Initialize(allyShipCtrl, field, playerShipCtrl);
                }
                spawnedShips.Add(allyShipCtrl);
            }

            // --- Спавн врагов ---
            for (int i = 0; i < numberOfEnemies; i++)
            {
                var enemyCell = freeCells[cellIndex++];
                var enemyPrefab = GarageLibrary.I.GetRandom(GarageItem.ShipType.Enemy).Prefab;
                var enemyInstance = Instantiate(
                    enemyPrefab,
                    enemyCell.transform.position,
                    Quaternion.Euler(0, 0, 180), 
                    parentShips // <-- родитель
                );
                var enemyGo = enemyInstance.gameObject;
                var enemyShipCtrl = SetupShipController(enemyGo, typeof(EnemyController), GarageItem.ShipType.Enemy);
                enemyShipCtrl.PositionCell = enemyCell;
                var enemyCtrl = enemyGo.GetComponent<EnemyController>();
                if (enemyCtrl != null)
                {
                    enemyCtrl.Initialize(enemyShipCtrl, field);
                }
                spawnedShips.Add(enemyShipCtrl);
            }

            Debug.Log($"[{name}] Успешно заспавнено {spawnedShips.Count} кораблей.");
        }

        private ShipController SetupShipController(
            GameObject shipGo,
            System.Type subControllerType,
            GarageItem.ShipType shipType
        )
        {
            var shipController = shipGo.GetComponent<ShipController>();
            if (shipController == null)
            {
                shipController = shipGo.AddComponent<ShipController>();
            }
            var existingSubCtrl = shipGo.GetComponent(subControllerType);
            if (existingSubCtrl == null)
            {
                shipGo.AddComponent(subControllerType);
            }
            var spriteCompound = shipGo.GetComponent<SpriteCompoundComponent>();
            if (spriteCompound != null)
            {
                var profileId = shipType.ToString().ToLower();
                spriteCompound.SetProfile(profileId);
            }
            return shipController;
        }
    }
}
