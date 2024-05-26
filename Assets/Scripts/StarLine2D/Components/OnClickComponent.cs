using System;
using StarLine2D.Utils;
using StarLine2D.Utils.Disposable;
using UnityEngine;
using UnityEngine.Events;

namespace StarLine2D.Components
{
    public class OnClickComponent : MonoBehaviour, IClickable
    {
        [SerializeField] private ClickEvent action;
        private Action<GameObject> _onClicked;
        
        public void OnPerformed(GameObject target)
        {
            action?.Invoke(target);
            _onClicked?.Invoke(target);
        }

        public ActionDisposable Subscribe(Action<GameObject> call)
        {
            _onClicked += call;
            return new ActionDisposable(() => _onClicked -= call);
        }

        public ActionDisposable SubscribeAndInvoke(Action<GameObject> call)
        {
            call?.Invoke(gameObject);
            return Subscribe(call);
        }
        
        [Serializable]
        private class ClickEvent : UnityEvent<GameObject> {}
    }
}