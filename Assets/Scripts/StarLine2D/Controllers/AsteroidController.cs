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
        // Если rotateClockwise = true, делаем пол-оборота по часовой стрелке при движении
        // Если false — против часовой стрелки
        [SerializeField] private bool rotateClockwise = true;

        // Можно (опционально) добавить аниматор для анимации уничтожения
        [Header("Анимация уничтожения (опционально)")]
        [SerializeField] private Animator animator;
        private bool _isDestroying;

        // --- Изменено: Сохраним ссылки на FieldController и GameController, чтобы при уничтожении большого астероида
        // можно было вызвать логику спавна маленьких в GameController.
        private FieldController _field;
        private GameController _gameController;

        public AsteroidSize Size => size;
        public int HP => hp;
        public float Mass => mass;
        public CellController PositionCell => positionCell;
        public CubeCellModel Direction => direction;

        /// <summary>
        /// Свойство, чтобы узнать, вращаем ли мы по часовой стрелке или нет.
        /// </summary>
        public bool RotateClockwise => rotateClockwise;

        private void Awake()
        {
            // Находим контроллеры на сцене (или можете настроить передачу ссылок иначе).
            _field = FindObjectOfType<FieldController>();
            _gameController = FindObjectOfType<GameController>();
        }

        /// <summary>
        /// Инициализация астероида стартовыми параметрами.
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
        }

        /// <summary>
        /// Плавное перемещение БЕЗ дополнительного поворота (как раньше).
        /// </summary>
        public IEnumerator MoveSmoothly(FieldController field, float moveDuration)
        {
            // Если уничтожается или нет поля/позиции
            if (_isDestroying || positionCell == null || field == null)
                yield break;

            // Проверяем следующую клетку
            int nextQ = positionCell.Q + direction.Q;
            int nextR = positionCell.R + direction.R;
            int nextS = positionCell.S + direction.S;

            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = field.FindCellByModel(nextCellModel);

            if (nextCell == null)
            {
                // Вылетел за границы — уничтожаем
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
        /// НОВЫЙ метод: плавное перемещение с «пол-оборотом» (180°).
        /// Астероид поворачивается на 180° в одну сторону (rotateClockwise).
        /// </summary>
        public IEnumerator MoveSmoothlyWithHalfTurn(FieldController field, float moveDuration)
        {
            if (_isDestroying || positionCell == null || field == null)
                yield break;

            // Считаем следующую клетку
            int nextQ = positionCell.Q + direction.Q;
            int nextR = positionCell.R + direction.R;
            int nextS = positionCell.S + direction.S;

            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = field.FindCellByModel(nextCellModel);

            if (nextCell == null)
            {
                // Вылетает за границы — уничтожаем
                Destroy(gameObject);
                yield break;
            }

            // Сохраняем старые координаты
            var oldPos = transform.position;
            var newPos = nextCell.transform.position;

            // Определяем угол пол-оборота
            // Положительный угол — вращение против часовой (Unity считает z-axis вверх),
            // Отрицательный — по часовой. 
            float halfTurnAngle = rotateClockwise ? -180f : 180f;

            // Считаем начальный и конечный кватернион
            var oldRot = transform.rotation;
            var newRot = oldRot * Quaternion.Euler(0, 0, halfTurnAngle);

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                // Линейно перемещаемся
                transform.position = Vector3.Lerp(oldPos, newPos, t);

                // Плавно поворачиваемся на 180° (Slerp)
                transform.rotation = Quaternion.Slerp(oldRot, newRot, t);

                yield return null;
            }

            // Фиксируем конечное положение и ориентацию
            transform.position = newPos;
            transform.rotation = newRot;
            positionCell = nextCell;
        }

        /// <summary>
        /// Получаем урон от оружия. Если HP <= 0, запускаем анимацию уничтожения.
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
        /// Короутина анимации уничтожения от оружия (пример, можно менять под свои нужды).
        /// При уничтожении большого астероида (AsteroidSize.Big) — спавн маленьких.
        /// </summary>
        private IEnumerator DestroyByWeapon()
        {
            _isDestroying = true;

            if (animator)
            {
                animator.SetTrigger("Destroy");
                yield return new WaitForSeconds(1f); 
            }
            else
            {
                // Без аниматора — простая задержка
                Debug.Log("Астероид уничтожен оружием, проигрываем визуальные эффекты...");
                yield return new WaitForSeconds(1f);
            }

            // --- Изменено: вызываем логику спавна маленьких астероидов в GameController,
            //               если этот астероид был большим.
            if (size == AsteroidSize.Big && _gameController != null)
            {
                _gameController.SpawnSmallAsteroids(this);
            }

            Destroy(gameObject);
        }
    }
}
