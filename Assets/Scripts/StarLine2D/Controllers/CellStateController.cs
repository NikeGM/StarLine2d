using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class CellsStateController : MonoBehaviour
    {
        private FieldController _fieldController;
        [SerializeField] private GameController _gameController;

        private ZoneModel _currentZone;
        private HashSet<CellController> _zoneCells;
        private Dictionary<string, StaticCellModel> _staticCells;
        private CellController _hoveredCell;

        // ---------- ПРОФИЛИ ----------
        public const string DefaultProfile         = "default";
        public const string ZoneProfile            = "zone";
        public const string WeaponHoverZoneProfile = "weapon-hover-zone";
        public const string WeaponHoverProfile     = "weapon-hover";
        public const string WeaponActiveProfile    = "weapon-active"; 
        public const string MoveHoverProfile       = "move-hover";
        public const string MoveActiveProfile      = "move-active";

        // ---------- ТИПЫ ЗОНЫ ----------
        public const string MoveZone   = "move-zone";
        public const string WeaponZone = "weapon-zone";
        
        // Публичное свойство, чтобы можно было проверять «ячейки зоны» извне
        public HashSet<CellController> ZoneCells => _zoneCells;

        public ZoneModel Zone => _currentZone;

        private void Awake()
        {
            _fieldController = GetComponent<FieldController>();
            if (_fieldController == null)
            {
                Debug.LogError("FieldController not found on the same GameObject.");
            }

            if (_gameController == null)
            {
                _gameController = FindObjectOfType<GameController>();
                if (_gameController == null)
                {
                    Debug.LogError("GameController not found in scene.");
                }
            }

            _staticCells = new Dictionary<string, StaticCellModel>();
            _zoneCells   = new HashSet<CellController>();
        }

        /// <summary>
        /// Главный метод, который «рисует» клетки (подсветку) на поле.
        /// </summary>
        public void Render()
        {
            // 1) Сбрасываем (default) все клетки
            foreach (var cell in _fieldController.Cells)
            {
                cell.SpriteCompound.SetProfile(DefaultProfile);
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
            if (_currentZone?.WeaponType == "Beam" && hoverInZone)
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
                if (_currentZone.Type == MoveZone && _gameController != null)
                {
                    var playerShip = _gameController.GetPlayerShip();
                    if (playerShip != null)
                    {
                        // Проверяем, влезает ли вся форма (текущая ориентация) в _zoneCells_
                        var shapeCells = _gameController.GetShapeCells(playerShip.ShipShape, _hoveredCell);
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
                                // Если форма не влезает — показываем обычный профиль «зоны»
                                _hoveredCell.SpriteCompound.SetProfile(ZoneProfile);
                            }
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

        /// <summary>
        /// Задаём «зону» для перемещения или стрельбы.
        /// </summary>
        public void SetZone(CellController center, int radius, string type, string weaponType)
        {
            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = type,
                WeaponType = weaponType
            };
            _zoneCells.Clear();

            // Если это зона перемещения (MoveZone)
            if (type == MoveZone && _gameController != null)
            {
                var playerShip = _gameController.GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = _gameController.GetShipCells(playerShip);

                    // 1) Собираем все клетки в радиусе от каждой клетки, которую занимает корабль
                    foreach (var cellInShip in shipCells)
                    {
                        // Получаем все клетки в "геометрическом" радиусе (без учёта препятствий)
                        var neighbors = _fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    // 2) Убираем из unionSet те клетки, которые САМИ по себе являются препятствием
                    unionSet.RemoveWhere(cell => cell.HasObstacle);

                    // 3) Если корабль одноклеточный — ничего дополнительно не проверяем
                    if (!IsSingleCellShip(playerShip))
                    {
                        // Если корабль больше 1 клетки, делаем дополнительную проверку «налезания»
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
            else if (type == WeaponZone && _gameController != null)
            {
                var playerShip = _gameController.GetPlayerShip();
                if (playerShip != null)
                {
                    var unionSet = new HashSet<CellController>();
                    var shipCells = _gameController.GetShipCells(playerShip);

                    foreach (var cellInShip in shipCells)
                    {
                        // Для оружия — своя логика, берём все клетки в радиусе
                        // без фильтрации препятствий
                        var neighbors = _fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }

                    _zoneCells = unionSet;
                }
            }

            Render();
        }

        /// <summary>
        /// Проверяем, может ли корабль (учитывая ShipShape) встать головной клеткой на headCell 
        /// (чтобы все клетки формы были свободны).
        /// </summary>
        private bool CheckShipCanStand(ShipController ship, CellController headCell)
        {
            if (!headCell) return false;
            if (headCell.HasObstacle) return false;

            // Получаем список клеток, которые займёт корабль
            var shapeCells = _gameController.GetShapeCells(ship.ShipShape, headCell);
            if (shapeCells == null || shapeCells.Count == 0)
                return false;

            // Проверяем, что все эти клетки существуют и не заняты препятствиями
            foreach (var c in shapeCells)
            {
                if (!c) return false;
                if (c.HasObstacle) return false;
            }

            return true;
        }

        /// <summary>
        /// Если корабль может менять ориентацию, пробуем все варианты. 
        /// Возвращаем true, если хотя бы в одной ориентации форма влезает.
        /// </summary>
        private bool CanShipStandInAnyOrientation(ShipController ship, CellController headCell)
        {
            var oldShape = ship.ShipShape;

            foreach (ShipShape shape in System.Enum.GetValues(typeof(ShipShape)))
            {
                ship.SetShipShape(shape);
                if (CheckShipCanStand(ship, headCell))
                {
                    // Возвращаем обратно исходную ориентацию
                    ship.SetShipShape(oldShape);
                    return true;
                }
            }

            ship.SetShipShape(oldShape);
            return false;
        }

        /// <summary>
        /// Устанавливаем зону оружия для будущей позиции корабля (прим. для двухклетного). 
        /// </summary>
        public void SetWeaponZoneForFuturePosition(List<CellController> shapeCells, int radius, string weaponType)
        {
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

        /// <summary>
        /// Пример метода, вызываемого при клике на клетку.
        /// Вы можете вызывать его из CellController.OnMouseDown или из UI.
        /// </summary>
        public void OnCellClicked(CellController clickedCell)
        {
            // Если активна "MoveZone", то при клике попытаемся двинуть корабль
            if (_currentZone != null && _currentZone.Type == MoveZone && _gameController != null)
            {
                var playerShip = _gameController.GetPlayerShip();
                if (playerShip != null)
                {
                    // Проверяем, можем ли встать (при любой ориентации) 
                    if (CanShipStandInAnyOrientation(playerShip, clickedCell))
                    {
                        playerShip.MoveCell = clickedCell;
                        Debug.Log($"Установили перемещение корабля на клетку {clickedCell}.");
                    }
                    else
                    {
                        Debug.Log($"Невозможно встать на клетку {clickedCell}. Вероятно, препятствие или форма не помещается.");
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // Доп. методы по новой логике «налезания» (при большом корабле)
        // --------------------------------------------------------------------

        /// <summary>
        /// Проверяем, является ли корабль одноклеточным. Для иллюстрации —
        /// считаем, что если GetShipCells возвращает ровно 1 клетку, корабль одноклеточный.
        /// </summary>
        private bool IsSingleCellShip(ShipController ship)
        {
            var cells = _gameController.GetShipCells(ship);
            return (cells.Count <= 1);
        }

        /// <summary>
        /// Проверяем, может ли корабль встать хоть в одном «варианте совмещения»,
        /// когда мы пытаемся приравнять любую клетку корабля к candidate.
        /// </summary>
        private bool CanShipFitInCandidate(ShipController ship, CellController candidate)
        {
            var currentShipCells = _gameController.GetShipCells(ship);
            if (currentShipCells == null || currentShipCells.Count == 0)
                return false;

            var oldShape = ship.ShipShape;

            // Перебираем все ориентации, если нужно
            foreach (ShipShape shape in System.Enum.GetValues(typeof(ShipShape)))
            {
                ship.SetShipShape(shape);

                // Перебираем каждую клетку, занимаемую кораблём сейчас
                foreach (var shipCell in currentShipCells)
                {
                    // Вычисляем смещение (offset) в Q, R, S
                    Vector3Int offset = new Vector3Int(
                        candidate.Q - shipCell.Q,
                        candidate.R - shipCell.R,
                        candidate.S - shipCell.S
                    );

                    // Проверяем, можем ли мы «сместить» весь корабль на этот offset без коллизий
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

        /// <summary>
        /// Проверяем, нет ли препятствий при смещении всех текущих клеток корабля на указанный offset.
        /// </summary>
        private bool CheckPlacementWithoutCollision(List<CellController> currentShipCells, Vector3Int offset)
        {
            foreach (var c in currentShipCells)
            {
                var newQ = c.Q + offset.x;
                var newR = c.R + offset.y;
                var newS = c.S + offset.z;

                var newCellModel = _fieldController.CubeGridModel.FindCellModel(newQ, newR, newS);
                if (newCellModel == null) 
                    return false; // Выходим за границы поля

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
