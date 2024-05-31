using StarLine2D.Utils.Observables;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class ShipController : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private FloatObservableProperty health;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int shootDistance = 5;
        [SerializeField] private int maxHealthPoints = 100;
        [SerializeField] private int damage = 100;

        private int healthPoints;

        public bool IsPlayer => isPlayer;
        public FloatObservableProperty Health => health;

        public int ShootDistance => shootDistance;
        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;

        public CellController PositionCell { get; set; }
        public CellController MoveCell { get; set; }
        public CellController ShotCell { get; set; }

        public int Damage => damage;

        private int HealthPoints
        {
            get => healthPoints;
            set => healthPoints = Mathf.Clamp(value, 0, maxHealthPoints);
        }

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            healthPoints = maxHealthPoints;
        }

        public void OnDamage(int inputDamage)
        {
            HealthPoints -= inputDamage;
            if (HealthPoints > 0) return;
            
            PositionCell?.ExplosionAnimation();
            Destroy(gameObject);
        }
    }
}