using System;
using System.Collections;
using UnityEngine;

namespace StarLine.Utils.Extensions
{
    public static class MonoBehaviourExtensions
    {
        public static Coroutine SetTimeout(this MonoBehaviour monoBehaviour, Action action, float timeout)
        {
            return monoBehaviour.StartCoroutine(TimeoutCoroutine(action, timeout));
        }
        
        private static IEnumerator TimeoutCoroutine(Action action, float timeout)
        {
            yield return new WaitForSeconds(timeout);
            action?.Invoke();
        }

        public static Coroutine DoFrames(this MonoBehaviour monoBehaviour, float length, Action<float> onFrame, Action onFinished=null)
        {
            return monoBehaviour.StartCoroutine(FramesCoroutine(length, onFrame, onFinished));
        }

        private static IEnumerator FramesCoroutine(float length, Action<float> onFrame, Action onFinished=null)
        {
            var time = 0f;
            while (time < length)
            {
                time += Time.deltaTime;
                var progress = time / length;
                onFrame?.Invoke(progress);

                yield return null;
            }
            
            onFinished?.Invoke();
        }

        public static Coroutine DoLerp(this MonoBehaviour monoBehaviour, float start, float end, float time, Action<float> onFrame)
        {
            return monoBehaviour.DoFrames(time, progress =>
            {
                var lerpValue = Mathf.Lerp(start, end, progress);
                onFrame?.Invoke(lerpValue);
            });
        }
    }
}