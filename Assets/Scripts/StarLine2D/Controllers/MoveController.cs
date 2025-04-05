using System;
using System.Collections;
using StarLine2D.Utils.Extensions;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class MoveController : MonoBehaviour
    {
        [SerializeField] private float durationRotation = 1f;
        [SerializeField] private float durationPosition = 1f;
        [SerializeField] private bool turnFirst = true;
        [SerializeField] private BoolVector3 freezeRotation;
        [SerializeField] private BoolVector3 freezePosition;

        private Coroutine _positionCoroutine;
        private Coroutine _rotationCoroutine;
        private Coroutine _compositeCoroutine;

        private void Awake()
        {
            if (durationRotation <= 0f)
            {
                Debug.LogError($"[{name}] durationRotation не задан или меньше/равен нулю.");
            }
            if (durationPosition <= 0f)
            {
                Debug.LogError($"[{name}] durationPosition не задан или меньше/равен нулю.");
            }
        }

        public void GoTo(Vector3 target)
        {
            if (_compositeCoroutine != null) StopCoroutine(_compositeCoroutine);
            if (_positionCoroutine != null) StopCoroutine(_positionCoroutine);
            if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
            _compositeCoroutine = StartCoroutine(GoToSequence(target));
        }

        private IEnumerator GoToSequence(Vector3 target)
        {
            LookAt(target);
            if (turnFirst) yield return _rotationCoroutine;
            MoveAt(target);
            if (turnFirst)
            {
                yield return _positionCoroutine;
            }
            else
            {
                var longest = durationRotation > durationPosition ? _rotationCoroutine : _positionCoroutine;
                yield return longest;
            }
        }

        public void MoveAt(Vector3 targetPosition)
        {
            if (_positionCoroutine != null) StopCoroutine(_positionCoroutine);
            var initialPosition = transform.position;
            if (freezePosition.X) targetPosition.x = initialPosition.x;
            if (freezePosition.Y) targetPosition.y = initialPosition.y;
            if (freezePosition.Z) targetPosition.z = initialPosition.z;
            _positionCoroutine = this.DoLerp(0, 1, durationPosition, t => LerpPosition(initialPosition, targetPosition, t));
        }

        private void LerpPosition(Vector3 a, Vector3 b, float t)
        {
            transform.position = Vector3.Lerp(a, b, t);
        }

        public void LookAt(Vector3 target)
        {
            if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
            var lookPos = Quaternion.Euler(0, 0, 90) * (target - transform.position);
            var initialRotation = transform.rotation;
            var targetRotation = Quaternion.LookRotation(Vector3.forward, lookPos);
            if (freezeRotation.X) targetRotation.x = initialRotation.x;
            if (freezeRotation.Y) targetRotation.y = initialRotation.y;
            if (freezeRotation.Z) targetRotation.z = initialRotation.z;
            _rotationCoroutine = this.DoLerp(0, 1, durationRotation, t => LerpRotation(initialRotation, targetRotation, t));
        }

        private void LerpRotation(Quaternion initialRotation, Quaternion targetRotation, float time)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, time);
        }

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
