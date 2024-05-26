using System;
using UnityEngine.Events;

namespace StarLine2D.Utils.Disposable.Extensions
{
    public static class UnityEventExtension
    {
        public static IDisposable Subscribe(this UnityEvent unityEvent, UnityAction call)
        {
            unityEvent.AddListener(call);
            return new ActionDisposable(() => unityEvent.RemoveListener(call));
        }

        public static IDisposable Subscribe<TType>(this UnityEvent<TType> unityEvent, UnityAction<TType> call)
        {
            unityEvent.AddListener(call);
            return new ActionDisposable(() => unityEvent.RemoveListener(call));
        }

        public static IDisposable Subscribe(this UnityEvent unityEvent, Action call)
        {
            return unityEvent.Subscribe(new UnityAction(call));
        }

        public static IDisposable Subscribe<TType>(this UnityEvent<TType> unityEvent, Action<TType> call)
        {
            return unityEvent.Subscribe<TType>(new UnityAction<TType>(call));
        }
    }
}