using System.Collections;
using UnityEngine;
using StarLine2D.Factories;
using StarLine2D.Models;

namespace StarLine2D.Controllers
{
    public enum AsteroidSize
    {
        Big,
        Small
    }

    public class AsteroidController : MonoBehaviour
    {
        [SerializeField] private AsteroidSize size = AsteroidSize.Big;
        [SerializeField] private int hp = 10;
        [SerializeField] private float mass = 1f;
        [SerializeField] private CellController positionCell;
        [SerializeField] private CubeCellModel direction;
        [SerializeField] private bool rotateClockwise = true;
        [SerializeField] private Transform asteroidSpriteTransform;
        [SerializeField] private Transform arrowRoot;
        [SerializeField] private ParticleSystem destroyParticles;
        [SerializeField] private Animator animator;
        [SerializeField] private UnityEvent onDestroy;
        
        private bool isDestroying;

        public AsteroidSize Size => size;
        public int Hp => hp;
        public float Mass => mass;
        public CellController PositionCell => positionCell;
        public CubeCellModel Direction => direction;
        public bool RotateClockwise => rotateClockwise;

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

        public void Initialize(AsteroidSize newSize, int newHp, float newMass, CellController newCell, CubeCellModel newDirection)
        {
            size = newSize;
            hp = newHp;
            mass = newMass;
            positionCell = newCell;
            direction = newDirection;
            if (positionCell != null)
            {
                transform.position = positionCell.transform.position;
            }
        }

        public int OnDamage(int damage)
        {
            if (isDestroying) return 0;
            int currentHp = hp;
            hp -= damage;
            if (hp <= 0)
            {
                StartCoroutine(DestroyByWeapon());
                return currentHp;
            }
            return damage;
        }

        private IEnumerator DestroyByWeapon()
        {
            isDestroying = true;
            if (asteroidSpriteTransform) asteroidSpriteTransform.gameObject.SetActive(false);
            if (arrowRoot) arrowRoot.gameObject.SetActive(false);
            if (animator)
            {
                animator.SetTrigger("Destroy");
                yield return new WaitForSeconds(1f);
            }
            
            // Ищем в сцене объект "Animation" для родителя частиц
            Transform animParent = null;
            var animGo = GameObject.Find("Animation");
            if (animGo != null)
            {
                animParent = animGo.transform;
            }

            if (destroyParticles != null)
            {
                var particles = Instantiate(destroyParticles, transform.position, Quaternion.identity, animParent);
                particles.Play();
                var main = particles.main;
                yield return new WaitForSeconds(main.duration);
            }
            if (size == AsteroidSize.Big)
            {
                var factory = FindObjectOfType<AsteroidFactory>();
                if (factory) factory.SpawnSmallAsteroids(this);
            }
            Destroy(gameObject);
        }

        public void UpdateTransformInstant(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            if (asteroidSpriteTransform) asteroidSpriteTransform.rotation = rot;
        }

        public IEnumerator UpdateTransformSmooth(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot, float duration)
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
            var dir = (toPos - fromPos);
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
        
        
    }
}
