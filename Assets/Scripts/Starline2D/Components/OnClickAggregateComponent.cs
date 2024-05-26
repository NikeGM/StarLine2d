using System;
using System.Collections.Generic;
using System.Linq;
using StarLine.Utils;
using StarLine.Utils.Disposable;
using UnityEngine;
using UnityEngine.Events;

namespace StarLine.Components
{
    public class OnClickAggregateComponent : MonoBehaviour
    {
        [SerializeField] private ClickEvent action;
        private Action<GameObject> _onClicked;

        private readonly List<OnClickComponent> _children = new();
        private readonly CompositeDisposable _trash = new();

        private void Start()
        {
            _children.AddRange(GetComponentsInChildren<OnClickComponent>());
            
            foreach (var child in _children)
            {
                _trash.Retain(child.Subscribe(OnChildClicked));
            }
        }

        private void OnChildClicked(GameObject obj)
        {
            _onClicked.Invoke(obj);
        }

        public ActionDisposable Subscribe(Action<GameObject> call)
        {
            _onClicked += call;
            return new ActionDisposable(() => _onClicked -= call);
        }

        [Serializable]
        private class ClickEvent : UnityEvent<IClickable> {}
    }
}