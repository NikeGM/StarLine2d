using System.Collections.Generic;
using System.Linq;
using StarLine2D.Components;
using StarLine2D.Models;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(OnClickComponent))]
    public class FieldController : MonoBehaviour
    {
        [SerializeField] private CellController cellPrefab;
        [SerializeField] [Range(1, 15)] private int gridRadius = 5;

        private OnClickComponent _onClick;
        private List<CellController> _cells = new();
        private CubeGridModel cubeGridModel;

        public OnClickComponent OnClick => _onClick;
        public List<CellController> Cells => _cells;

        private bool _initialized = false;
        private float _cellSize;

        public void Initialize()
        {
            if (_initialized) return;
            
            cubeGridModel = new CubeGridModel(gridRadius);
            _cells = GetComponentsInChildren<CellController>().ToList();
            _cells.ForEach(item => item.Initialize());

            _onClick = GetComponent<OnClickComponent>();
            
            _initialized = true;
        }

        private void Start()
        {
            Initialize();
        }

        public List<CellController> GetRandomCells(int count)
        {
            if (count > _cells.Count)
            {
                return new List<CellController>();
            }

            var randomCells = _cells.OrderBy(_ => Random.value).Take(count).ToList();
            return randomCells;
        }

        public CellController FindCellByModel(CubeCellModel cellModel)
        {
            return _cells.FirstOrDefault(cell => cell.Q == cellModel.Q && cell.R == cellModel.R && cell.S == cellModel.S);
        }

        public Vector3 GetCellPosition(CubeCellModel cellModel)
        {
            var posY = _cellSize * 3f / 2 * cellModel.Q;
            var posX = _cellSize * (Mathf.Sqrt(3) / 2 * cellModel.Q + Mathf.Sqrt(3) * cellModel.R);
            
            return new Vector3(posX, posY, 0);
        }
        
        private void CalcCellSize()
        {
            var tmpCell = Instantiate(cellPrefab, transform);
            var cellCollider = tmpCell.GetComponent<Collider2D>();
            _cellSize = cellCollider.bounds.size.y / 2;
            DestroyImmediate(tmpCell);
        }

        public List<CellController> GetNeighbors(CellController cell, int radius)
        {
            var cellModel = cubeGridModel.FindCellModel(cell.Q, cell.R, cell.S);
            if (cellModel == null)
            {
                return new List<CellController>();
            }

            var neighborModels = cubeGridModel.GetCellsInRadius(cellModel, radius);
            var neighborControllers = neighborModels
                .Select(FindCellByModel)
                .Where(controller => controller is not null)
                .ToList();

            return neighborControllers;
        }
        
        [ContextMenu("Generate Grid")]
        private List<CellController> GenerateGrid()
        {
            CalcCellSize();
            ClearGrid();

            var model = new CubeGridModel(gridRadius);
            var cellControllers = new List<CellController>();

            foreach (var cell in model.Cells.Values)
            {
                var position = GetCellPosition(cell);
                GameObject hex;

                #if UNITY_EDITOR
                hex = PrefabUtility.InstantiatePrefab(cellPrefab.gameObject, transform) as GameObject;
                #else
                hex = Instantiate(cellPrefab.gameObject, transform);
                #endif

                if (!hex) continue;
                hex.transform.position = position;
                hex.name = $"CubeCell_{cell.Q}_{cell.R}_{cell.S}";

                var cellController = hex.GetComponent<CellController>();
                cellController.Initialize();
                cellController.SetCoords(cell.Q, cell.R, cell.S);
                
                cellControllers.Add(cellController);
            }

            return cellControllers;
        }

        [ContextMenu("Clear Grid")]
        private void ClearGrid()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
