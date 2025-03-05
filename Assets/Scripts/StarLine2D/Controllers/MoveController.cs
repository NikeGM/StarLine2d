using System;
using System.Collections;
using StarLine2D.Utils.Extensions;
using UnityEngine;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Универсальный контроллер перемещений и поворотов объекта в 2D.
    /// Не требует ссылок из инспектора: работает с transform того объекта,
    /// на котором установлен данный скрипт.
    /// </summary>
    public class MoveController : MonoBehaviour
    {
        [Header("Настройки анимации движения и поворота")]
        [SerializeField] private float durationRotation = 1f;   // Длительность анимации поворота
        [SerializeField] private float durationPosition = 1f;   // Длительность анимации перемещения

        [Tooltip("Если true, сначала заканчиваем поворот, потом - перемещение; иначе параллельно.")]
        [SerializeField] private bool turnFirst = true;

        [Header("Заморозка осей (false - ось не заморожена)")]
        [SerializeField] private BoolVector3 freezeRotation;
        [SerializeField] private BoolVector3 freezePosition;

        private Coroutine _positionCoroutine;
        private Coroutine _rotationCoroutine;
        private Coroutine _compositeCoroutine;

        /// <summary>
        /// Запустить последовательность "смотри на цель и иди к цели" (позиция в 2D/3D).
        /// </summary>
        /// <param name="target">Мировая позиция, куда смотреть/идти.</param>
        public void GoTo(Vector3 target)
        {
            // Остановим старые корутины, если они были
            if (_compositeCoroutine != null)
            {
                StopCoroutine(_compositeCoroutine);
            }
            if (_positionCoroutine != null)
            {
                StopCoroutine(_positionCoroutine);
            }
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }

            _compositeCoroutine = StartCoroutine(GoToSequence(target));
        }

        /// <summary>
        /// Корутин, запускающий поворот и перемещение
        /// в зависимости от флага turnFirst.
        /// </summary>
        private IEnumerator GoToSequence(Vector3 target)
        {
            // Сначала поворачиваемся к цели
            LookAt(target);

            if (turnFirst)
            {
                // Ждём завершения анимации поворота
                yield return _rotationCoroutine;
            }

            // Начинаем движение
            MoveAt(target);

            if (turnFirst)
            {
                // Ждём завершения анимации движения
                yield return _positionCoroutine;
            }
            else
            {
                // Иначе ждём ту корутину, которая идёт дольше
                var longest = durationRotation > durationPosition ? _rotationCoroutine : _positionCoroutine;
                yield return longest;
            }
        }

        /// <summary>
        /// Запустить только перемещение к указанной позиции (без поворота).
        /// </summary>
        public void MoveAt(Vector3 targetPosition)
        {
            // Останавливаем старую корутину движения, если была
            if (_positionCoroutine != null)
            {
                StopCoroutine(_positionCoroutine);
            }

            var initialPosition = transform.position;

            // Если нужно зафиксировать какую-то ось — копируем её из начальной позиции
            if (freezePosition.X) targetPosition.x = initialPosition.x;
            if (freezePosition.Y) targetPosition.y = initialPosition.y;
            if (freezePosition.Z) targetPosition.z = initialPosition.z;

            // Запускаем линейную интерполяцию
            _positionCoroutine = this.DoLerp(
                0,
                1,
                durationPosition,
                time => LerpPosition(initialPosition, targetPosition, time)
            );
        }

        private void LerpPosition(Vector3 a, Vector3 b, float t)
        {
            transform.position = Vector3.Lerp(a, b, t);
        }

        /// <summary>
        /// Запустить только поворот к заданной позиции (2D-ориентация).
        /// </summary>
        public void LookAt(Vector3 target)
        {
            // Останавливаем старую корутину поворота, если была
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }

            // В 2D используем LookRotation, где "вперёд" — Vector3.forward,
            // а вектор направления дополнительно поворачиваем на 90 градусов
            // (опционально, если нужно смотреть "носом" вверх и т.д.)
            var lookPos = Quaternion.Euler(0, 0, 90) * (target - transform.position);

            var initialRotation = transform.rotation;
            var targetRotation = Quaternion.LookRotation(Vector3.forward, lookPos);

            // При заморозке осей копируем соответствующие компоненты из исходного вращения
            if (freezeRotation.X) targetRotation.x = initialRotation.x;
            if (freezeRotation.Y) targetRotation.y = initialRotation.y;
            if (freezeRotation.Z) targetRotation.z = initialRotation.z;

            _rotationCoroutine = this.DoLerp(
                0,
                1,
                durationRotation,
                time => LerpRotation(initialRotation, targetRotation, time)
            );
        }

        private void LerpRotation(Quaternion initialRotation, Quaternion targetRotation, float time)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, time);
        }

        /// <summary>
        /// Вспомогательная структура-флаг для блокировки отдельных осей
        /// при выполнении трансформа (перемещения или поворота).
        /// </summary>
        [Serializable]
        private class BoolVector3
        {
            [SerializeField] private bool x;
            [SerializeField] private bool y;
            [SerializeField] private bool z;

            public bool X => x;
            public bool Y => y;
            public bool Z => z;
        }
    }
}
