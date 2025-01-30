using System.Collections.Generic;
using StarLine2D.Utils.Observables;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public enum WeaponType
    {
        Point, // Точечное оружие
        Beam   // Лучевое оружие
    }

    [System.Serializable]
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
        public CellController  ShootCell  { get; set; }
    }

    public class ShipController : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private IntObservableProperty health;
        [SerializeField] private IntObservableProperty score;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private List<Weapon> weapons = new List<Weapon>();

        public IntObservableProperty Score => score;
        public bool IsPlayer => isPlayer;
        public IntObservableProperty Health => health;
        public int MaxHealth => maxHealth;

        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;
        public List<Weapon> Weapons => weapons;

        public CellController PositionCell { get; set; }
        public CellController MoveCell { get; set; }
        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            health.Clamp(0, maxHealth);
            health.Validate();
            score.Validate();
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
    }
}
