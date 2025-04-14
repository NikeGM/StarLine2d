using UnityEngine;
using System;
using StarLine2D.Utils.Disposable;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Данные о префабе препятствия.
    /// Похоже на ShipPrefabData — у нас может быть несколько вариантов префабов.
    /// </summary>
    [Serializable]
    public class ObstaclePrefabData
    {
        public GameObject prefab;
    }
}

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Компонент «препятствия» (статический объект).
    /// Например, может иметь какую-то графику, коллайдер и т.д.
    /// </summary>
    public class ObstacleController : MonoBehaviour
    {
        public CellController PositionCell { get; set; }

        // Логика для подписок на уничтожение.
        private Action _onDestroy;
        private readonly CompositeDisposable _trash = new();

        /// <summary>
        /// Метод для подписки на событие уничтожения.
        /// Аналогично AsteroidController / ShipController.
        /// </summary>
        public ActionDisposable Subscribe(Action call)
        {
            _onDestroy += call;
            var disposable = new ActionDisposable(() => _onDestroy -= call);
            _trash.Retain(disposable);
            return disposable;
        }

        /// <summary>
        /// Вызывается при уничтожении GameObject. 
        /// Сначала вызываются все подписки, затем очищаются Disposable.
        /// </summary>
        private void OnDestroy()
        {
            _onDestroy?.Invoke();
            _trash?.Dispose();
        }
    }
}