using System.Collections.Generic;
using JetBrains.Annotations;
using StarLine2D.Components;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class CellsStateController : MonoBehaviour
    {
        private FieldController _fieldController;

        private ZoneModel _currentZone;
        private Dictionary<string, StaticCellModel> _staticCells;
        private CellController _hoveredCell;

        public const string MoveZone = "move-zone";
        public const string WeaponZone = "weapon-zone";
        public const string MovePoint = "move-point";
        public const string ShootPoint = "shoot-point";

        public ZoneModel Zone => _currentZone;

        private void Awake()
        {
            _fieldController = GetComponent<FieldController>();
            if (_fieldController == null)
            {
                Debug.LogError("FieldController not found on the same GameObject.");
            }

            _staticCells = new Dictionary<string, StaticCellModel>();
        }

        public void Render()
        {
            foreach (var cell in _fieldController.Cells)
            {
                cell.DisplayState.SetState("default");
            }

            if (_currentZone != null)
            {
                foreach (var cell in _fieldController.GetNeighbors(_currentZone.Center, _currentZone.Radius))
                {
                    cell.DisplayState.SetState(_currentZone.Type);
                }
            }

            if (_currentZone != null && _hoveredCell != null &&
                _fieldController.IsCellInZone(_hoveredCell, _currentZone.Center, _currentZone.Radius))
            {
                _hoveredCell.DisplayState.SetState(_currentZone.Type == MoveZone ? "move-hover" : "weapon-hover", 0.4f);
            }

            if (_currentZone?.WeaponType == "Beam" && _currentZone.Center && _hoveredCell)
            {
                var hoverZone = _fieldController.GetLine(_currentZone.Center, _hoveredCell);
                Debug.Log("Hover zone " + hoverZone + " to " + hoverZone.Count );
                foreach (var cell in hoverZone)
                {
                    cell.DisplayState.SetState("hover-zone");
                }
            }
            
            foreach (var staticCell in _staticCells.Values)
            {
                staticCell.Cell.DisplayState.SetState(staticCell.Type);
            }
        }

        public void SetZone(CellController center, int radius, string type, string weaponType)
        {
            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = type,
                WeaponType = weaponType
            };
            Render();
        }

        public void ClearZone()
        {
            _currentZone = null;
            Render();
        }

        public void AddStaticCell(string id, CellController cell, string type)
        {
            if (_staticCells.ContainsKey(id))
            {
                _staticCells[id] = new StaticCellModel
                {
                    Cell = cell,
                    Type = type
                };
            }
            else
            {
                _staticCells.Add(id, new StaticCellModel
                {
                    Cell = cell,
                    Type = type
                });
            }

            Render();
        }

        public void RemoveStaticCell(string id)
        {
            if (_staticCells.ContainsKey(id))
            {
                _staticCells.Remove(id);
                Render();
            }
        }

        public void ClearStaticCells()
        {
            _staticCells.Clear();
            Render();
        }

        public void SetHoveredCell(CellController cell)
        {
            _hoveredCell = cell;
            Render();
        }

        public class ZoneModel
        {
            public CellController Center { get; set; }
            public int Radius { get; set; }
            public string Type { get; set; }
            public string WeaponType { get; set; }
        }

        private class StaticCellModel
        {
            public CellController Cell { get; set; }
            public string Type { get; set; }
        }

        private class HoverZoneModel
        {
            public List<CellController> Cells { get; set; } = new List<CellController>();
        }
    }
}