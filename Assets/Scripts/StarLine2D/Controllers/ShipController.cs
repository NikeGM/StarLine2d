using System;
using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Models;
using StarLine2D.Utils.Observables;
using StarLine2D.Utils.Disposable;

namespace StarLine2D.Controllers
{
    public enum WeaponType
    {
        Point, // Точечное оружие
        Beam   // Лучевое оружие
    }

    public enum ShipShape
    {
        Single,       // Одна клетка
        HorizontalR,  // Две клетки по горизонтали (доп. слева)
        HorizontalL   // Две клетки по горизонтали (доп. справа)
    }

    [Serializable]
    public class Weapon
    {
        [SerializeField] private int damage;
        [SerializeField] private int range;
        [SerializeField] private WeaponType type;
        [SerializeField] private int reload;
        [SerializeField] private CellController shootCell;

        public int Damage => damage;
        public int Range => range;
        public WeaponType Type => type;
        public int Reload => reload;
        public CellController ShootCell { get; set; }
    }

    public class ShipController : MonoBehaviour
    {
        // Убираем [SerializeField], чтобы поле не задавалось через инспектор
        private FieldController field;

        [Header("Настройки корабля")]
        [SerializeField] private IntObservableProperty health;
        [SerializeField] private IntObservableProperty score;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private List<Weapon> weapons = new();
        [SerializeField] private ShipShape shipShape = ShipShape.Single;

        [SerializeField] private CellController positionCell;

        // Список моделей клеток (CubeCellModel) для вычислений формы
        private readonly List<CubeCellModel> shipCellModels = new();

        private Action _onDestroy;
        private CompositeDisposable _trash = new();

        public CellController MoveCell { get; set; }

        public IntObservableProperty Health => health;
        public IntObservableProperty Score => score;
        public int MaxHealth => maxHealth;
        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;
        public List<Weapon> Weapons => weapons;
        public ShipShape ShipShape => shipShape;

        // При изменении "головной" клетки пересчитываем модели формы корабля
        public CellController PositionCell
        {
            get => positionCell;
            set
            {
                positionCell = value;
                UpdateShipCellModels();
            }
        }

        public List<CubeCellModel> ShipCellModels => shipCellModels;

        private void Awake()
        {
            // Пытаемся найти FieldController на сцене
            field = FindObjectOfType<FieldController>();
            if (!field)
            {
                Debug.LogWarning($"[{name}] ShipController: FieldController не найден на сцене!");
            }

            OnValidate();
        }

        private void OnValidate()
        {
            health.Clamp(0, maxHealth);
            health.Validate();
            score.Validate();
            UpdateShipCellModels();
        }

        /// <summary>
        /// Формируем список занимаемых клеток и обновляем визуальную позицию
        /// </summary>
        private void UpdateShipCellModels()
        {
            shipCellModels.Clear();
            if (!positionCell) return;

            var q = positionCell.Q;
            var r = positionCell.R;
            var s = positionCell.S;

            // Всегда добавляем "головную" клетку
            shipCellModels.Add(new CubeCellModel(q, r, s));

            // Добавляем вторую клетку (если корабль двухклеточный)
            switch (shipShape)
            {
                case ShipShape.Single:
                    // Нет второй клетки
                    break;

                case ShipShape.HorizontalR:
                    // Доп. клетка слева (q-1, s+1)
                    shipCellModels.Add(new CubeCellModel(q - 1, r, s + 1));
                    break;

                case ShipShape.HorizontalL:
                    // Доп. клетка справа (q+1, s-1)
                    shipCellModels.Add(new CubeCellModel(q + 1, r, s - 1));
                    break;
            }

            // Обновляем позицию по среднему между всеми клетками
            UpdateVisualPosition();
        }

        /// <summary>
        /// Ставит центр корабля в середину всех занимаемых им клеток.
        /// Если корабль одноклеточный – позиция совпадает с одной клеткой,
        /// если двухклеточный – будет по центру между ними.
        /// </summary>
        private void UpdateVisualPosition()
        {
            if (!field)
            {
                // Если не нашли FieldController — ничего не делаем
                return;
            }

            if (shipCellModels.Count == 0) return;

            Vector3 sumPositions = Vector3.zero;
            int count = 0;

            // Суммируем координаты всех занятых клеток
            foreach (var cubeCell in shipCellModels)
            {
                var cell = field.FindCellByModel(cubeCell);
                if (cell != null)
                {
                    sumPositions += cell.transform.position;
                    count++;
                }
            }

            if (count > 0)
            {
                // Среднее арифметическое
                transform.position = sumPositions / count;
            }

            // Масштаб оставляем (1,1,1), чтобы корабль НЕ растягивался
            transform.localScale = Vector3.one;
        }

        public int OnDamage(int inputDamage)
        {
            var currentHp = health.Value;
            health.Value -= inputDamage;
            if (health.Value > 0) return inputDamage;

            PositionCell?.ExplosionAnimation();
            Destroy(gameObject);

            return currentHp;
        }

        public void AddScore(int outputDamage)
        {
            score.Value += outputDamage * 10;
        }

        public void FlushShoots()
        {
            foreach (var weapon in weapons)
            {
                weapon.ShootCell = null;
            }
        }

        public void SetShipShape(ShipShape newShape)
        {
            shipShape = newShape;
            UpdateShipCellModels();
        }

        /// <summary>
        /// Аналогично AsteroidController. Подписка на событие уничтожения
        /// </summary>
        public ActionDisposable Subscribe(Action call)
        {
            _onDestroy += call;
            var disposable = new ActionDisposable(() => _onDestroy -= call);
            _trash.Retain(disposable);
            return disposable;
        }

        private void OnDestroy()
        {
            // Сначала вызываем все подписки
            _onDestroy?.Invoke();
            // Очищаем Disposable
            _trash?.Dispose();
        }

        /// <summary>
        /// Возвращает «относительные» координаты клеток в форме корабля,
        /// считая (0,0,0) за «головную» клетку.
        /// Может быть полезно при проверках в PositionManager.
        /// </summary>
        public List<CubeCellModel> GetRelativeShapeOffsets()
        {
            var offsets = new List<CubeCellModel>
            {
                new CubeCellModel(0, 0, 0) // "голова"
            };

            switch (shipShape)
            {
                case ShipShape.Single:
                    // Нет дополнительных клеток
                    break;

                case ShipShape.HorizontalR:
                    offsets.Add(new CubeCellModel(-1, 0, 1));
                    break;

                case ShipShape.HorizontalL:
                    offsets.Add(new CubeCellModel(1, 0, -1));
                    break;
            }

            return offsets;
        }
    }
}
