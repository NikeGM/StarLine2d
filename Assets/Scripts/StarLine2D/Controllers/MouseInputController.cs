using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarLine2D.Controllers
{
    public class MouseInputController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool propagate;
        
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[100];
        private readonly bool[] _hasChild = new bool[100];
        private readonly int[] _parents = new int[100];
        private readonly bool[] _isT = new bool[100];
        
        private int _hitsCount;
        
        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }


        public IEnumerable<(T, GameObject)> FilterHits<T>()
        {
            for (var i = 0; i < _hitsCount; i++)
            {
                _hasChild[i] = false;
                _parents[i] = -1;

                var go = _hits[i].collider.gameObject;
                var t = go.GetComponent<T>();
                _isT[i] = t != null;
            }

            for (var i = 0; i < _hitsCount; i++)
            {
                var goI = _hits[i].collider.gameObject;
                if (!_isT[i]) continue;
                
                for (var j = 0; j < _hitsCount; j++)
                {
                    var goJ = _hits[j].collider.gameObject;
                    if (goI == goJ) continue;
                    if (!_isT[j]) continue;
                    if (!goJ.transform.IsChildOf(goI.transform)) continue;
                    
                    _hasChild[i] = true;
                    _parents[j] = i;
                    break;
                }
            }
            
            for (var i = 0; i < _hitsCount; i++)
            {
                if (!_isT[i]) continue;
                if (_hasChild[i]) continue;

                var target = _hits[i].collider.gameObject;
                var current = i;
                while (current >= 0)
                {
                    var go = _hits[current].collider.gameObject;
                    var ts = go.GetComponents<T>();
                    foreach (var t in ts)
                    {
                        yield return (t, target);
                    }

                    current = _parents[current];
                    if (!propagate) break;
                }
            }
        }
        
        private void Update()
        {
            UpdateHits();
        }

        public void UpdateHits()
        {
            if (mainCamera is null || Pointer.current == null)
            {
                return;
            }
            
            Vector3 mouse = Pointer.current.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(mouse);
            
            _hitsCount = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, _hits);
        }
    }
}