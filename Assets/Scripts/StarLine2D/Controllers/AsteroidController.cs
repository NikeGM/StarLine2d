using UnityEngine;
using StarLine2D.Models;
using System.Collections;

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

        [Header("Текущая позиция и направление (куб. координаты)")]
        [SerializeField] private CellController positionCell;
        [SerializeField] private CubeCellModel direction;

        [Header("Параметры вращения")]
        [SerializeField] private bool rotateClockwise = true;

        [Header("Ссылки на объекты внутри префаба")]
        [SerializeField] private Transform _asteroidSpriteTransform; 
        [SerializeField] private Transform _arrowRoot;

        [Header("Эффекты уничтожения (Particles)")]
        [SerializeField] private ParticleSystem destroyParticles;

        [Header("Анимация уничтожения (опционально)")]
        [SerializeField] private Animator animator;

        private bool _isDestroying;
        private FieldController _field;
        private GameController _gameController;

        public AsteroidSize Size => size;
        public int HP => hp;
        public float Mass => mass;
        public CellController PositionCell => positionCell;
        public CubeCellModel Direction => direction;
        public bool RotateClockwise => rotateClockwise;

        private void Awake()
        {
            _field = FindObjectOfType<FieldController>();
            _gameController = FindObjectOfType<GameController>();

            // Подстраховка на случай, если ссылки не выставлены в инспекторе
            if (!_asteroidSpriteTransform)
            {
                var spriteChild = transform.Find("Sprite");
                if (spriteChild) _asteroidSpriteTransform = spriteChild;
            }
            if (!_arrowRoot)
            {
                var arrowChild = transform.Find("Arrow");
                if (arrowChild) _arrowRoot = arrowChild;
            }
        }

        /// <summary>
        /// Инициализация астероида на стартовой клетке, с задаными параметрами.
        /// </summary>
        public void Initialize(AsteroidSize size, int hp, float mass, CellController startCell, CubeCellModel direction)
        {
            this.size = size;
            this.hp = hp;
            this.mass = mass;
            this.positionCell = startCell;
            this.direction = direction;

            if (positionCell != null)
            {
                transform.position = positionCell.transform.position;
            }

            SetupArrowOnSpawn();
        }

        /// <summary>
        /// Настроить стрелку при появлении (пример).
        /// </summary>
        private void SetupArrowOnSpawn()
        {
            if (!_arrowRoot || positionCell == null || _field == null) return;

            int nextQ = positionCell.Q + direction.Q;
            int nextR = positionCell.R + direction.R;
            int nextS = positionCell.S + direction.S;
            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = _field.FindCellByModel(nextCellModel);

            if (nextCell == null || nextCell == positionCell)
            {
                // Нет движения - прячем стрелку
                _arrowRoot.gameObject.SetActive(false);
                return;
            }

            // Иначе показываем стрелку
            _arrowRoot.gameObject.SetActive(true);

            // Рассчитываем угол
            var oldPos = transform.position;
            var newPos = nextCell.transform.position;
            var dir = (newPos - oldPos).normalized;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float arrowAngle = baseAngle - 90f;
            _arrowRoot.rotation = Quaternion.Euler(0, 0, arrowAngle);
        }

        /// <summary>
        /// Плавное перемещение без пол-оборота.
        /// </summary>
        public IEnumerator MoveSmoothly(FieldController field, float moveDuration)
        {
            if (_isDestroying || positionCell == null || field == null)
                yield break;

            int nextQ = positionCell.Q + direction.Q;
            int nextR = positionCell.R + direction.R;
            int nextS = positionCell.S + direction.S;

            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = field.FindCellByModel(nextCellModel);

            if (nextCell == null)
            {
                Destroy(gameObject);
                yield break;
            }

            var oldPos = transform.position;
            var newPos = nextCell.transform.position;

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                transform.position = Vector3.Lerp(oldPos, newPos, t);
                yield return null;
            }

            transform.position = newPos;
            positionCell = nextCell;
        }

        /// <summary>
        /// Плавное перемещение с пол-оборотом + поворот стрелки.
        /// </summary>
        public IEnumerator MoveSmoothlyWithHalfTurn(FieldController field, float moveDuration)
        {
            if (_isDestroying || positionCell == null || field == null)
                yield break;

            int nextQ = positionCell.Q + direction.Q;
            int nextR = positionCell.R + direction.R;
            int nextS = positionCell.S + direction.S;

            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = field.FindCellByModel(nextCellModel);

            if (nextCell == null)
            {
                // Вышли за границы, уничтожаем
                Destroy(gameObject);
                yield break;
            }

            // Если не движемся
            if (nextCell == positionCell)
            {
                if (_arrowRoot)
                    _arrowRoot.gameObject.SetActive(false);
                yield break;
            }
            else
            {
                // Включаем стрелку
                if (_arrowRoot)
                    _arrowRoot.gameObject.SetActive(true);
            }

            // Пол-оборот
            float halfTurnAngle = rotateClockwise ? -180f : 180f;
            var oldRot = _asteroidSpriteTransform ? _asteroidSpriteTransform.rotation : Quaternion.identity;
            var newRot = oldRot * Quaternion.Euler(0, 0, halfTurnAngle);

            // Поворот стрелки
            var oldPos = transform.position;
            var newPos = nextCell.transform.position;
            var directionVector = (newPos - oldPos).normalized;
            float baseAngle = Mathf.Atan2(directionVector.y, directionVector.x) * Mathf.Rad2Deg;
            float arrowAngle = baseAngle - 90f;

            var arrowOldRot = _arrowRoot ? _arrowRoot.rotation : Quaternion.identity;
            var arrowNewRot = Quaternion.Euler(0, 0, arrowAngle);

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                transform.position = Vector3.Lerp(oldPos, newPos, t);

                // Вращаем спрайт астероида
                if (_asteroidSpriteTransform)
                {
                    _asteroidSpriteTransform.rotation = Quaternion.Slerp(oldRot, newRot, t);
                }

                // Поворот стрелки
                if (_arrowRoot)
                {
                    _arrowRoot.rotation = Quaternion.Slerp(arrowOldRot, arrowNewRot, t);
                }

                yield return null;
            }

            transform.position = newPos;
            positionCell = nextCell;

            if (_asteroidSpriteTransform)
                _asteroidSpriteTransform.rotation = newRot;

            if (_arrowRoot)
                _arrowRoot.rotation = arrowNewRot;
        }

        /// <summary>
        /// Вызывается при попадании оружия. Если HP <= 0 — уничтожается.
        /// </summary>
        public int OnDamage(int damage)
        {
            if (_isDestroying) return 0;

            int currentHp = hp;
            hp -= damage;
            if (hp <= 0)
            {
                StartCoroutine(DestroyByWeapon());
                return currentHp;
            }
            return damage;
        }

        /// <summary>
        /// Корутина уничтожения. Сначала скрываем спрайт,
        /// потом играем анимацию / партиклы, затем уничтожаем объект.
        /// </summary>
        private IEnumerator DestroyByWeapon()
        {
            _isDestroying = true;

            // 1) Спрячем «камень» и стрелку
            if (_asteroidSpriteTransform)
                _asteroidSpriteTransform.gameObject.SetActive(false);

            if (_arrowRoot)
                _arrowRoot.gameObject.SetActive(false);

            // 2) Запускаем (опционально) анимацию
            if (animator)
            {
                animator.SetTrigger("Destroy");
                yield return new WaitForSeconds(1f); 
            }

            // 3) Запускаем партиклы (если есть)
            if (destroyParticles != null)
            {
                var particles = Instantiate(destroyParticles, transform.position, Quaternion.identity);
                particles.Play();
                var main = particles.main;
                // Ждём, пока они отыграют
                yield return new WaitForSeconds(main.duration);
            }

            // 4) Если большой астероид, вызываем спавн мелких
            if (size == AsteroidSize.Big && _gameController != null)
            {
                _gameController.SpawnSmallAsteroids(this);
            }

            // 5) Уничтожаем
            Destroy(gameObject);
        }
    }
}
