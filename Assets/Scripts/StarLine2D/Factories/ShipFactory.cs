using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Models;
using StarLine2D.Components;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace StarLine2D.Controllers
{
    // >>>> Новый enum типа корабля <<<<
    public enum ShipType
    {
        Player,
        Ally,
        Enemy
    }
}

namespace StarLine2D.Controllers
{
    // >>>> Структура для хранения префаба и его типа <<<<
    [Serializable]
    public class ShipPrefabData
    {
        public GameObject prefab;
        public ShipType type;
    }
}


namespace StarLine2D.Controllers
{
    /// <summary>
    /// Класс-фабрика для создания кораблей (игрока, союзников, врагов).
    /// </summary>
    public class ShipFactory
    {
        private readonly List<ShipPrefabData> _allShipPrefabs;
        private readonly int _selectedPlayerIndex;
        private readonly int _numberOfAllies;
        private readonly int _numberOfEnemies;

        public ShipFactory(
            List<ShipPrefabData> allShipPrefabs,
            int selectedPlayerIndex,
            int numberOfAllies,
            int numberOfEnemies
        )
        {
            _allShipPrefabs = allShipPrefabs;
            _selectedPlayerIndex = selectedPlayerIndex;
            _numberOfAllies = numberOfAllies;
            _numberOfEnemies = numberOfEnemies;
        }

        /// <summary>
        /// Основной метод, который спаунит все корабли и возвращает список ShipController.
        /// </summary>
        public List<ShipController> SpawnAllShips(FieldController field)
        {
            var ships = new List<ShipController>();

            // Фильтруем список, чтобы получить отдельные категории
            var playerPrefabs = _allShipPrefabs.Where(p => p.type == ShipType.Player).ToList();
            var allyPrefabs   = _allShipPrefabs.Where(p => p.type == ShipType.Ally).ToList();
            var enemyPrefabs  = _allShipPrefabs.Where(p => p.type == ShipType.Enemy).ToList();

            // Общее количество кораблей: 1 игрок + союзники + враги
            int totalShipsCount = 1 + _numberOfAllies + _numberOfEnemies;

            // Получаем случайные свободные клетки (по количеству кораблей)
            var randomCells = field.GetRandomCells(totalShipsCount);
            if (randomCells.Count < totalShipsCount)
            {
                Debug.LogError("Не хватает свободных клеток, чтобы разместить все корабли!");
                return ships;
            }

            int cellIndex = 0;

            // ----- Спауним игрока -----
            if (playerPrefabs.Count == 0)
            {
                Debug.LogError("В списке allShipPrefabs нет корабля с ShipType.Player!");
                return ships;
            }

            // Берём один из Player-префабов по _selectedPlayerIndex
            int clampedIndex = Mathf.Clamp(_selectedPlayerIndex, 0, playerPrefabs.Count - 1);
            var playerPrefab = playerPrefabs[clampedIndex].prefab;

            var playerCell = randomCells[cellIndex++];
            var playerShipObj = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

            // Привязываем PlayerController и устанавливаем профиль
            var playerShipController = AddControllerAndProfile(
                playerShipObj,
                typeof(PlayerController),
                ShipType.Player
            );

            // Инициализируем позицию
            playerShipController.PositionCell = playerCell;
            playerShipObj.transform.position = GetCellCenter(playerShipController, field);
            playerShipObj.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Инициализация PlayerController
            var playerCtrl = playerShipObj.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Initialize(playerShipController);
            }
            ships.Add(playerShipController);

            // ----- Спауним союзные корабли -----
            for (int i = 0; i < _numberOfAllies; i++)
            {
                if (allyPrefabs.Count == 0)
                {
                    Debug.LogWarning("В списке allShipPrefabs нет кораблей с ShipType.Ally!");
                    break;
                }

                var allyIndex = Random.Range(0, allyPrefabs.Count);
                var allyPrefabGo = allyPrefabs[allyIndex].prefab;

                var allyCell = randomCells[cellIndex++];
                var allyObj = Object.Instantiate(allyPrefabGo, Vector3.zero, Quaternion.identity);

                var allyShipController = AddControllerAndProfile(
                    allyObj,
                    typeof(AllyController),
                    ShipType.Ally
                );

                allyShipController.PositionCell = allyCell;
                allyObj.transform.position = GetCellCenter(allyShipController, field);
                allyObj.transform.rotation = Quaternion.Euler(0, 0, 0);

                var allyCtrl = allyObj.GetComponent<AllyController>();
                if (allyCtrl != null)
                {
                    allyCtrl.Initialize(allyShipController, field, playerShipController);
                }

                ships.Add(allyShipController);
            }

            // ----- Спауним вражеские корабли -----
            for (int i = 0; i < _numberOfEnemies; i++)
            {
                if (enemyPrefabs.Count == 0)
                {
                    Debug.LogWarning("В списке allShipPrefabs нет кораблей с ShipType.Enemy!");
                    break;
                }

                var enemyIndex = Random.Range(0, enemyPrefabs.Count);
                var enemyPrefabGo = enemyPrefabs[enemyIndex].prefab;

                var enemyCell = randomCells[cellIndex++];
                var enemyObj = Object.Instantiate(enemyPrefabGo, Vector3.zero, Quaternion.identity);

                var enemyShipController = AddControllerAndProfile(
                    enemyObj,
                    typeof(EnemyController),
                    ShipType.Enemy
                );

                enemyShipController.PositionCell = enemyCell;
                enemyObj.transform.position = GetCellCenter(enemyShipController, field);
                // Враг изначально «смотрит вниз»
                enemyObj.transform.rotation = Quaternion.Euler(0, 0, 180);

                var enemyCtrl = enemyObj.GetComponent<EnemyController>();
                if (enemyCtrl != null)
                {
                    enemyCtrl.Initialize(enemyShipController, field);
                }

                ships.Add(enemyShipController);
            }

            return ships;
        }

        /// <summary>
        /// Создаёт/добавляет нужный контроллер (Player/Ally/Enemy)
        /// и устанавливает профиль в SpriteCompoundComponent.
        /// </summary>
        private ShipController AddControllerAndProfile(GameObject shipGo, System.Type controllerType, ShipType shipType)
        {
            // Гарантируем наличие ShipController
            var shipController = shipGo.GetComponent<ShipController>();
            if (!shipController)
            {
                shipController = shipGo.AddComponent<ShipController>();
            }

            // Добавляем нужный контроллер (Player/Ally/Enemy), если его нет
            var existingCtrl = shipGo.GetComponent(controllerType);
            if (existingCtrl == null)
            {
                shipGo.AddComponent(controllerType);
            }

            // Устанавливаем профиль на основе ShipType
            var spriteCompound = shipGo.GetComponent<SpriteCompoundComponent>();
            if (spriteCompound != null)
            {
                // Превращаем enum-значение в строку, совпадающую с Id в инспекторе
                // Напр. ShipType.Ally -> "ally"
                var profileId = shipType.ToString().ToLower();

                // Пробуем установить профиль
                if (!spriteCompound.SetProfile(profileId))
                {
                    Debug.LogWarning($"{shipGo.name} не имеет профиля {profileId} в SpriteCompoundComponent!");
                }
            }
            else
            {
                Debug.LogWarning($"{shipGo.name} не имеет SpriteCompoundComponent, не могу сменить профиль!");
            }

            return shipController;
        }

        /// <summary>
        /// Определяем позицию в мире, исходя из ячейки.
        /// </summary>
        private Vector3 GetCellCenter(ShipController ship, FieldController field)
        {
            if (ship.PositionCell == null)
                return Vector3.zero;

            // Переводим Q,R,S -> модель
            var model = new CubeCellModel(
                ship.PositionCell.Q,
                ship.PositionCell.R,
                ship.PositionCell.S
            );

            // Ищем CellController
            var cell = field.FindCellByModel(model);
            if (cell != null)
            {
                return cell.transform.position;
            }

            return Vector3.zero;
        }
    }
}
