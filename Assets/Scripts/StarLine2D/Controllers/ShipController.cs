using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarLine2D.Models;
using StarLine2D.Utils.Observables;
using StarLine2D.Utils.Disposable;
using StarLine2D.Managers;

namespace StarLine2D.Controllers
{
    public enum WeaponType
    {
        Point,
        Beam
    }

    public enum ShipShape
    {
        Single,
        HorizontalR,
        HorizontalL
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

    public class ShipController : MonoBehaviour, ICollisionParticipant
    {
        [SerializeField] private ParticleSystem shotAnimation;
        [SerializeField] private ParticleSystem explosionAnimation;
        [SerializeField] private float mass = 1f;
        [SerializeField] private IntObservableProperty health;
        [SerializeField] private IntObservableProperty score;
        [SerializeField] private MoveController moveController;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private List<Weapon> weapons = new();
        [SerializeField] private ShipShape shipShape = ShipShape.Single;
        [SerializeField] private CellController positionCell;

        private FieldController field;
        private readonly List<CubeCellModel> shipCellModels = new();
        private Action _onDestroy;
        private readonly CompositeDisposable _trash = new();

        public CellController MoveCell { get; set; }
        public float Mass => mass;
        public bool IsObstacle => false;

        public CellController PositionCell
        {
            get => positionCell;
            set
            {
                positionCell = value;
                UpdateShipCellModels();
            }
        }

        public IntObservableProperty Health => health;
        public IntObservableProperty Score => score;
        public int MaxHealth => maxHealth;
        public int MoveDistance => moveDistance;
        public MoveController MoveController => moveController;
        public List<Weapon> Weapons => weapons;
        public ShipShape ShipShape => shipShape;
        public List<CubeCellModel> ShipCellModels => shipCellModels;

        public IEnumerable<CellController> DesiredCells
        {
            get
            {
                var mainCell = MoveCell ?? positionCell;
                if (!mainCell) yield break;
                foreach (var c in GetShapeCells(shipShape, mainCell))
                {
                    if (c != null) yield return c;
                }
            }
        }

        private void Awake()
        {
            field = FindObjectOfType<FieldController>();
            if (!field) Debug.LogError($"[{name}] FieldController not found in scene.");
            OnValidate();
        }

        private void OnValidate()
        {
            health.Clamp(0, maxHealth);
            health.Validate();
            score.Validate();
            UpdateShipCellModels();
        }

        public int OnDamage(int dmg)
        {
            var oldHp = health.Value;
            health.Value -= dmg;
            if (health.Value > 0) return dmg;
            Debug.Log($"Ship {name} destroyed by damage {dmg}.");
            PlayExplosionAnimation(positionCell);
            Destroy(gameObject);
            return oldHp;
        }

        public void AddScore(int outputDamage)
        {
            score.Value += outputDamage * 10;
        }

        public void FlushShoots()
        {
            foreach (var w in weapons) w.ShootCell = null;
        }

        public void SetShipShape(ShipShape newShape)
        {
            shipShape = newShape;
            UpdateShipCellModels();
        }

        public ActionDisposable Subscribe(Action call)
        {
            _onDestroy += call;
            var disp = new ActionDisposable(() => _onDestroy -= call);
            _trash.Retain(disp);
            return disp;
        }

        private void OnDestroy()
        {
            _onDestroy?.Invoke();
            positionCell = null;
            _trash.Dispose();
        }

        private void UpdateShipCellModels()
        {
            shipCellModels.Clear();
            if (!positionCell) return;
            var q = positionCell.Q;
            var r = positionCell.R;
            var s = positionCell.S;
            shipCellModels.Add(new CubeCellModel(q, r, s));
            switch (shipShape)
            {
                case ShipShape.Single:
                    break;
                case ShipShape.HorizontalR:
                    shipCellModels.Add(new CubeCellModel(q - 1, r, s + 1));
                    break;
                case ShipShape.HorizontalL:
                    shipCellModels.Add(new CubeCellModel(q + 1, r, s - 1));
                    break;
            }
            UpdateVisualPosition();
        }

        public List<CubeCellModel> GetRelativeShapeOffsets()
        {
            var offsets = new List<CubeCellModel> { new CubeCellModel(0, 0, 0) };
            switch (shipShape)
            {
                case ShipShape.Single:
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

        private void UpdateVisualPosition()
        {
            if (!field) return;
            if (shipCellModels.Count == 0) return;
            var sum = Vector3.zero;
            var c = 0;
            foreach (var cell in shipCellModels
                         .Select(model => field.FindCellByModel(model))
                         .Where(cell => cell))
            {
                sum += cell.transform.position;
                c++;
            }
            if (c > 0) transform.position = sum / c;
            transform.localScale = Vector3.one;
        }

        public void PlayShotAnimation(CellController targetCell)
        {
            if (!shotAnimation)
            {
                Debug.LogWarning($"[{name}] No ShotAnimation assigned.");
                return;
            }
            if (!targetCell)
            {
                Debug.LogWarning($"[{name}] No targetCell for shot animation.");
                return;
            }
            var animGo = GameObject.Find("Animation");
            if (!animGo) Debug.LogError($"[{name}] GameObject 'Animation' not found in scene.");
            var animParent = animGo ? animGo.transform : null;
            var instance = Instantiate(shotAnimation, targetCell.transform.position, Quaternion.identity, animParent);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }

        public void PlayExplosionAnimation(CellController targetCell)
        {
            if (!explosionAnimation)
            {
                Debug.LogWarning($"[{name}] No ExplosionAnimation assigned.");
                return;
            }
            if (!targetCell)
            {
                Debug.LogWarning($"[{name}] No targetCell for explosion animation.");
                return;
            }
            var animGo = GameObject.Find("Animation");
            if (!animGo) Debug.LogError($"[{name}] GameObject 'Animation' not found in scene.");
            var animParent = animGo ? animGo.transform : null;
            var instance = Instantiate(explosionAnimation, targetCell.transform.position, Quaternion.identity, animParent);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }

        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            var result = new List<CellController>();
            if (!field || !headCell) return result;
            result.Add(headCell);
            switch (shape)
            {
                case ShipShape.Single:
                    break;
                case ShipShape.HorizontalR:
                    var left = field.FindCellByModel(new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1));
                    if (left) result.Add(left);
                    break;
                case ShipShape.HorizontalL:
                    var right = field.FindCellByModel(new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1));
                    if (right) result.Add(right);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
            return result;
        }
    }
}
