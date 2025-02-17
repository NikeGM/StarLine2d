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
        [SerializeField] private int selectedPlayerIndex = 0;
        [SerializeField] private int selectedEnemyIndex = 0;

        [SerializeField] private bool debugAlwaysHit;
        
        // Фиксированная длительность движения (в секундах)
        [SerializeField] private float moveDuration = 2.0f;

        private List<ShipController> _ships = new();
        private readonly CompositeDisposable _trash = new();
        private CellsStateController _cellsStateController;

        private int _currentWeapon = -1;

        private void Awake()
        {
            // Загрузка сцены "Hud"
            Utils.Utils.AddScene("Hud");
        }

        public void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
            _ships = SetPlayersShips();
            _cellsStateController = field.GetComponent<CellsStateController>();
        }

        /// <summary>
        /// Преобразует список моделей клеток (CubeCellModel), полученных из ShipController,
        /// в список реальных CellController для работы с анимациями, проверками коллизий и т.п.
        /// </summary>
        public List<CellController> GetShipCells(ShipController ship)
        {
            var result = new List<CellController>();
            var modelCells = ship.ShipCellModels; // Список CubeCellModel из ShipController

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

        /// <summary>
        /// Клик "Position" - выбираем перемещение
        /// </summary>
        public void OnPositionClicked()
        {
            var player = GetPlayerShip();
            player.FlushShoots();
            player.MoveCell = null;
            _cellsStateController.ClearStaticCells();
            // Ставим зону перемещения (обычный способ)
            _cellsStateController.SetZone(player.PositionCell, player.MoveDistance, CellsStateController.MoveZone, "");
        }

        /// <summary>
        /// Клик "Attack" (Weapon 0 / 1 / 2...)
        /// Теперь учитываем будущую позицию корабля (MoveCell), 
        /// чтобы строить зону выстрела "из будущего положения".
        /// </summary>
        public void OnAttackClicked(int index)
        {
            var player = GetPlayerShip();
            // ВАЖНО: проверяем, выбрана ли уже MoveCell (куда корабль переместится)
            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }

            // Берем оружие
            var weapon = player.Weapons[index];

            // Узнаем форму корабля на будущей клетке
            var shapeCells = GetShapeCells(player.ShipShape, player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке (выход за границы?)");
                return;
            }

            // Новым методом в CellsStateController ставим зону для атаки "из будущего"
            _cellsStateController.SetWeaponZoneForFuturePosition(shapeCells, weapon.Range, weapon.Type.ToString());

            _currentWeapon = index;
        }

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

            var playerMoveCoroutine = StartCoroutine(MoveShip(playerShip));
            var enemyMoveCoroutine = StartCoroutine(MoveShip(enemyShip));

            yield return playerMoveCoroutine;
            yield return enemyMoveCoroutine;

            Shot(playerShip);
            Shot(enemyShip);

            if (isCollision)
            {
                OnShipCollision(playerShip, enemyShip);
            }
        }

        private void OnCellClicked(GameObject go)
        {
            Debug.Log(go);
            if (go == field.gameObject) return;

            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;

            // Проверяем, в какой зоне находимся
            if (_cellsStateController.Zone.Type == CellsStateController.MoveZone)
            {
                // Логика выбора перемещения
                var player = GetPlayerShip();
                if (player == null) return;

                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: часть корабля окажется за границей поля!");
                    return;
                }

                // Помечаем все эти клетки как MovePoint
                for (int i = 0; i < shapeCells.Count; i++)
                {
                    _cellsStateController.AddStaticCell(
                        $"Move_{i}",
                        shapeCells[i],
                        CellsStateController.MovePoint
                    );
                }

                player.MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            // Если это зона атаки
            if (_currentWeapon != -1 && _cellsStateController.Zone.Type == CellsStateController.WeaponZone)
            {
                // Ставим ShootPoint
                _cellsStateController.AddStaticCell(_currentWeapon.ToString(), cell, CellsStateController.ShootPoint);
                GetPlayerShip().Weapons[_currentWeapon].ShootCell = cell;
                _cellsStateController.ClearZone();
            }
        }

        /// <summary>
        /// Создаёт корабль игрока и корабль врага в случайных клетках.
        /// </summary>
        private List<ShipController> SetPlayersShips()
        {
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

            // Ставим корабль в центр занимаемых клеток (если двухклеточный)
            player.transform.position = GetShapeCenter(playerShipController);
            enemy.transform.position  = GetShapeCenter(enemyShipController);

            // Начальные углы
            player.transform.rotation = Quaternion.Euler(0, 0, 0);
            enemy.transform.rotation = Quaternion.Euler(0, 0, 180);

            // Инициализация врага
            enemyController.Initialize(enemyCell, field);

            return shipsList;
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

        public ShipController GetEnemyShip()
        {
            return _ships.Find(item => !item.IsPlayer);
        }

        /// <summary>
        /// Корутин перемещения от старой середины к новой.
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

            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return SmoothMove(ship, oldCenter, newCenter, moveDuration);

            if (ship.IsPlayer)
                ship.transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                ship.transform.rotation = Quaternion.Euler(0, 0, 180);
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
        /// Возвращает центр (среднюю точку) всех клеток, которые занимает корабль.
        /// </summary>
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

        private void OnDestroy()
        {
            _trash.Dispose();
        }

        /// <summary>
        /// Логика стрельбы (Point/Beam).
        /// Beam идёт от ближайшей клетки к цели.
        /// </summary>
        private void Shot(ShipController ship)
        {
            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell)
                    continue;

                // Ищем ближайшую клетку корабля
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
                    foreach (var c in cellsOnLine)
                        shootCells.Add(c);
                }

                // Анимация
                shootCells.ToList().ForEach(c => c.ShotAnimation());

                // Урон
                var damagedShips = new HashSet<ShipController>();

                foreach (var shootCell in shootCells)
                {
                    if (HasShip(shootCell))
                    {
                        var damagedShip = GetShip(shootCell);
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var resultedDamage = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
                                // Самострел
                                ship.AddScore(-resultedDamage);
                            }
                            else
                            {
                                ship.AddScore(resultedDamage);
                                damagedShips.Add(damagedShip);
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

        // --------------------------------------------------------------------
        // Метод, возвращающий форму корабля (1 или 2 клетки) или пустой список,
        // если часть формы выходит за границы поля.
        // --------------------------------------------------------------------
        public List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (headCell == null) 
                return new List<CellController>(); 

            var result = new List<CellController>();
            result.Add(headCell);

            switch (shape)
            {
                case ShipShape.Single:
                    // Одноклеточный
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
    }
}
