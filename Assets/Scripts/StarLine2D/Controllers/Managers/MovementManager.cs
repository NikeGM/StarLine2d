using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Controllers; // чтобы знать, что такое GameController, ShipController

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Менеджер, отвечающий за логику движения.
    /// Можно всё хранить в private-полях (например moveDuration),
    /// или же брать их из GameController, если там задано.
    /// </summary>
    public class MovementManager : MonoBehaviour
    {
        private GameController _gameController;

        [SerializeField] private float defaultMoveDuration = 2.0f;

        /// <summary>
        /// Инициализируем менеджер, передавая ему главный GameController
        /// (чтобы был доступ к Ship'ам, Field и прочему).
        /// </summary>
        public void Init(GameController gameController)
        {
            _gameController = gameController;
        }

        /// <summary>
        /// Пример корутины движения всех кораблей и параллельно - астероидов.
        /// </summary>
        public IEnumerator MoveAllShipsAndAsteroids()
        {
            // Для примера берём список кораблей и астероидов из _gameController
            List<ShipController> ships = _gameController.Ships;
            List<AsteroidController> asteroids = _gameController.Asteroids;

            var movementCoroutines = new List<Coroutine>();
            foreach (var ship in ships)
            {
                movementCoroutines.Add(
                    StartCoroutine(MoveShip(ship))
                );
            }

            // Пример: если астероид должен крутиться/лететь
            foreach (var asteroid in asteroids)
            {
                if (asteroid != null)
                {
                    movementCoroutines.Add(
                        StartCoroutine(
                            asteroid.MoveSmoothlyWithHalfTurn(
                                _gameController.Field,
                                defaultMoveDuration
                            )
                        )
                    );
                }
            }

            // Дожидаемся, пока все coroutines завершатся
            foreach (var cor in movementCoroutines)
            {
                yield return cor;
            }
        }

        /// <summary>
        /// Корутинка движения одного корабля.
        /// </summary>
        private IEnumerator MoveShip(ShipController ship)
        {
            if (ship == null || ship.MoveCell == null)
                yield break;

            // Допустим, здесь возьмём метод GetShapeCenter из GameController
            Vector3 oldCenter = ship.transform.position;
            if (_gameController)
            {
                oldCenter = GetShapeCenter(ship);
            }

            var newCell = ship.MoveCell;
            ship.MoveCell = null;
            ship.PositionCell = newCell;

            Vector3 newCenter = newCell.transform.position;
            if (_gameController)
            {
                newCenter = GetShapeCenter(ship);
            }

            // Поворот к цели
            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return SmoothMove(ship, oldCenter, newCenter, defaultMoveDuration);

            // Поворот обратно «лицом»
            var playerCtrl = ship.GetComponent<PlayerController>();
            if (playerCtrl != null)
                ship.transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                ship.transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        /// <summary>
        /// Плавное движение корабля.
        /// </summary>
        private IEnumerator SmoothMove(ShipController ship, Vector3 startPos, Vector3 endPos, float duration)
        {
            if (duration <= 0.0001f)
            {
                ship.transform.position = endPos;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ship.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            ship.transform.position = endPos;
        }

        /// <summary>
        /// Получаем «геометрический центр» корабля, глядя на клетки, которые он занимает.
        /// Раньше это было в GameController, но можно перенести сюда или дублировать.
        /// </summary>
        private Vector3 GetShapeCenter(ShipController ship)
        {
            if (_gameController == null) 
                return ship.transform.position;

            var field = _gameController.Field;
            var models = ship.ShipCellModels;
            if (models.Count == 0)
                return ship.transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var m in models)
            {
                var cell = field.FindCellByModel(m);
                if (cell != null)
                {
                    sum += cell.transform.position;
                    count++;
                }
            }

            if (count == 0)
                return ship.transform.position;
            return sum / count;
        }
    }
}
