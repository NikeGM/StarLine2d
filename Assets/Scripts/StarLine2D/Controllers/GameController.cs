using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Components;
using StarLine2D.Utils.Disposable;
using StarLine2D.Models;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Главный «координатор» игры (пример).
    /// Берёт менеджеры на том же GameObject через GetComponent<>.
    /// Спауним препятствия первыми, чтобы занять клетки.
    /// Потом корабли, потом астероиды.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Ссылки на поле и префабы (корабли)")]
        [SerializeField] private FieldController field;
        [SerializeField] private List<ShipPrefabData> allShipPrefabs;
        [SerializeField] private int selectedPlayerIndex = 0;
        [SerializeField] private int numberOfEnemies = 3;
        [SerializeField] private int numberOfAllies = 2;

        [Header("Настройка астероидов")]
        [SerializeField] private GameObject bigAsteroidPrefab;
        [SerializeField] private GameObject smallAsteroidPrefab;
        [SerializeField] private int numberOfAsteroids = 5;
        [SerializeField] private int minAsteroidHp = 5;
        [SerializeField] private int maxAsteroidHp = 20;
        [SerializeField] private float minAsteroidMass = 0.5f;
        [SerializeField] private float maxAsteroidMass = 3.0f;

        [Header("Настройка препятствий")]
        [SerializeField] private List<ObstaclePrefabData> obstaclePrefabs;
        [SerializeField] private int numberOfObstacles = 3;

        // Фабрики
        private ShipFactory _shipFactory;
        private AsteroidFactory _asteroidFactory;
        private ObstacleFactory _obstacleFactory;

        // Менеджеры (не через инспектор, а берем на том же GameObject)
        private MovementManager movementManager;
        private AttackManager attackManager;
        private CollisionManager collisionManager;
        private TurnManager turnManager;

        // Списки созданных объектов
        private List<ShipController> _ships = new();
        private List<AsteroidController> _asteroids = new();
        private List<ObstacleController> _obstacles = new();

        private readonly CompositeDisposable _trash = new();
        private CellsStateController _cellsStateController;

        // Свойства для доступа из менеджеров:
        public FieldController Field => field;
        public List<ShipController> Ships => _ships;
        public List<AsteroidController> Asteroids => _asteroids;
        public ShipFactory ShipFactory => _shipFactory;
        public AsteroidFactory AsteroidFactory => _asteroidFactory;

        private void Awake()
        {
            // Загрузка сцены HUD, если нужно
            Utils.Utils.AddScene("Hud");

            // Создаём фабрики
            _shipFactory = new ShipFactory(
                allShipPrefabs,
                selectedPlayerIndex,
                numberOfAllies,
                numberOfEnemies
            );
            _asteroidFactory = new AsteroidFactory(
                bigAsteroidPrefab,
                smallAsteroidPrefab,
                numberOfAsteroids,
                minAsteroidHp,
                maxAsteroidHp,
                minAsteroidMass,
                maxAsteroidMass
            );
            _obstacleFactory = new ObstacleFactory(
                obstaclePrefabs,
                numberOfObstacles
            );

            // Ищем менеджеры на том же GameObject
            movementManager  = GetComponent<MovementManager>();
            attackManager    = GetComponent<AttackManager>();
            collisionManager = GetComponent<CollisionManager>();
            turnManager      = GetComponent<TurnManager>();

            // Инициализируем их
            if (movementManager != null)  movementManager.Init(this);
            if (attackManager != null)    attackManager.Init(this);
            if (collisionManager != null) collisionManager.Init(this);
            if (turnManager != null)      turnManager.Init(this);
        }

        private void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
            _cellsStateController = field.GetComponent<CellsStateController>();

            // 1) Спауним препятствия (занимают клетки в первую очередь)
            _obstacles.AddRange(
                _obstacleFactory.SpawnObstacles(field)
            );

            // 2) Спауним корабли
            _ships = _shipFactory.SpawnAllShips(field);

            // 3) Спауним астероиды
            _asteroids.AddRange(_asteroidFactory.SpawnAsteroids(field, _ships));
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }

        // --------------------------------------------------------------------
        // Методы, которые дергают логику менеджеров
        // --------------------------------------------------------------------
        public void OnPositionClicked()
        {
            if (attackManager)
                attackManager.OnPositionClicked();
        }

        public void OnAttackClicked(int weaponIndex)
        {
            if (attackManager)
                attackManager.OnAttackClicked(weaponIndex);
        }

        // Запуск корутины «конца хода»
        public void StartCoroutineTurn()
        {
            if (turnManager)
            {
                StartCoroutine(turnManager.TurnFinished());
            }
        }

        private void OnCellClicked(GameObject go)
        {
            if (attackManager)
                attackManager.OnCellClicked(go);
        }

        // --------------------------------------------------------------------
        // Вспомогательные методы (нужны менеджерам).
        // --------------------------------------------------------------------
        public ShipController GetPlayerShip()
        {
            return _ships.Find(s => s != null && s.GetComponent<PlayerController>() != null);
        }

        public void SpawnSmallAsteroids(AsteroidController bigAsteroid)
        {
            if (_asteroidFactory == null) return;
            _asteroidFactory.SpawnSmallAsteroids(bigAsteroid, field, _asteroids, _ships);
        }

        // --------------------------------------------------------------------
        // "Обёрточные" методы, чтобы старый код (например, CellsStateController) не ломался
        // при вызовах gameController.GetShapeCells(...), gameController.TurnFinished(), и т.д.
        // --------------------------------------------------------------------
        public System.Collections.IEnumerator TurnFinished()
        {
            if (turnManager != null)
            {
                yield return StartCoroutine(turnManager.TurnFinished());
            }
        }

        public List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (attackManager == null)
                return new List<CellController>();
            return attackManager.GetShapeCells_Public(shape, headCell);
        }

        public List<CellController> GetShipCells(ShipController ship)
        {
            if (attackManager == null)
                return new List<CellController>();
            return attackManager.GetShipCells_Public(ship);
        }
    }
}
