using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.Models;
using StarLine2D.Utils.Disposable;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private FieldController field;

        // Списки префабов для игрока и врагов
        [SerializeField] private List<GameObject> playerPrefabs;
        [SerializeField] private List<GameObject> enemyPrefabs;

        // Индексы выбранных префабов в списках (можно менять в инспекторе)
        [SerializeField] private int selectedPlayerIndex;
        [SerializeField] private int selectedEnemyIndex;

        [SerializeField] private bool debugAlwaysHit;

        // Фиксированная длительность движения (в секундах) - применяется для кораблей и астероидов
        [SerializeField] private float moveDuration = 2.0f;

        private List<ShipController> _ships = new();
        private readonly CompositeDisposable _trash = new();
        private CellsStateController _cellsStateController;
        private int _currentWeapon = -1;

        // -------------------------------------------------------
        // Поля для генерации и хранения астероидов
        // -------------------------------------------------------

        // --- Изменено: используем bigAsteroidPrefab для больших
        [Header("Астероиды")]
        [SerializeField] private GameObject bigAsteroidPrefab;  // Префаб большого
        [SerializeField] private GameObject smallAsteroidPrefab; // Префаб маленького

        [SerializeField] private int numberOfAsteroids = 5; // Количество генерируемых больших астероидов
        [SerializeField] private int minAsteroidHp = 5;     // Минимальное HP
        [SerializeField] private int maxAsteroidHp = 20;    // Максимальное HP
        [SerializeField] private float minAsteroidMass = 0.5f; // Минимальная масса
        [SerializeField] private float maxAsteroidMass = 3.0f; // Максимальная масса

        // Храним все созданные астероиды, чтобы управлять ими в конце раунда
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
            _ships = SetPlayersShips();
            _cellsStateController = field.GetComponent<CellsStateController>();

            // После инициализации поля и кораблей создаём астероиды
            SpawnAsteroids();
        }

        /// <summary>
        /// Создаём корабль игрока и корабль врага в случайных клетках.
        /// </summary>
        private List<ShipController> SetPlayersShips()
        {
            // Берём 2 случайные клетки на поле
            var randomCells = field.GetRandomCells(2);
            var playerCell = randomCells[0];
            var enemyCell = randomCells[1];

            // Инстанцируем корабли (изначально в (0,0))
            var playerPrefab = playerPrefabs[selectedPlayerIndex];
            var enemyPrefab = enemyPrefabs[selectedEnemyIndex];
            var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            var enemy = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);

            var shipsList = new List<ShipController>();
            var playerShipController = player.GetComponent<ShipController>();
            var enemyShipController = enemy.GetComponent<ShipController>();
            var enemyController = enemy.GetComponent<EnemyController>();

            shipsList.Add(playerShipController);
            shipsList.Add(enemyShipController);

            // Устанавливаем "головные" клетки
            playerShipController.PositionCell = playerCell;
            enemyShipController.PositionCell = enemyCell;

            // Ставим корабль в центр занимаемых клеток
            player.transform.position = GetShapeCenter(playerShipController);
            enemy.transform.position = GetShapeCenter(enemyShipController);

            // Начальные углы
            player.transform.rotation = Quaternion.Euler(0, 0, 0);
            enemy.transform.rotation = Quaternion.Euler(0, 0, 180);

            // Инициализация врага
            enemyController.Initialize(enemyCell, field);

            return shipsList;
        }

        /// <summary>
        /// При уничтожении объекта освобождаем подписки.
        /// </summary>
        private void OnDestroy()
        {
            _trash.Dispose();
        }

        /// <summary>
        /// Клик "Position" - выбираем перемещение.
        /// </summary>
        public void OnPositionClicked()
        {
            var player = GetPlayerShip();
            player.FlushShoots();
            player.MoveCell = null;
            _cellsStateController.ClearStaticCells();
            _cellsStateController.SetZone(player.PositionCell, player.MoveDistance, CellsStateController.MoveZone, "");
        }

        /// <summary>
        /// Клик "Attack" (Weapon 0 / 1 / 2...). Ставим зону выстрела.
        /// </summary>
        public void OnAttackClicked(int index)
        {
            var player = GetPlayerShip();
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

        /// <summary>
        /// Вызывается, когда игрок закончил выбор перемещения/атаки и нужно завершить ход.
        /// </summary>
        public IEnumerator TurnFinished()
        {
            _cellsStateController.ClearZone();
            _cellsStateController.ClearStaticCells();

            var playerShip = GetPlayerShip();
            var enemyShip = GetEnemyShip();
            var enemyController = enemyShip.GetComponent<EnemyController>();

            enemyController?.Move();
            enemyController?.Shot(playerShip);

            var isCollision = enemyShip.MoveCell == playerShip.MoveCell;

            // Список корутин для одновременного движения
            List<Coroutine> movementCoroutines = new List<Coroutine>
            {
                // Корабли двигаются по старой логике
                StartCoroutine(MoveShip(playerShip)),
                StartCoroutine(MoveShip(enemyShip))
            };

            // Астероиды двигаются по новой логике — с пол-оборотом
            foreach (var asteroid in _asteroids)
            {
                if (asteroid != null)
                {
                    movementCoroutines.Add(StartCoroutine(
                        asteroid.MoveSmoothlyWithHalfTurn(field, moveDuration)
                    ));
                }
            }

            // Ждём, пока все закончат
            foreach (var cor in movementCoroutines)
            {
                yield return cor;
            }

            // Далее идёт логика выстрелов, коллизий и т.д.
            Shot(playerShip);
            Shot(enemyShip);
            if (isCollision)
            {
                OnShipCollision(playerShip, enemyShip);
            }

            CleanupAsteroids();
        }

        /// <summary>
        /// Плавное перемещение корабля ship.
        /// Аналогично перемещению астероидов, но со своей логикой поворота.
        /// </summary>
        private IEnumerator MoveShip(ShipController ship)
        {
            if (ship.MoveCell == null)
                yield break;

            var oldCenter = GetShapeCenter(ship);
            var newCell = ship.MoveCell;
            ship.MoveCell = null;
            ship.PositionCell = newCell;

            var newCenter = GetShapeCenter(ship);

            // Вращение корабля на угол, соответствующий направлению движения
            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Плавная интерполяция
            yield return SmoothMove(ship, oldCenter, newCenter, moveDuration);

            // Возвращаем стандартный угол
            ship.transform.rotation = Quaternion.Euler(0, 0, ship.IsPlayer ? 0 : 180);
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

        /// <summary>
        /// Атака (Point/Beam). Проверяем корабли и астероиды.
        /// </summary>
        private void Shot(ShipController ship)
        {
            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell)
                    continue;

                var shipCells = GetShipCells(ship);
                CellController closestCell = null;
                int minDistance = int.MaxValue;

                foreach (var cell in shipCells)
                {
                    int dist = field.GetDistance(cell, shipWeapon.ShootCell);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestCell = cell;
                    }
                }

                if (closestCell == null)
                    continue;

                // Проверяем дальность
                if (minDistance - 1 > shipWeapon.Range)
                    continue;

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

                // Анимация попадания и урон
                shootCells.ToList().ForEach(c => c.ShotAnimation());

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
                                ship.AddScore(-resultedDamage);
                            }
                            else
                            {
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
                            if (damageDone > 0 && asteroid.HP > 0)
                            {
                                ship.AddScore(5);
                            }
                        }
                    }
                }

                shipWeapon.ShootCell = null;
            }
        }

        private void OnShipCollision(ShipController ship1, ShipController ship2)
        {
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
            return _ships.Find(item => item.IsPlayer);
        }

        private ShipController GetEnemyShip()
        {
            return _ships.Find(item => !item.IsPlayer);
        }

        private void OnCellClicked(GameObject go)
        {
            Debug.Log(go);
            if (go == field.gameObject) return;

            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;

            // Если зона перемещения
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
                    _cellsStateController.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateController.MovePoint);
                }

                player.MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            // Если зона атаки
            if (_currentWeapon != -1 && _cellsStateController.Zone.Type == CellsStateController.WeaponZone)
            {
                _cellsStateController.AddStaticCell(_currentWeapon.ToString(), cell, CellsStateController.ShootPoint);
                GetPlayerShip().Weapons[_currentWeapon].ShootCell = cell;
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

        // -------------------------------------------------------
        // Генерация больших астероидов (Big)
        // -------------------------------------------------------
        private void SpawnAsteroids()
        {
            if (bigAsteroidPrefab == null)
            {
                Debug.LogWarning("Не задан префаб большого астероида (bigAsteroidPrefab)!");
                return;
            }

            for (int i = 0; i < numberOfAsteroids; i++)
            {
                var randomCells = field.GetRandomCells(1);
                if (randomCells.Count == 0) break;
                var startCell = randomCells[0];

                var asteroidGo = Instantiate(bigAsteroidPrefab, startCell.transform.position, Quaternion.identity);
                var asteroidCtrl = asteroidGo.GetComponent<AsteroidController>();
                if (asteroidCtrl == null)
                {
                    Debug.LogWarning("Префаб большого астероида не содержит AsteroidController!");
                    continue;
                }

                var randomSize = AsteroidSize.Big; // По задаче мы хотим сгенерировать большие
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

        /// <summary>
        /// Удаляем из списка все астероиды, которые были уничтожены (null).
        /// </summary>
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

        /// <summary>
        /// Возвращает одно из 7 направлений (6 соседних + "стоять на месте") в куб. координатах.
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

        // -------------------------------------------------------
        // Спавним от 1 до 6 маленьких астероидов вокруг уничтоженного большого
        // (в свободные соседние клетки).
        // -------------------------------------------------------
        public void SpawnSmallAsteroids(AsteroidController bigAsteroid)
        {
            if (!smallAsteroidPrefab || !bigAsteroid) return;
            if (bigAsteroid.Size != AsteroidSize.Big) return;

            // Находим соседние клетки (радиус 1)
            var neighbors = field.GetNeighbors(bigAsteroid.PositionCell, 1);

            // Фильтруем занятые (где уже есть астероид/корабль)
            neighbors = neighbors.Where(cell => 
                !_asteroids.Any(a => a && a.PositionCell == cell) && 
                _ships.All(s => s.PositionCell != cell)).ToList();

            // Рандомим, сколько именно спавнить (от 1 до 6)
            int spawnCount = Random.Range(1, 7);

            // Если свободных клеток меньше, берём максимум, который влезет
            if (spawnCount > neighbors.Count)
                spawnCount = neighbors.Count;

            if (spawnCount <= 0) return;

            // Берём случайные свободные клетки из neighbors
            var randomNeighbors = neighbors.OrderBy(_ => Random.value).Take(spawnCount).ToList();

            // Спавним маленькие астероиды
            foreach (var cell in randomNeighbors)
            {
                // Направление = (cell - bigAsteroidCell)
                var direction = new CubeCellModel(
                    cell.Q - bigAsteroid.PositionCell.Q,
                    cell.R - bigAsteroid.PositionCell.R,
                    cell.S - bigAsteroid.PositionCell.S
                );

                var smallGo = Instantiate(smallAsteroidPrefab, cell.transform.position, Quaternion.identity);
                var smallCtrl = smallGo.GetComponent<AsteroidController>();

                // ХП и масса меньше в 10 раз
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
