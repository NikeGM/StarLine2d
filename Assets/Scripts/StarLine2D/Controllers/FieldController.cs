using System.Collections.Generic;
using System.Linq;
using StarLine2D.Components;
using StarLine2D.Models;
using UnityEditor;
using UnityEngine;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(OnClickAggregateComponent))]
    public class FieldController : MonoBehaviour
    {
        [SerializeField] private CellController cellPrefab;
        [SerializeField] [Range(1, 15)] private int gridRadius = 5;

        private OnClickAggregateComponent _onClick;
        private List<CellController> _cells = new();
        private CubeGridModel cubeGridModel;

        public OnClickAggregateComponent OnClick => _onClick;
        public List<CellController> Cells => _cells;

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;
            
            cubeGridModel = new CubeGridModel(gridRadius);
            _cells = GetComponentsInChildren<CellController>().ToList();
            _cells.ForEach(item => item.Initialize());

            _onClick = GetComponent<OnClickAggregateComponent>();
            
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
            var posX = 3f / 2 * cellModel.Q;
            var posY = 0f;
            var posZ = Mathf.Sqrt(3) / 2 * cellModel.Q + Mathf.Sqrt(3) * cellModel.R;
            return new Vector3(posX, posY, posZ);
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
            ClearGrid();

            var model = new CubeGridModel(gridRadius);
            var cellControllers = new List<CellController>();

            foreach (var cell in model.Cells.Values)
            {
                var position = GetCellPosition(cell);
                var hex = PrefabUtility.InstantiatePrefab(cellPrefab.gameObject, transform) as GameObject;

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
