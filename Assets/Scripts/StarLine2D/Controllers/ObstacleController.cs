using UnityEngine;
using System;

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

        // Если нужно что-то ещё, например, урон при коллизии, HP и т.п. — добавляйте.
    }
}