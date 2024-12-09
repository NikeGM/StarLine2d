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
        
        private void Start()
        {
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("Collider not found on hex object: " + gameObject.name);
            }
            else if (!collider.enabled)
            {
                Debug.LogWarning("Collider is disabled on hex object: " + gameObject.name);
            }
        }
        
        public void OnPerformed(GameObject target)
        {
            Debug.Log("OnPerformed called on: " + target.name);  // Добавьте это отладочное сообщение
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