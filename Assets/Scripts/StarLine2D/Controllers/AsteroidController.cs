using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Factories;
using StarLine2D.Models;
using StarLine2D.Utils.Disposable;
using UnityEngine.Events;
using StarLine2D.Managers;

namespace StarLine2D.Controllers
{
    public enum AsteroidSize
    {
        Big,
        Small
    }

    public class AsteroidController : MonoBehaviour, ICollisionParticipant
    {
        private static readonly int Destroy1 = Animator.StringToHash("Destroy");
        [SerializeField] private AsteroidSize size = AsteroidSize.Big;
        [SerializeField] private int hp = 10;
        [SerializeField] private float asteroidMass = 1f;
        [SerializeField] private CellController positionCell;
        [SerializeField] private CubeCellModel direction;
        [SerializeField] private bool rotateClockwise = true;
        [SerializeField] private Transform asteroidSpriteTransform;
        [SerializeField] private Transform arrowRoot;
        [SerializeField] private ParticleSystem destroyParticles;
        [SerializeField] private Animator animator;
        [SerializeField] private UnityEvent onDestroy;

        private CellController oldCell;
        private Action _onDestroy;
        private readonly CompositeDisposable _trash = new CompositeDisposable();
        private bool isDestroying;

        public IEnumerable<CellController> DesiredCells
        {
            get
            {
                if (positionCell != null) yield return positionCell;
            }
        }

        public float Mass => asteroidMass;
        public bool IsObstacle => false;

        public CellController PositionCell
        {
            get => positionCell;
            set => positionCell = value;
        }

        public int OnDamage(int dmg)
        {
            if (isDestroying) return 0;
            var oldHp = hp;
            hp -= dmg;
            if (hp > 0) return dmg;
            StartCoroutine(DestroyByWeapon());
            return oldHp;
        }

        public AsteroidSize Size => size;
        public int Hp => hp;
        public CubeCellModel Direction => direction;
        public bool RotateClockwise => rotateClockwise;
        public CellController OldCell => oldCell;

        private void Awake()
        {
            if (!asteroidSpriteTransform)
            {
                var spriteChild = transform.Find("Sprite");
                if (spriteChild) asteroidSpriteTransform = spriteChild;
            }
            if (!arrowRoot)
            {
                var arrowChild = transform.Find("Arrow");
                if (arrowChild) arrowRoot = arrowChild;
            }
        }

        public void Initialize(
            AsteroidSize newSize,
            int newHp,
            float newMass,
            CellController newCell,
            CubeCellModel newDirection
        )
        {
            size = newSize;
            hp = newHp;
            asteroidMass = newMass;
            positionCell = newCell;
            direction = newDirection;
            if (positionCell) transform.position = positionCell.transform.position;
            var field = FindObjectOfType<FieldController>();
            if (!field)
            {
                Debug.LogError("[AsteroidController] FieldController not found in scene.");
                return;
            }
            if (!positionCell || direction == null) return;
            var nextQ = positionCell.Q + direction.Q;
            var nextR = positionCell.R + direction.R;
            var nextS = positionCell.S + direction.S;
            var nextCell = field.FindCellByModel(new CubeCellModel(nextQ, nextR, nextS));
            if (nextCell)
            {
                UpdateArrowDirection(positionCell.transform.position, nextCell.transform.position);
            }
            else
            {
                var fromPos = positionCell.transform.position;
                var fallbackDir = new Vector3(direction.Q, direction.R, 0f);
                if (fallbackDir.sqrMagnitude < 0.001f)
                {
                    if (arrowRoot) arrowRoot.gameObject.SetActive(false);
                }
                else
                {
                    var fallbackPos = fromPos + fallbackDir.normalized * 1f;
                    UpdateArrowDirection(fromPos, fallbackPos);
                }
            }
        }

        public void StorePreviousCell(CellController cell)
        {
            oldCell = cell;
        }

        public void RevertToOldCellAndReverseDirection()
        {
            if (!oldCell) return;
            positionCell = oldCell;
            direction = new CubeCellModel(-direction.Q, -direction.R, -direction.S);
            rotateClockwise = !rotateClockwise;
        }

        private IEnumerator DestroyByWeapon()
        {
            isDestroying = true;
            if (asteroidSpriteTransform) asteroidSpriteTransform.gameObject.SetActive(false);
            if (arrowRoot) arrowRoot.gameObject.SetActive(false);
            if (animator)
            {
                animator.SetTrigger(Destroy1);
                yield return new WaitForSeconds(1f);
            }
            if (destroyParticles)
            {
                var cellForExplosion = oldCell ?? positionCell;
                var animGo = GameObject.Find("Animation");
                if (!animGo) Debug.LogError("[AsteroidController] GameObject 'Animation' not found in scene.");
                var animParent = animGo ? animGo.transform : null;
                var particles = Instantiate(destroyParticles, cellForExplosion.transform.position, Quaternion.identity, animParent);
                particles.Play();
                var main = particles.main;
                yield return new WaitForSeconds(main.duration);
            }
            if (size == AsteroidSize.Big)
            {
                var factory = FindObjectOfType<AsteroidFactory>();
                if (!factory) Debug.LogError("[AsteroidController] AsteroidFactory not found in scene.");
                if (factory)
                {
                    var spawnCell = oldCell ?? positionCell;
                    var backup = positionCell;
                    positionCell = spawnCell;
                    factory.SpawnSmallAsteroids(this);
                    positionCell = backup;
                }
            }
            Destroy(gameObject);
        }

        public void UpdateTransformInstant(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            if (asteroidSpriteTransform) asteroidSpriteTransform.rotation = rot;
        }

        public IEnumerator UpdateTransformSmooth(
            Vector3 startPos,
            Vector3 endPos,
            Quaternion startRot,
            Quaternion endRot,
            float duration
        )
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                if (asteroidSpriteTransform)
                {
                    asteroidSpriteTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
                }
                yield return null;
            }
            transform.position = endPos;
            if (asteroidSpriteTransform) asteroidSpriteTransform.rotation = endRot;
        }

        public void SetPositionCell(CellController newCell)
        {
            positionCell = newCell;
        }

        public void UpdateArrowDirection(Vector3 fromPos, Vector3 toPos)
        {
            if (!arrowRoot) return;
            var dir = toPos - fromPos;
            if (dir.sqrMagnitude < 0.001f)
            {
                arrowRoot.gameObject.SetActive(false);
                return;
            }
            arrowRoot.gameObject.SetActive(true);
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float arrowAngle = baseAngle - 90f;
            arrowRoot.rotation = Quaternion.Euler(0, 0, arrowAngle);
        }

        public ActionDisposable Subscribe(Action call)
        {
            _onDestroy += call;
            var disposable = new ActionDisposable(() => _onDestroy -= call);
            _trash.Retain(disposable);
            return disposable;
        }

        private void OnDestroy()
        {
            _onDestroy?.Invoke();
            onDestroy?.Invoke();
            positionCell = null;
            _trash.Dispose();
        }
    }
}
