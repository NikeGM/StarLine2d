using StarLine2D.Utils.Observables;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class ShipController : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private IntObservableProperty health;
        [SerializeField] private IntObservableProperty score;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int shootDistance = 5;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int damage = 100;

        public IntObservableProperty Score => score;
        public bool IsPlayer => isPlayer;
        public IntObservableProperty Health => health;
        public int MaxHealth => maxHealth;

        public int ShootDistance => shootDistance;
        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;

        public CellController PositionCell { get; set; }
        public CellController MoveCell { get; set; }
        public CellController ShotCell { get; set; }

        public int Damage => damage;

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
    }
}