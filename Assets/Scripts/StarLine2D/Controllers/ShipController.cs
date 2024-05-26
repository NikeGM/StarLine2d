using StarLine2D.Utils.Observables;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class ShipController : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private FloatObservableProperty health;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 2;
        [SerializeField] private int shootDistance = 5;
        
        public bool IsPlayer => isPlayer;
        public FloatObservableProperty Health => health;
        
        public int ShootDistance => shootDistance;
        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;
        
        private CellController _positionCell;
        public CellController PositionCell { get; set; }
        
        private CellController _moveCell;
        public CellController MoveCell { get; set; }
        
        private CellController _shotCell;
        public CellController ShotCell { get; set; }

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            health.Validate();
            var val = health.Value;
            if (val is >= 0 and <= 1) return;

            val = Mathf.Min(val, 1);
            val = Mathf.Max(val, 0);
            health.Value = val;
        }
    }
}
