using System.Collections.Generic;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class TouchInputController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool propagate = false;
        
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[100];
        private readonly bool[] _hasChild = new bool[100];
        private readonly int[] _parents = new int[100];
        private readonly bool[] _isT = new bool[100];
        
        private int _hitsCount = 0;
        
        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Start()
        {
            Debug.Log("[TouchInputController] TouchInputController initialized.");
        }

        public IEnumerable<(T, GameObject)> FilterHits<T>()
        {
            if (_hitsCount > 0)
            {
                Debug.Log("[TouchInputController] FilterHits<{typeof(T).Name}> started. _hitsCount: {_hitsCount}");
            }
            
            // Логика обработки касаний
            for (var i = 0; i < _hitsCount; i++)
            {
                _hasChild[i] = false;
                _parents[i] = -1;

                var go = _hits[i].collider.gameObject;
                var t = go.GetComponent<T>();
                _isT[i] = t != null;

                Debug.Log($"[TouchInputController] Hit {_hits[i].collider.name}. Has Component<{typeof(T).Name}>: {_isT[i]}");
            }

            // Проверяем, является ли каждый объект родителем для других объектов
            for (var i = 0; i < _hitsCount; i++)
            {
                var goI = _hits[i].collider.gameObject;
                if (!_isT[i]) continue;

                Debug.Log($"[TouchInputController] Object {_hits[i].collider.name} has Component<{typeof(T).Name}>.");

                for (var j = 0; j < _hitsCount; j++)
                {
                    var goJ = _hits[j].collider.gameObject;
                    if (goI == goJ) continue;
                    if (!_isT[j]) continue;
                    if (!goJ.transform.IsChildOf(goI.transform)) continue;
                    
                    _hasChild[i] = true;
                    _parents[j] = i;

                    Debug.Log($"[TouchInputController] Object {_hits[j].collider.name} is child of {_hits[i].collider.name}.");
                    
                    break;
                }
            }
            
            // Выводим отфильтрованные результаты
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
                        Debug.Log($"[TouchInputController] Filtered result: ({t}, {target})");
                        yield return (t, target);
                    }

                    current = _parents[current];
                    if (!propagate) break;
                }
            }

            // Debug.Log($"[TouchInputController] FilterHits<{typeof(T).Name}> completed.");
        }
        
        private void Update()
        {
            if (mainCamera is null)
            {
                Debug.Log($"Main camera is null");
                return;
            }
            
            // Обработка ввода с тачскрина
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector3 touchPosition = mainCamera.ScreenToWorldPoint(touch.position);
                var ray = mainCamera.ScreenPointToRay(touch.position);
                
                _hitsCount = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, _hits);
                
                Debug.Log($"[TouchInputController] Update: Touch position - {touchPosition}, Ray origin - {ray.origin}, Ray direction - {ray.direction}, Hits count - {_hitsCount}");
                
                // Логируем каждое попадание для отладки
                for (int j = 0; j < _hitsCount; j++)
                {
                    Debug.Log($"[TouchInputController] Hit {_hits[j].collider.name} at {_hits[j].point}.");
                }
            }
        }
    }
}
