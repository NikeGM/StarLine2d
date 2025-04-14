using System.Collections;
using System.Collections.Generic;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Managers
{
    /// <summary>
    /// Менеджер движения кораблей и астероидов.
    /// </summary>
    public class MovementManager : MonoBehaviour
    {
        [SerializeField] private float defaultMoveDuration = 2.0f;
        [SerializeField] private FieldController fieldController;
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private AsteroidFactory asteroidFactory;

        /// <summary>
        /// Корутину движения можно вызывать, когда нужно.
        /// Например, из TurnManager.
        /// </summary>
        public IEnumerator MoveAllShipsAndAsteroids()
        {
            var movementCoroutines = new List<Coroutine>();
            var ships = shipFactory.GetSpawnedShips();
            var asteroids = asteroidFactory.GetSpawnedAsteroids();
            
            // Движение кораблей
            foreach (var ship in ships)
            {
                movementCoroutines.Add(StartCoroutine(MoveShip(ship)));
            }

            // Движение / поворот астероидов
            foreach (var asteroid in asteroids)
            {
                if (asteroid && fieldController)
                {
                    // Вместо asteroid.MoveSmoothlyWithHalfTurn(...) —
                    // теперь всё считаем здесь и вызываем корутину из MovementManager.
                    movementCoroutines.Add(StartCoroutine(
                        MoveAsteroidHalfTurn(asteroid, defaultMoveDuration)
                    ));
                }
            }

            // Дождёмся, пока все завершатся
            foreach (var cor in movementCoroutines)
            {
                yield return cor;
            }
        }

        private IEnumerator MoveShip(ShipController ship)
        {
            if (!ship || !ship.MoveCell)
                yield break;

            // ... (логика движения корабля как раньше)
            // (Старый центр, новый центр, поворот и т.д.)

            // Старый центр
            Vector3 oldCenter = ship.transform.position;
            if (fieldController)
                oldCenter = GetShapeCenter(ship, fieldController);

            // Новая клетка
            var newCell = ship.MoveCell;
            ship.MoveCell = null; 
            ship.PositionCell = newCell;

            // Новый центр
            Vector3 newCenter = newCell.transform.position;
            if (fieldController)
                newCenter = GetShapeCenter(ship, fieldController);

            // Поворот корпуса
            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);

            yield return SmoothMove(ship, oldCenter, newCenter, defaultMoveDuration);

            // Повернуть "лицом вперёд" (для игрока) или "назад" (для врагов)
            var playerCtrl = ship.GetComponent<PlayerController>();
            if (playerCtrl != null)
                ship.transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                ship.transform.rotation = Quaternion.Euler(0, 0, 180);
        }

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

        private Vector3 GetShapeCenter(ShipController ship, FieldController field)
        {
            if (!ship || !field) 
                return ship ? ship.transform.position : Vector3.zero;

            var models = ship.ShipCellModels;
            if (models.Count == 0)
                return ship.transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var m in models)
            {
                var cell = field.FindCellByModel(m);
                if (cell)
                {
                    sum += cell.transform.position;
                    count++;
                }
            }

            if (count == 0)
                return ship.transform.position;

            return sum / count;
        }

        // --------------------------------------------------------------------
        // Новые методы для АСТЕРОИДОВ:
        // --------------------------------------------------------------------

        /// <summary>
        /// Пример логики, аналогичной старому MoveSmoothlyWithHalfTurn:
        /// - Находим nextCell по direction
        /// - Проверяем, не вышли за границы
        /// - Уничтожаем (или игнорируем движение), если препятствие
        /// - Плавно двигаем астероид
        /// - Выполняем «пол-оборота» (half turn)
        /// - Обновляем PositionCell внутри AsteroidController
        /// </summary>
        private IEnumerator MoveAsteroidHalfTurn(AsteroidController asteroid, float moveDuration)
        {
            if (!asteroid || asteroid.Hp <= 0) yield break;
            if (!fieldController) yield break;

            var currentCell = asteroid.PositionCell;
            if (!currentCell) yield break;  // вдруг уже нет клетки

            // Считаем nextCell
            int nextQ = currentCell.Q + asteroid.Direction.Q;
            int nextR = currentCell.R + asteroid.Direction.R;
            int nextS = currentCell.S + asteroid.Direction.S;

            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = fieldController.FindCellByModel(nextCellModel);

            // Если следующей ячейки нет — уничтожаем (вышли за границы)
            if (!nextCell)
            {
                Destroy(asteroid.gameObject);
                yield break;
            }

            // Если там препятствие, не двигаемся (или уничтожаем — выбирайте логику)
            if (nextCell.HasObstacle)
            {
                yield break;
            }

            // Если nextCell == currentCell, вообще нет движения
            if (nextCell == currentCell)
            {
                // Можем спрятать стрелку
                asteroid.UpdateArrowDirection(asteroid.transform.position, asteroid.transform.position);
                yield break;
            }

            // Подготавливаем поворот
            float halfTurnAngle = asteroid.RotateClockwise ? -180f : 180f;

            // Текущий угол спрайта
            var oldRot = asteroid.transform.rotation; // или _asteroidSpriteTransform.rotation
            var newRot = oldRot * Quaternion.Euler(0, 0, halfTurnAngle);

            // Старое и новое положение
            var oldPos = asteroid.transform.position;
            var newPos = nextCell.transform.position;

            // Обновим стрелку (если есть)
            asteroid.UpdateArrowDirection(oldPos, newPos);

            // Запускаем плавное перемещение + плавный поворот 
            yield return StartCoroutine(
                asteroid.UpdateTransformSmooth(
                    startPos: oldPos,
                    endPos: newPos,
                    startRot: oldRot,
                    endRot: newRot,
                    duration: moveDuration
                )
            );

            // По окончании движения фиксируем новую клетку
            asteroid.SetPositionCell(nextCell);
        }
    }
}
