using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Components
{
    public class DisplayStateComponent : MonoBehaviour
    {
        [SerializeField] private List<DisplayStateItem> states = new();

        private string _currentState;

        private void Awake()
        {
            if (states.Count == 0) return;

            _currentState = states[0].Name;
            SetState(states[0].Name);
        }

        public bool HasState(string stateName)
        {
            return states.Any(item => item.Name == stateName);
        }
        
        public virtual void SetState(string stateName)
        {
            if (!HasState(stateName)) return;
            if (stateName == _currentState) return;
 
            Flush();
            GameObject go = states.Find(item => item.Name == stateName).GameObject;
            go.SetActive(true);
            
            _currentState = stateName;
        }

        public virtual string GetCurrentState()
        {
            return _currentState;
        }

        public virtual bool Is(string stateName)
        {
            return GetCurrentState() == stateName;
        }

        private void Flush()
        {
            foreach (DisplayStateItem item in states)
            {
                item.GameObject.SetActive(false);
            }
        }

        [Serializable]
        private class DisplayStateItem
        {
            [SerializeField] private string name;
            [SerializeField] private GameObject gameObject;

            public string Name => name;
            public GameObject GameObject => gameObject;
        }
    }
}