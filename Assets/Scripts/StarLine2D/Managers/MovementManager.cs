using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class MovementManager : MonoBehaviour
    {
        [SerializeField] private float defaultMoveDuration = 2.0f;
        [SerializeField] private FieldController fieldController;
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private AsteroidFactory asteroidFactory;

        private void Awake()
        {
            if (defaultMoveDuration <= 0f)
            {
                Debug.LogError($"[{name}] defaultMoveDuration не должен быть <= 0.");
            }

            if (!fieldController)
            {
                Debug.LogError($"[{name}] FieldController не назначен.");
            }

            if (!shipFactory)
            {
                Debug.LogError($"[{name}] ShipFactory не назначен.");
            }

            if (!asteroidFactory)
            {
                Debug.LogError($"[{name}] AsteroidFactory не назначен.");
            }
        }

        public IEnumerator MoveAllShipsAndAsteroids()
        {
            var ships = shipFactory.GetSpawnedShips();
            var asteroids = asteroidFactory.GetSpawnedAsteroids();

            var movementCoroutines = ships.Select(ship => StartCoroutine(MoveShip(ship))).ToList();
            movementCoroutines.AddRange(
                from asteroid in asteroids
                where asteroid && fieldController
                select StartCoroutine(MoveAsteroidHalfTurn(asteroid, defaultMoveDuration))
            );

            foreach (var cor in movementCoroutines)
            {
                yield return cor;
            }
        }

        private IEnumerator MoveShip(ShipController ship)
        {
            if (!ship || !ship.MoveCell) yield break;
            var oldCenter = ship.transform.position;
            if (fieldController) oldCenter = GetShapeCenter(ship, fieldController);
            var newCell = ship.MoveCell;
            ship.MoveCell = null;
            ship.PositionCell = newCell;
            var newCenter = newCell.transform.position;
            if (fieldController) newCenter = GetShapeCenter(ship, fieldController);
            var direction = newCenter - oldCenter;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ship.transform.rotation = Quaternion.Euler(0, 0, angle);
            yield return SmoothMove(ship, oldCenter, newCenter, defaultMoveDuration);
            var playerCtrl = ship.GetComponent<PlayerController>();
            ship.transform.rotation = Quaternion.Euler(0, 0, playerCtrl ? 0 : 180);
        }

        private IEnumerator SmoothMove(ShipController ship, Vector3 startPos, Vector3 endPos, float duration)
        {
            if (duration <= 0.0001f)
            {
                ship.transform.position = endPos;
                yield break;
            }

            var elapsed = 0f;
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
            if (!ship || !field) return ship ? ship.transform.position : Vector3.zero;
            var models = ship.ShipCellModels;
            if (models.Count == 0) return ship.transform.position;
            var sum = Vector3.zero;
            var count = 0;
            foreach (var cell in models.Select(field.FindCellByModel).Where(cell => cell))
            {
                sum += cell.transform.position;
                count++;
            }

            if (count == 0) return ship.transform.position;
            return sum / count;
        }

        private IEnumerator MoveAsteroidHalfTurn(AsteroidController asteroid, float moveDuration)
        {
            if (!asteroid || asteroid.Hp <= 0) yield break;
            if (!fieldController) yield break;
            var currentCell = asteroid.PositionCell;
            if (!currentCell) yield break;
            var nextQ = currentCell.Q + asteroid.Direction.Q;
            var nextR = currentCell.R + asteroid.Direction.R;
            var nextS = currentCell.S + asteroid.Direction.S;
            var nextCellModel = new CubeCellModel(nextQ, nextR, nextS);
            var nextCell = fieldController.FindCellByModel(nextCellModel);
            if (!nextCell)
            {
                Destroy(asteroid.gameObject);
                yield break;
            }

            asteroid.StorePreviousCell(asteroid.PositionCell);
            if (nextCell == currentCell)
            {
                asteroid.UpdateArrowDirection(asteroid.transform.position, asteroid.transform.position);
                yield break;
            }

            var halfTurnAngle = asteroid.RotateClockwise ? -180f : 180f;
            var oldRot = asteroid.transform.rotation;
            var newRot = oldRot * Quaternion.Euler(0, 0, halfTurnAngle);
            var oldPos = asteroid.transform.position;
            var newPos = nextCell.transform.position;
            asteroid.UpdateArrowDirection(oldPos, newPos);
            yield return StartCoroutine(
                asteroid.UpdateTransformSmooth(
                    oldPos,
                    newPos,
                    oldRot,
                    newRot,
                    moveDuration
                )
            );
            asteroid.SetPositionCell(nextCell);
        }
    }
}