using System.Collections.Generic;
using System.Linq;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class CellsStateManager : MonoBehaviour
    {
        [SerializeField] private FieldController _fieldController;
        [SerializeField] private ShipFactory _shipFactory;
        private ZoneModel _currentZone;

        // Инициализация коллекций на месте, без вызова методов Initialize().
        private HashSet<CellController> _zoneCells = new HashSet<CellController>();
        private Dictionary<string, StaticCellModel> _staticCells = new Dictionary<string, StaticCellModel>();

        private CellController _hoveredCell;

        // ---------- ПРОФИЛИ ----------
        public const string DefaultProfile = "default";
        public const string ZoneProfile = "zone";
        public const string WeaponHoverZoneProfile = "weapon-hover-zone";
        public const string WeaponHoverProfile = "weapon-hover";
        public const string WeaponActiveProfile = "weapon-active";
        public const string MoveHoverProfile = "move-hover";
        public const string MoveActiveProfile = "move-active";

        // ---------- ТИПЫ ЗОНЫ ----------
        public const string MoveZone = "move-zone";
        public const string WeaponZone = "weapon-zone";

        public HashSet<CellController> ZoneCells => _zoneCells;
        public ZoneModel Zone => _currentZone;

        // --------------------------------------------------------------------
        // Главный метод, который «рисует» клетки (подсветку) на поле.
        // --------------------------------------------------------------------
        public void Render()
        {
            // 1) Сбрасываем (default) все клетки
            if (_fieldController?.Cells != null)
            {
                foreach (var cell in _fieldController.Cells)
                {
                    cell.SpriteCompound.SetProfile(DefaultProfile);
                }
            }

            // 2) Подсветка всех клеток «зоны»
            if (_currentZone != null && _zoneCells != null)
            {
                foreach (var cell in _zoneCells)
                {
                    cell.SpriteCompound.SetProfile(ZoneProfile);
                }
            }

            // 3) Логика "hover" (подсветка при наведении)
            bool hoverInZone = (_hoveredCell != null
                                && _currentZone != null
                                && _zoneCells.Contains(_hoveredCell));

            // Если WeaponZone с лучевым оружием (Beam) и мы «ховерим» внутри зоны
            if (_currentZone?.WeaponType == "Beam" && hoverInZone && _fieldController != null)
            {
                var hoverLine = _fieldController.GetLine(_currentZone.Center, _hoveredCell);
                foreach (var cell in hoverLine)
                {
                    cell.SpriteCompound.SetProfile(WeaponHoverZoneProfile);
                }
            }

            // 3б) Подсветка самой hovered-клетки
            if (hoverInZone)
            {
                // Если MoveZone => MoveHoverProfile, иначе WeaponHoverProfile
                string hoverState = (_currentZone.Type == MoveZone) ? MoveHoverProfile : WeaponHoverProfile;
                _hoveredCell.SpriteCompound.SetProfile(hoverState);

                // Доп. правило: если это MoveZone и корабль двухклетный (или более)
                var playerShip = GetPlayerShip();
                if (_currentZone.Type == MoveZone && playerShip != null)
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

            // 4) Статические клетки (MoveActiveProfile, WeaponActiveProfile, и т.д.)
            foreach (var staticCell in _staticCells.Values)
            {
                staticCell.Cell.SpriteCompound.SetProfile(staticCell.Type);
            }
        }

        // --------------------------------------------------------------------
        // Задаём «зону» для перемещения или стрельбы.
        // --------------------------------------------------------------------
        public void SetZone(CellController center, int radius, string type, string weaponType)
        {
            Debug.Log("ZONEEE1!!!" + type);
            if (_fieldController == null)
                return;

            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = type,
                WeaponType = weaponType
            };

            _zoneCells.Clear();

            // Если это зона перемещения (MoveZone)
            if (type == MoveZone)
            {
                var playerShip = GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = GetShipCells(playerShip);

                    // Собираем все клетки в радиусе от каждой клетки, которую занимает корабль
                    foreach (var cellInShip in shipCells)
                    {
                        var neighbors = _fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    // Убираем клетки с препятствиями
                    unionSet.RemoveWhere(cell => cell.HasObstacle);

                    // Если корабль больше одной клетки
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

                    _zoneCells = unionSet;
                }
            }
            // Если это зона оружия (WeaponZone)
            else if (type == WeaponZone)
            {
                var playerShip = GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = GetShipCells(playerShip);

                    foreach (var cellInShip in shipCells)
                    {
                        var neighbors = _fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    _zoneCells = unionSet;
                }
            }

            Render();
        }

        // --------------------------------------------------------------------
        // Ставим зону оружия, учитывая будущую позицию (список shapeCells).
        // --------------------------------------------------------------------
        public void SetWeaponZoneForFuturePosition(List<CellController> shapeCells, int radius, string weaponType)
        {
            if (_fieldController == null)
                return;

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
                var neighbors = _fieldController.GetNeighbors(sc, radius);
                neighbors.Add(sc);
                unionSet.UnionWith(neighbors);
            }

            _zoneCells = unionSet;
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
            if (cell == null)
                return;

            if (!_staticCells.ContainsKey(id))
            {
                _staticCells.Add(id, new StaticCellModel
                {
                    Cell = cell,
                    Type = type
                });
            }
            else
            {
                _staticCells[id] = new StaticCellModel
                {
                    Cell = cell,
                    Type = type
                };
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

        /// <summary>
        /// Пример метода, вызываемого при клике на клетку.
        /// </summary>
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
                        Debug.Log($"Установили перемещение корабля на клетку {clickedCell}.");
                    }
                    else
                    {
                        Debug.Log($"Невозможно встать на клетку {clickedCell}. Форма не помещается или препятствие.");
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // Вспомогательные методы (заменяют вызовы _gameController)
        // --------------------------------------------------------------------

        /// <summary>
        /// Ищем объект с PlayerController, берём у него ShipController.
        /// </summary>
        private ShipController GetPlayerShip()
        {
            return _shipFactory.GetPlayerShip();
        }

        /// <summary>
        /// Возвращает клетки, соответствующие ShipCellModels корабля.
        /// </summary>
        private List<CellController> GetShipCells(ShipController ship)
        {
            var result = new List<CellController>();
            if (!ship) return result;
            if (_fieldController == null) return result;

            foreach (var model in ship.ShipCellModels)
            {
                var c = _fieldController.FindCellByModel(model);
                if (c != null)
                {
                    result.Add(c);
                }
            }

            return result;
        }

        /// <summary>
        /// Строим форму корабля (Single, HorizontalR, HorizontalL) относительно headCell.
        /// </summary>
        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            var field = _fieldController;
            var result = new List<CellController>();
            if (!field || !headCell) return result;

            // Всегда включаем headCell
            result.Add(headCell);

            switch (shape)
            {
                case ShipShape.Single:
                    // Одноклеточный
                    break;

                case ShipShape.HorizontalR:
                {
                    // (Q-1, R, S+1)
                    var leftModel = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                    var leftCell = field.FindCellByModel(leftModel);
                    if (leftCell == null)
                        return new List<CellController>(); // пусто, если не влезает
                    result.Add(leftCell);
                    break;
                }
                case ShipShape.HorizontalL:
                {
                    // (Q+1, R, S-1)
                    var rightModel = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                    var rightCell = field.FindCellByModel(rightModel);
                    if (rightCell == null)
                        return new List<CellController>();
                    result.Add(rightCell);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Проверяем, является ли корабль одноклеточным.
        /// </summary>
        private bool IsSingleCellShip(ShipController ship)
        {
            var cells = GetShipCells(ship);
            return (cells.Count <= 1);
        }

        /// <summary>
        /// Проверяем, может ли корабль встать головой на headCell (с учётом препятствий).
        /// </summary>
        private bool CheckShipCanStand(ShipController ship, CellController headCell)
        {
            if (!headCell) return false;
            if (headCell.HasObstacle) return false;

            var shapeCells = GetShapeCells(ship.ShipShape, headCell);
            if (shapeCells.Count == 0) return false;

            // Все клетки должны существовать и не иметь препятствий
            if (shapeCells.Any(c => c == null || c.HasObstacle))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяем все ориентации, если корабль может менять форму.
        /// </summary>
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

        /// <summary>
        /// Проверяем, может ли корабль вместиться при смещении клеток.
        /// (Используется для многоячейного корабля в SetZone).
        /// </summary>
        private bool CanShipFitInCandidate(ShipController ship, CellController candidate)
        {
            var currentShipCells = GetShipCells(ship);
            if (currentShipCells.Count == 0)
                return false;

            var oldShape = ship.ShipShape;
            foreach (ShipShape shape in System.Enum.GetValues(typeof(ShipShape)))
            {
                ship.SetShipShape(shape);
                foreach (var shipCell in currentShipCells)
                {
                    var offset = new Vector3Int(
                        candidate.Q - shipCell.Q,
                        candidate.R - shipCell.R,
                        candidate.S - shipCell.S
                    );

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
            if (_fieldController == null)
                return false;

            foreach (var c in currentShipCells)
            {
                var newQ = c.Q + offset.x;
                var newR = c.R + offset.y;
                var newS = c.S + offset.z;

                var newCellModel = _fieldController.CubeGridModel.FindCellModel(newQ, newR, newS);
                if (newCellModel == null)
                    return false;

                var newCell = _fieldController.FindCellByModel(newCellModel);
                if (newCell == null || newCell.HasObstacle)
                {
                    return false;
                }
            }

            return true;
        }

        // --------------------------------------------------------------------
        // Вспомогательные классы
        // --------------------------------------------------------------------
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