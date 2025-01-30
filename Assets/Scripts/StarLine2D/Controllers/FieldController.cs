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
        [SerializeField] private CellsStateController cellsStateController;
        [SerializeField] [Range(1, 30)] private int gridWidth = 30;
        [SerializeField] [Range(1, 25)] private int gridHeight = 25;

        private OnClickComponent _onClick;
        private List<CellController> _cells = new();
        private CubeGridModel cubeGridModel;

        public OnClickComponent OnClick => _onClick;
        public List<CellController> Cells => _cells;
        public CubeGridModel CubeGridModel => cubeGridModel;
        public CellsStateController CellStateController => cellsStateController;

        private bool _initialized;
        private float _cellHeight;
        private float _cellWidth;

        public void Initialize()
        {
            if (_initialized) return;

            cubeGridModel = new CubeGridModel(gridWidth, gridHeight);
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
            return _cells.FirstOrDefault(
                cell => cell.Q == cellModel.Q && cell.R == cellModel.R && cell.S == cellModel.S);
        }

        public Vector3 GetCellPosition(CubeCellModel cellModel)
        {
            var fieldWidth = _cellWidth * gridWidth;
            var fieldHeight = (gridHeight + 1) * _cellWidth + (gridHeight - 1) * _cellHeight / 2;

            var posY = -_cellHeight * 3f / 2 * cellModel.R;
            var posX = -_cellHeight * (Mathf.Sqrt(3) / 2 * cellModel.R + Mathf.Sqrt(3) * cellModel.S);

            return new Vector3(posX - fieldWidth + _cellWidth / 2, posY + fieldHeight / 2, 0);
        }

        private void CalcCellSize()
        {
            var tmpCell = Instantiate(cellPrefab, transform);
            var cellCollider = tmpCell.GetComponent<Collider2D>();
            var offset = 0.1f;
            _cellHeight = (cellCollider.bounds.size.y + offset) / 2;
            _cellWidth = _cellHeight * Mathf.Sqrt(3) / 2;
            Debug.Log(_cellWidth + " " + _cellHeight);
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

        public bool IsCellInZone(CellController cell, CellController center, int radius)
        {
            if (cell == null || center == null) return false;

            var neighbors = GetNeighbors(center, radius);
            return neighbors.Contains(cell);
        }

        public int GetDistance(CellController start, CellController end)
        {
            return cubeGridModel.GetDistance(
                cubeGridModel.FindCellModel(start.Q, start.R, start.S),
                cubeGridModel.FindCellModel(end.Q, end.R, end.S)
            );
        }

        public List<CellController> GetLine(CellController start, CellController end)
        {
            var pathModel = cubeGridModel.GetLine(
                cubeGridModel.FindCellModel(start.Q, start.R, start.S),
                cubeGridModel.FindCellModel(end.Q, end.R, end.S)
            );
            return pathModel.Select(FindCellByModel).ToList();
        }

        public List<CellController> GetWeaponZone(Weapon weapon, CellController shootCell, CellController positionCell)
        {
            var zone = new List<CellController>();
            if (weapon.Type == WeaponType.Beam)
            {
                return GetLine(positionCell, shootCell);
            }

            zone.Add(shootCell);
            return zone;
        }

        [ContextMenu("Generate Grid")]
        private List<CellController> GenerateGrid()
        {
            CalcCellSize();
            ClearGrid();

            var model = new CubeGridModel(gridWidth, gridHeight);
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