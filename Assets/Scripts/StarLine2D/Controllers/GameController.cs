using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.Models;
using StarLine2D.Utils.Disposable;
using UnityEngine;
using StarLine2D.Components;  // Чтобы можно было использовать SpriteCompoundComponent

namespace StarLine2D.Controllers
{
    // >>>> Новый enum типа корабля <<<<
    public enum ShipType
    {
        Player,
        Ally,
        Enemy
    }

    // >>>> Структура для хранения префаба и его типа <<<<
    [System.Serializable]
    public class ShipPrefabData
    {
        public GameObject prefab;
        public ShipType type;
    }

    public class GameController : MonoBehaviour
    {
        [SerializeField] private FieldController field;

        // -------------------------------------------------------
        // Теперь один список всех кораблей, 
        // где у каждого есть ShipType = Player / Ally / Enemy
        // -------------------------------------------------------
        [SerializeField] private List<ShipPrefabData> allShipPrefabs;

        // Индекс, какой именно префаб игрока брать среди тех, что помечены Player
        [SerializeField] private int selectedPlayerIndex = 0;

        // Количество вражеских и союзных кораблей
        [SerializeField] private int numberOfEnemies = 3;
        [SerializeField] private int numberOfAllies = 2;

        [SerializeField] private bool debugAlwaysHit;

        // Фиксированная длительность движения (в секундах)
        [SerializeField] private float moveDuration = 2.0f;

        private List<ShipController> _ships = new();
        private readonly CompositeDisposable _trash = new();
        private CellsStateController _cellsStateController;
        private int _currentWeapon = -1;

        // -------------------------------------------------------
        // Поля для генерации и хранения астероидов
        // -------------------------------------------------------
        [Header("Астероиды")] 
        [SerializeField] private GameObject bigAsteroidPrefab;   // Префаб большого
        [SerializeField] private GameObject smallAsteroidPrefab; // Префаб маленького

        [SerializeField] private int numberOfAsteroids = 5;      // Количество генерируемых больших астероидов
        [SerializeField] private int minAsteroidHp = 5;          // Минимальное HP
        [SerializeField] private int maxAsteroidHp = 20;         // Максимальное HP
        [SerializeField] private float minAsteroidMass = 0.5f;   // Минимальная масса
        [SerializeField] private float maxAsteroidMass = 3.0f;   // Максимальная масса

        private readonly List<AsteroidController> _asteroids = new();

        private void Awake()
        {
            // Загрузка сцены "Hud" (при наличии такой сцены)
            Utils.Utils.AddScene("Hud");
        }

        public void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
            _cellsStateController = field.GetComponent<CellsStateController>();
            
            // Спауним корабль игрока, несколько союзников и врагов
            SpawnAllShips();

            // После инициализации поля и кораблей создаём астероиды
            SpawnAsteroids();
        }

        /// <summary>
        /// Спауним все корабли: игрока, союзников и врагов — всё из единого списка allShipPrefabs.
        /// </summary>
        private void SpawnAllShips()
        {
            // Фильтруем список, чтобы получить отдельные категории
            var playerPrefabs = allShipPrefabs.Where(p => p.type == ShipType.Player).ToList();
            var allyPrefabs   = allShipPrefabs.Where(p => p.type == ShipType.Ally).ToList();
            var enemyPrefabs  = allShipPrefabs.Where(p => p.type == ShipType.Enemy).ToList();

            // Общее количество кораблей: 1 игрок + союзники + враги
            int totalShipsCount = 1 + numberOfAllies + numberOfEnemies;

            // Получаем случайные свободные клетки (по количеству кораблей)
            var randomCells = field.GetRandomCells(totalShipsCount);
            if (randomCells.Count < totalShipsCount)
            {
                Debug.LogError("Не хватает свободных клеток, чтобы разместить все корабли!");
                return;
            }

            _ships = new List<ShipController>();
            int cellIndex = 0;

            // ----- Спауним игрока -----
            if (playerPrefabs.Count == 0)
            {
                Debug.LogError("В списке allShipPrefabs нет корабля с ShipType.Player!");
                return;
            }

            // Берём один из Player-префабов по selectedPlayerIndex
            int clampedIndex = Mathf.Clamp(selectedPlayerIndex, 0, playerPrefabs.Count - 1);
            var playerPrefab = playerPrefabs[clampedIndex].prefab;
            
            var playerCell = randomCells[cellIndex++];
            var playerShipObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

            // Добавляем PlayerController и ставим цвет (Blue)
            var playerShipController = AddControllerAndColor(
                playerShipObj, 
                typeof(PlayerController), 
                ShipType.Player
            );

            // Инициализируем позицию
            playerShipController.PositionCell = playerCell;
            playerShipObj.transform.position = GetShapeCenter(playerShipController);
            playerShipObj.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Инициализация PlayerController
            var playerCtrl = playerShipObj.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Initialize(playerShipController);
            }
            _ships.Add(playerShipController);

            // ----- Спауним союзные корабли (numberOfAllies штук) -----
            for (int i = 0; i < numberOfAllies; i++)
            {
                if (allyPrefabs.Count == 0)
                {
                    Debug.LogWarning("В списке allShipPrefabs нет кораблей с ShipType.Ally!");
                    break;
                }

                var allyIndex = Random.Range(0, allyPrefabs.Count);
                var allyPrefabGo = allyPrefabs[allyIndex].prefab;

                var allyCell = randomCells[cellIndex++];
                var allyObj = Instantiate(allyPrefabGo, Vector3.zero, Quaternion.identity);

                // Добавляем AllyController и ставим цвет (Green)
                var allyShipController = AddControllerAndColor(
                    allyObj, 
                    typeof(AllyController), 
                    ShipType.Ally
                );

                allyShipController.PositionCell = allyCell;
                allyObj.transform.position = GetShapeCenter(allyShipController);
                allyObj.transform.rotation = Quaternion.Euler(0, 0, 0);

                var allyCtrl = allyObj.GetComponent<AllyController>();
                if (allyCtrl != null)
                {
                    allyCtrl.Initialize(allyShipController, field, playerShipController);
                }

                _ships.Add(allyShipController);
            }

            // ----- Спауним вражеские корабли (numberOfEnemies штук) -----
            for (int i = 0; i < numberOfEnemies; i++)
            {
                if (enemyPrefabs.Count == 0)
                {
                    Debug.LogWarning("В списке allShipPrefabs нет кораблей с ShipType.Enemy!");
                    break;
                }

                var enemyIndex = Random.Range(0, enemyPrefabs.Count);
                var enemyPrefabGo = enemyPrefabs[enemyIndex].prefab;

                var enemyCell = randomCells[cellIndex++];
                var enemyObj = Instantiate(enemyPrefabGo, Vector3.zero, Quaternion.identity);

                // Добавляем EnemyController и ставим цвет (Red)
                var enemyShipController = AddControllerAndColor(
                    enemyObj, 
                    typeof(EnemyController), 
                    ShipType.Enemy
                );

                enemyShipController.PositionCell = enemyCell;
                enemyObj.transform.position = GetShapeCenter(enemyShipController);
                // Начальный угол — "смотрит вниз"
                enemyObj.transform.rotation = Quaternion.Euler(0, 0, 180);

                var enemyCtrl = enemyObj.GetComponent<EnemyController>();
                if (enemyCtrl != null)
                {
                    enemyCtrl.Initialize(enemyShipController, field);
                }

                _ships.Add(enemyShipController);
            }
        }

        /// <summary>
        /// Метод, который добавляет к объекту нужный контроллер (если его нет),
        /// а также меняет цвет через SpriteCompoundComponent.
        /// </summary>
        private ShipController AddControllerAndColor(GameObject shipGo, System.Type controllerType, ShipType color)
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

            // Ставим цвет
            var spriteCompound = shipGo.GetComponent<SpriteCompoundComponent>();
            if (spriteCompound != null)
            {
                // Пытаемся установить профиль (blue/green/red)
                string profileId = color.ToString().ToLower(); // "blue", "green", "red"
                if (!spriteCompound.SetProfile(profileId))
                {
                    Debug.LogWarning($"{shipGo.name} не имеет профиля {profileId} в SpriteCompoundComponent!");
                }
            }
            else
            {
                Debug.LogWarning($"{shipGo.name} не имеет SpriteCompoundComponent, не могу сменить цвет!");
            }

            return shipController;
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }

        public void OnPositionClicked()
        {
            var player = GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            player.FlushShoots();
            player.MoveCell = null;
            _cellsStateController.ClearStaticCells();
            _cellsStateController.SetZone(player.PositionCell, player.MoveDistance, CellsStateController.MoveZone, "");
        }

        public void OnAttackClicked(int index)
        {
            var player = GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }
            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }

            var weapon = player.Weapons[index];
            var shapeCells = GetShapeCells(player.ShipShape, player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке (выход за границы?)");
                return;
            }

            _cellsStateController.SetWeaponZoneForFuturePosition(shapeCells, weapon.Range, weapon.Type.ToString());
            _currentWeapon = index;
        }

        public IEnumerator TurnFinished()
        {
            _cellsStateController.ClearZone();
            _cellsStateController.ClearStaticCells();

            // Удаление уничтоженных объектов (кораблей и астероидов)
            _ships = _ships.Where(s => s != null && s.gameObject != null).ToList();
            _asteroids.RemoveAll(a => a == null || a.gameObject == null);

            // 1) Ход врагов (AI-логика)
            var allEnemyShips = _ships
                .Where(s => s.GetComponent<EnemyController>() != null)
                .ToList();
            var playerShip = GetPlayerShip();
            foreach (var eship in allEnemyShips)
            {
                var eCtrl = eship.GetComponent<EnemyController>();
                if (eCtrl)
                {
                    eCtrl.Move();
                    eCtrl.Shot(playerShip);
                }
            }

            // 2) Ход союзников
            var allyShips = _ships
                .Where(s => s.GetComponent<AllyController>() != null)
                .ToList();
            foreach (var aShip in allyShips)
            {
                var aCtrl = aShip.GetComponent<AllyController>();
                if (aCtrl)
                {
                    aCtrl.Move();
                    var enemiesForAllies = _ships
                        .Where(s => s.GetComponent<EnemyController>() != null)
                        .ToList();
                    aCtrl.Shot(enemiesForAllies);
                }
            }

            // 3) Движение всех кораблей
            List<Coroutine> movementCoroutines = new List<Coroutine>();
            foreach (var ship in _ships)
            {
                movementCoroutines.Add(StartCoroutine(MoveShip(ship)));
            }

            // Параллельно двигаются астероиды
            foreach (var asteroid in _asteroids)
            {
                if (asteroid != null)
                {
                    movementCoroutines.Add(StartCoroutine(
                        asteroid.MoveSmoothlyWithHalfTurn(field, moveDuration)
                    ));
                }
            }

            foreach (var cor in movementCoroutines)
            {
                yield return cor;
            }

            // 4) Атака для всех кораблей
            foreach (var ship in _ships)
            {
                Shot(ship);
            }

            // 5) Проверяем коллизии кораблей
            for (int i = 0; i < _ships.Count; i++)
            {
                for (int j = i + 1; j < _ships.Count; j++)
                {
                    var s1 = _ships[i];
                    var s2 = _ships[j];
                    if (s1.PositionCell == s2.PositionCell && s1.PositionCell != null)
                    {
                        OnShipCollision(s1, s2);
                    }
                }
            }

            // 6) Убираем «мертвые» астероиды из списка
            CleanupAsteroids();
        }

        private IEnumerator MoveShip(ShipController ship)
        {
            if (ship == null || ship.MoveCell == null)
                yield break;

            var oldCenter = GetShapeCenter(ship);
            var newCell = ship.MoveCell;
            ship.MoveCell = null;
            ship.PositionCell = newCell;

            var newCenter = GetShapeCenter(ship);

            // Поворот корабля к цели
            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return SmoothMove(ship, oldCenter, newCenter, moveDuration);

            // Игрок смотрит вверх (0), остальные - вниз (180)
            var playerCtrl = ship.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                ship.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                ship.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }

        private IEnumerator SmoothMove(ShipController ship, Vector3 startPos, Vector3 endPos, float duration)
        {
            if (duration <= 0.0001f)
            {
                ship.transform.position = endPos;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ship.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            ship.transform.position = endPos;
        }

        private void Shot(ShipController ship)
        {
            if (ship == null) return;

            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell) 
                    continue;

                var shipCells = GetShipCells(ship);
                CellController closestCell = null;
                int minDistance = int.MaxValue;

                // Ищем ближайшую ячейку формы корабля к ячейке выстрела
                foreach (var cell in shipCells)
                {
                    int dist = field.GetDistance(cell, shipWeapon.ShootCell);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestCell = cell;
                    }
                }

                if (closestCell == null) continue;

                // Проверяем дальность
                if (minDistance > shipWeapon.Range) continue;

                // Формируем набор клеток, куда идёт выстрел
                var shootCells = new HashSet<CellController>();
                if (shipWeapon.Type == WeaponType.Point)
                {
                    shootCells.Add(shipWeapon.ShootCell);
                }
                else if (shipWeapon.Type == WeaponType.Beam)
                {
                    var cellsOnLine = field.GetLine(closestCell, shipWeapon.ShootCell);
                    if (cellsOnLine.Count > 0 && cellsOnLine[0] == closestCell)
                    {
                        cellsOnLine.RemoveAt(0);
                    }
                    foreach (var c in cellsOnLine)
                        shootCells.Add(c);
                }

                // Анимация
                shootCells.ToList().ForEach(c => c.ShotAnimation());

                // Наносим урон
                var damagedShips = new HashSet<ShipController>();
                foreach (var cell in shootCells)
                {
                    // Урон по кораблю
                    if (HasShip(cell))
                    {
                        var damagedShip = GetShip(cell);
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var resultedDamage = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
                                // Попал в самого себя
                                ship.AddScore(-resultedDamage);
                            }
                            else
                            {
                                // Попал в другого
                                ship.AddScore(resultedDamage);
                                damagedShips.Add(damagedShip);
                            }
                        }
                    }
                    else
                    {
                        // Урон по астероиду
                        var asteroid = _asteroids.FirstOrDefault(a => a.PositionCell == cell);
                        if (asteroid != null)
                        {
                            var damageDone = asteroid.OnDamage(shipWeapon.Damage);
                            // Немного очков, если астероид выжил, но получил урон
                            if (damageDone > 0 && asteroid.HP > 0)
                            {
                                ship.AddScore(5);
                            }
                        }
                    }
                }

                // Сброс ShootCell
                shipWeapon.ShootCell = null;
            }
        }

        private void OnShipCollision(ShipController ship1, ShipController ship2)
        {
            if (ship1 == null || ship2 == null) return;

            int ship1Hp = ship1.Health.Value;
            int ship2Hp = ship2.Health.Value;

            ship1.OnDamage(ship2Hp);
            ship2.OnDamage(ship1Hp);
        }

        public List<CellController> GetShipCells(ShipController ship)
        {
            var result = new List<CellController>();
            var modelCells = ship.ShipCellModels;

            foreach (var model in modelCells)
            {
                var controller = field.FindCellByModel(model);
                if (controller != null)
                {
                    result.Add(controller);
                }
            }

            return result;
        }

        private Vector3 GetShapeCenter(ShipController ship)
        {
            var models = ship.ShipCellModels;
            if (models.Count == 0)
                return ship.transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var m in models)
            {
                var cell = field.FindCellByModel(m);
                if (cell != null)
                {
                    sum += cell.transform.position;
                    count++;
                }
            }

            if (count == 0) return ship.transform.position;
            return sum / count;
        }

        private bool HasShip(CellController cell)
        {
            return _ships.Any(item => item.PositionCell == cell);
        }

        private ShipController GetShip(CellController cell)
        {
            return _ships.Find(item => item.PositionCell == cell);
        }

        public ShipController GetPlayerShip()
        {
            return _ships.Find(item => item != null && item.GetComponent<PlayerController>() != null);
        }

        private void OnCellClicked(GameObject go)
        {
            if (go == field.gameObject) return;
            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;

            // Зона перемещения
            if (_cellsStateController.Zone.Type == CellsStateController.MoveZone)
            {
                var player = GetPlayerShip();
                if (player == null) return;

                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: часть корабля окажется за границей поля!");
                    return;
                }

                for (int i = 0; i < shapeCells.Count; i++)
                {
                    _cellsStateController.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateController.MoveActiveProfile);
                }

                player.MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            // Зона атаки
            if (_currentWeapon != -1 && _cellsStateController.Zone.Type == CellsStateController.WeaponZone)
            {
                _cellsStateController.AddStaticCell(_currentWeapon.ToString(), cell, CellsStateController.WeaponActiveProfile);
                var player = GetPlayerShip();
                if (player)
                {
                    player.Weapons[_currentWeapon].ShootCell = cell;
                }
                _cellsStateController.ClearZone();
            }
        }

        // --------------------------------------------------------------------
        // Метод, возвращающий форму корабля (1 или 2 клетки) или пустой список,
        // если часть формы выходит за границы поля.
        // --------------------------------------------------------------------
        public List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (headCell == null)
                return new List<CellController>();

            var result = new List<CellController> { headCell };

            switch (shape)
            {
                case ShipShape.Single:
                    // Одноклеточный корабль
                    break;

                case ShipShape.HorizontalR:
                {
                    var modelLeft = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                    var leftCell = field.FindCellByModel(modelLeft);
                    if (leftCell == null)
                        return new List<CellController>();
                    result.Add(leftCell);
                    break;
                }
                case ShipShape.HorizontalL:
                {
                    var modelRight = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                    var rightCell = field.FindCellByModel(modelRight);
                    if (rightCell == null)
                        return new List<CellController>();
                    result.Add(rightCell);
                    break;
                }
            }

            return result;
        }

        private void SpawnAsteroids()
        {
            if (bigAsteroidPrefab == null)
            {
                Debug.LogWarning("Не задан префаб большого астероида (bigAsteroidPrefab)!");
                return;
            }

            var freeCells = field.Cells.Where(cell =>
                    !_asteroids.Any(a => a.PositionCell == cell)
                    && !_ships.Any(s => s.PositionCell == cell)
                )
                .OrderBy(_ => Random.value)
                .ToList();

            int spawnCount = numberOfAsteroids;
            if (freeCells.Count < spawnCount)
                spawnCount = freeCells.Count;

            for (int i = 0; i < spawnCount; i++)
            {
                var startCell = freeCells[i];
                var asteroidGo = Instantiate(bigAsteroidPrefab, startCell.transform.position, Quaternion.identity);
                var asteroidCtrl = asteroidGo.GetComponent<AsteroidController>();
                if (!asteroidCtrl)
                {
                    Debug.LogWarning("Префаб большого астероида не содержит AsteroidController!");
                    continue;
                }

                var randomSize = AsteroidSize.Big;
                var randomHp = Random.Range(minAsteroidHp, maxAsteroidHp + 1);
                var randomMass = Random.Range(minAsteroidMass, maxAsteroidMass);
                var randomDirection = GetRandomDirection();

                asteroidCtrl.Initialize(
                    randomSize,
                    randomHp,
                    randomMass,
                    startCell,
                    randomDirection
                );

                _asteroids.Add(asteroidCtrl);
            }
        }

        private void CleanupAsteroids()
        {
            for (int i = _asteroids.Count - 1; i >= 0; i--)
            {
                if (!_asteroids[i])
                {
                    _asteroids.RemoveAt(i);
                }
            }
        }

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

        public void SpawnSmallAsteroids(AsteroidController bigAsteroid)
        {
            if (!smallAsteroidPrefab || !bigAsteroid) return;
            if (bigAsteroid.Size != AsteroidSize.Big) return;

            var neighbors = field.GetNeighbors(bigAsteroid.PositionCell, 1);
            neighbors = neighbors.Where(cell =>
                !_asteroids.Any(a => a && a.PositionCell == cell) &&
                _ships.All(s => s.PositionCell != cell)).ToList();

            int spawnCount = Random.Range(1, 7);
            if (spawnCount > neighbors.Count)
                spawnCount = neighbors.Count;
            if (spawnCount <= 0) return;

            var randomNeighbors = neighbors.OrderBy(_ => Random.value).Take(spawnCount).ToList();

            foreach (var cell in randomNeighbors)
            {
                var direction = new CubeCellModel(
                    cell.Q - bigAsteroid.PositionCell.Q,
                    cell.R - bigAsteroid.PositionCell.R,
                    cell.S - bigAsteroid.PositionCell.S
                );

                var smallGo = Instantiate(smallAsteroidPrefab, cell.transform.position, Quaternion.identity);
                var smallCtrl = smallGo.GetComponent<AsteroidController>();

                int smallHp = Mathf.Max(1, bigAsteroid.HP / 10);
                float smallMass = Mathf.Max(0.1f, bigAsteroid.Mass / 10f);
  
                smallCtrl.Initialize(
                    AsteroidSize.Small,
                    smallHp,
                    smallMass,
                    cell,
                    direction
                );

                _asteroids.Add(smallCtrl);
            }
        }
    }
}
