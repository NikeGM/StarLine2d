using System;
using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Models;
using StarLine2D.Utils.Observables;

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
        [SerializeField] private IntObservableProperty health;
        [SerializeField] private IntObservableProperty score;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private List<Weapon> weapons = new();
        [SerializeField] private ShipShape shipShape = ShipShape.Single;

        // "Головная" клетка корабля
        [SerializeField] private CellController positionCell;

        // Список моделей клеток (CubeCellModel) для вычислений формы
        private readonly List<CubeCellModel> shipCellModels = new();

        public CellController MoveCell { get; set; }

        // Убрали isPlayer — теперь тип корабля определяем по контроллерам (Player/Ally/Enemy)
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

        // Список CubeCellModel для внешнего доступа (GameController)
        public List<CubeCellModel> ShipCellModels => shipCellModels;

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            health.Clamp(0, maxHealth);
            health.Validate();
            score.Validate();
            UpdateShipCellModels();
        }

        private void UpdateShipCellModels()
        {
            shipCellModels.Clear();
            if (!positionCell) return;

            var q = positionCell.Q;
            var r = positionCell.R;
            var s = positionCell.S;

            // Головная клетка
            shipCellModels.Add(new CubeCellModel(q, r, s));

            // Вторая клетка (если двухклеточный)
            switch (shipShape)
            {
                case ShipShape.Single:
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
    }
}
