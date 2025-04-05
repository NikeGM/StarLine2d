using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class CellsStateManager : MonoBehaviour
    {
        [SerializeField] private FieldController fieldController;
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private PositionManager positionManager;

        private ZoneModel _currentZone;
        private readonly HashSet<CellController> _zoneCells = new HashSet<CellController>();
        private readonly Dictionary<string, StaticCellModel> _staticCells = new Dictionary<string, StaticCellModel>();
        private CellController _hoveredCell;

        private const string DefaultProfile = "default";
        private const string ZoneProfile = "zone";
        private const string WeaponHoverZoneProfile = "weapon-hover-zone";
        private const string WeaponHoverProfile = "weapon-hover";
        public const string WeaponActiveProfile = "weapon-active";
        private const string MoveHoverProfile = "move-hover";
        public const string MoveActiveProfile = "move-active";
        public const string MoveZone = "move-zone";
        public const string WeaponZone = "weapon-zone";

        public HashSet<CellController> ZoneCells => _zoneCells;
        public ZoneModel Zone => _currentZone;

        private void Awake()
        {
            if (!fieldController) Debug.LogError($"[{name}] FieldController не назначен в CellsStateManager.");
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory не назначен в CellsStateManager.");
            if (!positionManager) Debug.LogError($"[{name}] PositionManager не назначен в CellsStateManager.");
        }

        private void Render()
        {
            if (fieldController?.Cells != null)
            {
                foreach (var cell in fieldController.Cells)
                {
                    cell.SpriteCompound.SetProfile(DefaultProfile);
                }
            }

            if (_currentZone != null && _zoneCells.Count > 0)
            {
                foreach (var cell in _zoneCells)
                {
                    cell.SpriteCompound.SetProfile(ZoneProfile);
                }
            }

            bool hoverInZone = _hoveredCell && _currentZone != null && _zoneCells.Contains(_hoveredCell);
            if (_currentZone?.WeaponType == "Beam" && hoverInZone && fieldController)
            {
                var hoverLine = fieldController.GetLine(_currentZone.Center, _hoveredCell);
                foreach (var cell in hoverLine)
                {
                    cell.SpriteCompound.SetProfile(WeaponHoverZoneProfile);
                }
            }

            if (hoverInZone)
            {
                var hoverState = (_currentZone.Type == MoveZone) ? MoveHoverProfile : WeaponHoverProfile;
                _hoveredCell.SpriteCompound.SetProfile(hoverState);
                var playerShip = GetPlayerShip();

                if (_currentZone.Type == MoveZone && playerShip)
                {
                    var shapeCells = GetShapeCells(playerShip.ShipShape, _hoveredCell);
                    if (shapeCells.Count == 0)
                    {
                        _hoveredCell.SpriteCompound.SetProfile(ZoneProfile);
                    }
                    else
                    {
                        bool entireShapeInZone = shapeCells.All(sc => _zoneCells.Contains(sc));
                        if (entireShapeInZone)
                        {
                            foreach (var sc in shapeCells)
                            {
                                sc.SpriteCompound.SetProfile(MoveHoverProfile);
                            }
                        }
                        else
                        {
                            _hoveredCell.SpriteCompound.SetProfile(ZoneProfile);
                        }
                    }
                }
            }

            foreach (var staticCell in _staticCells.Values)
            {
                staticCell.Cell.SpriteCompound.SetProfile(staticCell.Type);
            }
        }

        public void SetZone(CellController center, int radius, string type, string weaponType)
        {
            if (!fieldController) return;

            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = type,
                WeaponType = weaponType
            };

            _zoneCells.Clear();

            if (type == MoveZone)
            {
                var playerShip = GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = GetShipCells(playerShip);

                    foreach (var cellInShip in shipCells)
                    {
                        var neighbors = fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    unionSet.RemoveWhere(cell => !positionManager.IsCellFree(cell));

                    if (!IsSingleCellShip(playerShip))
                    {
                        var validCells = new HashSet<CellController>();
                        foreach (var candidate in unionSet)
                        {
                            if (CanShipFitInCandidate(playerShip, candidate))
                            {
                                validCells.Add(candidate);
                            }
                        }
                        unionSet = validCells;
                    }

                    _zoneCells.UnionWith(unionSet);
                }
            }
            else if (type == WeaponZone)
            {
                var playerShip = GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = GetShipCells(playerShip);

                    foreach (var cellInShip in shipCells)
                    {
                        var neighbors = fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    _zoneCells.UnionWith(unionSet);
                }
            }

            Render();
        }

        public void SetWeaponZoneForFuturePosition(List<CellController> shapeCells, int radius, string weaponType)
        {
            if (!fieldController) return;

            var center = (shapeCells.Count > 0) ? shapeCells[0] : null;
            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = WeaponZone,
                WeaponType = weaponType
            };

            _zoneCells.Clear();

            var unionSet = new HashSet<CellController>();
            foreach (var sc in shapeCells)
            {
                var neighbors = fieldController.GetNeighbors(sc, radius);
                neighbors.Add(sc);
                unionSet.UnionWith(neighbors);
            }

            _zoneCells.UnionWith(unionSet);
            Render();
        }

        public void ClearZone()
        {
            _currentZone = null;
            _zoneCells.Clear();
            Render();
        }

        public void AddStaticCell(string id, CellController cell, string type)
        {
            if (!cell) return;
            _staticCells[id] = new StaticCellModel { Cell = cell, Type = type };
            Render();
        }

        public void RemoveStaticCell(string id)
        {
            if (_staticCells.Remove(id))
            {
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

        public void OnCellClicked(CellController clickedCell)
        {
            if (_currentZone != null && _currentZone.Type == MoveZone)
            {
                var playerShip = GetPlayerShip();
                if (playerShip != null)
                {
                    if (CanShipStandInAnyOrientation(playerShip, clickedCell))
                    {
                        playerShip.MoveCell = clickedCell;
                    }
                    else
                    {
                        Debug.Log($"Невозможно встать на клетку {clickedCell}.");
                    }
                }
            }
        }

        private ShipController GetPlayerShip()
        {
            return shipFactory.GetPlayerShip();
        }

        private List<CellController> GetShipCells(ShipController ship)
        {
            var result = new List<CellController>();
            if (!ship) return result;
            if (!fieldController) return result;

            foreach (var model in ship.ShipCellModels)
            {
                var c = fieldController.FindCellByModel(model);
                if (c != null)
                {
                    result.Add(c);
                }
            }

            return result;
        }

        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (!fieldController || !headCell) return new List<CellController>();

            var result = new List<CellController> { headCell };

            if (shape == ShipShape.HorizontalR)
            {
                var leftModel = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                var leftCell = fieldController.FindCellByModel(leftModel);
                if (!leftCell) return new List<CellController>();
                result.Add(leftCell);
            }
            else if (shape == ShipShape.HorizontalL)
            {
                var rightModel = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                var rightCell = fieldController.FindCellByModel(rightModel);
                if (!rightCell) return new List<CellController>();
                result.Add(rightCell);
            }

            return result;
        }

        private bool IsSingleCellShip(ShipController ship)
        {
            var cells = GetShipCells(ship);
            return (cells.Count <= 1);
        }

        private bool CheckShipCanStand(ShipController ship, CellController headCell)
        {
            if (!headCell) return false;
            if (!positionManager.IsCellFree(headCell)) return false;

            var shapeCells = GetShapeCells(ship.ShipShape, headCell);
            if (shapeCells.Count == 0) return false;
            if (shapeCells.Any(c => c == null || !positionManager.IsCellFree(c))) return false;

            return true;
        }

        private bool CanShipStandInAnyOrientation(ShipController ship, CellController headCell)
        {
            var oldShape = ship.ShipShape;
            foreach (ShipShape possibleShape in System.Enum.GetValues(typeof(ShipShape)))
            {
                ship.SetShipShape(possibleShape);
                if (CheckShipCanStand(ship, headCell))
                {
                    ship.SetShipShape(oldShape);
                    return true;
                }
            }

            ship.SetShipShape(oldShape);
            return false;
        }

        private bool CanShipFitInCandidate(ShipController ship, CellController candidate)
        {
            var currentShipCells = GetShipCells(ship);
            if (currentShipCells.Count == 0) return false;

            var oldShape = ship.ShipShape;
            foreach (ShipShape shape in System.Enum.GetValues(typeof(ShipShape)))
            {
                ship.SetShipShape(shape);
                foreach (var shipCell in currentShipCells)
                {
                    var offset = new Vector3Int(candidate.Q - shipCell.Q, candidate.R - shipCell.R, candidate.S - shipCell.S);
                    if (CheckPlacementWithoutCollision(currentShipCells, offset))
                    {
                        ship.SetShipShape(oldShape);
                        return true;
                    }
                }
            }

            ship.SetShipShape(oldShape);
            return false;
        }

        private bool CheckPlacementWithoutCollision(List<CellController> currentShipCells, Vector3Int offset)
        {
            if (!fieldController) return false;

            foreach (var c in currentShipCells)
            {
                var newQ = c.Q + offset.x;
                var newR = c.R + offset.y;
                var newS = c.S + offset.z;

                var newCellModel = fieldController.CubeGridModel.FindCellModel(newQ, newR, newS);
                if (newCellModel == null) return false;

                var newCell = fieldController.FindCellByModel(newCellModel);
                if (!newCell || !positionManager.IsCellFree(newCell)) return false;
            }

            return true;
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
    }
}
