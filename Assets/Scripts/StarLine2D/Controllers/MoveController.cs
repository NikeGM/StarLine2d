using System;
using System.Collections;
using StarLine2D.Utils.Extensions;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class MoveController : MonoBehaviour
    {
        [SerializeField] private ShipController ship;
        [SerializeField] private float durationRotation = 1f;
        [SerializeField] private float durationPosition = 1f;
        [SerializeField] private bool turnFirst = true;

        [SerializeField] private BoolVector3 freezeRotation;
        [SerializeField] private BoolVector3 freezePosition;

        private Coroutine _positionCoroutine;
        private Coroutine _rotationCoroutine;
        private Coroutine _compositeCoroutine;

        public void GoTo(Vector3 target)
        {
            if (_compositeCoroutine != null)
            {
                StopCoroutine(_compositeCoroutine);
                StopCoroutine(_positionCoroutine);
                StopCoroutine(_rotationCoroutine);
            }
            
            _compositeCoroutine = StartCoroutine(GoToSequence(target));
        }

        private IEnumerator GoToSequence(Vector3 target)
        {
            LookAt(target);
            if (turnFirst)
            {
                yield return _rotationCoroutine;
            }
            
            MoveAt(target);
            if (turnFirst)
            {
                yield return _positionCoroutine;
            }

            if (!turnFirst)
            {
                var longest = durationRotation > durationPosition ? _rotationCoroutine : _positionCoroutine;
                yield return longest;
            }
        }
        
        public IEnumerator GoTo(Transform target)
        {
            yield return GoToSequence(target.position);
        }

        public IEnumerator GoTo(GameObject target)
        {
            yield return GoToSequence(target.transform.position);
        }

        public void MoveAt(Vector3 targetPosition)
        {
            if (_positionCoroutine != null)
            {
                StopCoroutine(_positionCoroutine);
            }
            
            var initialPosition = ship.transform.position;

            if (freezePosition.X) targetPosition.x = initialPosition.x;
            if (freezePosition.Y) targetPosition.y = initialPosition.y;
            if (freezePosition.Z) targetPosition.z = initialPosition.z;

            _positionCoroutine = this.DoLerp(0, 1, durationPosition, time => LerpPosition(initialPosition, targetPosition, time));
        }

        private void LerpPosition(Vector3 a, Vector3 b, float t)
        {
            ship.transform.position = Vector3.Lerp(a, b, t);
        }

        public void MoveAt(Transform target)
        {
            MoveAt(target.position);
        }

        public void MoveAt(GameObject target)
        {
            MoveAt(target.transform);
        }

        public void LookAt(Vector3 target)
        {
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }

            var lookPos = Quaternion.Euler(0, 0, 90) * (target - ship.transform.position);

            var initialRotation = ship.transform.rotation;
            var targetRotation = Quaternion.LookRotation(Vector3.forward, lookPos);

            if (freezeRotation.X) targetRotation.x = initialRotation.x;
            if (freezeRotation.Y) targetRotation.y = initialRotation.y;
            if (freezeRotation.Z) targetRotation.z = initialRotation.z;

            _rotationCoroutine = this.DoLerp(0, 1, durationRotation, time => LerpRotation(initialRotation, targetRotation, time));
        }

        private void LerpRotation(Quaternion initialRotation, Quaternion targetRotation, float time)
        {
            ship.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, time);
        }

        public void LookAt(Transform target)
        {
            LookAt(target.position);
        }

        public void LookAt(GameObject target)
        {
            LookAt(target.transform);
        }

        [Serializable]
        private class BoolVector3
        {
            [SerializeField] private bool x = false;
            [SerializeField] private bool y = false;
            [SerializeField] private bool z = false;

            public bool X => x;
            public bool Y => y;
            public bool Z => z;
        }
    }
}