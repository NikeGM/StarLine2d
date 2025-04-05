using System;
using System.Collections.Generic;
using UnityEngine;
using StarLine2D.Utils.Disposable;
using StarLine2D.Managers;

namespace StarLine2D.Controllers
{
    [Serializable]
    public class ObstaclePrefabData
    {
        public GameObject prefab;
    }

    public class ObstacleController : MonoBehaviour, ICollisionParticipant
    {
        [SerializeField] private CellController positionCell;
        private Action _onDestroy;
        private readonly CompositeDisposable _trash = new();

        public IEnumerable<CellController> DesiredCells
        {
            get
            {
                if (positionCell != null) yield return positionCell;
            }
        }

        public float Mass => 0f;
        public bool IsObstacle => true;

        public CellController PositionCell
        {
            get => positionCell;
            set => positionCell = value;
        }

        public int OnDamage(int dmg)
        {
            Debug.Log("Obstacle is indestructible => ignore damage.");
            return 0;
        }

        public ActionDisposable Subscribe(Action call)
        {
            _onDestroy += call;
            var disposable = new ActionDisposable(() => _onDestroy -= call);
            _trash.Retain(disposable);
            return disposable;
        }

        private void OnDestroy()
        {
            _onDestroy?.Invoke();
            positionCell = null;
            _trash.Dispose();
        }
    }
}