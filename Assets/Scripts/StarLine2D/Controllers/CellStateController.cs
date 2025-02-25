using System.Collections.Generic;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class CellsStateController : MonoBehaviour
    {
        private FieldController _fieldController;

        [SerializeField] private GameController _gameController;

        // Текущая зона (MoveZone / WeaponZone и т.п.)
        private ZoneModel _currentZone;

        // Все клетки, входящие в текущую «зону»
        private HashSet<CellController> _zoneCells;

        // Статические клетки (MovePoint, WeaponPoint и т.д.)
        private Dictionary<string, StaticCellModel> _staticCells;

        // Текущая клетка под курсором (hover)
        private CellController _hoveredCell;

        // ---------- ПРОФИЛИ для SpriteCompound ----------
        public const string DefaultProfile         = "default";
        public const string ZoneProfile            = "zone";
        public const string WeaponHoverZoneProfile = "weapon-hover-zone";
        public const string WeaponHoverProfile     = "weapon-hover";
        public const string WeaponActiveProfile    = "weapon-active"; // заменяет ShootPoint
        public const string MoveHoverProfile       = "move-hover";
        public const string MoveActiveProfile      = "move-active";   // заменяет MovePoint
        
        // ---------- ТИПЫ ЗОНЫ ----------
        public const string MoveZone   = "move-zone";
        public const string WeaponZone = "weapon-zone";
        
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
        /// Отрисовывает все клетки: зона, ховер, статические метки и т.п.
        /// </summary>
        public void Render()
        {
            // 1) Сброс состояния (ставим профиль "default" для всех клеток)
            foreach (var cell in _fieldController.Cells)
            {
                cell.SpriteCompound.SetProfile(DefaultProfile);
            }

            // 2) Подсветка всех клеток зоны (если зона есть)
            if (_currentZone != null && _zoneCells != null)
            {
                foreach (var cell in _zoneCells)
                {
                    cell.SpriteCompound.SetProfile(ZoneProfile);
                }
            }

            // 3) Логика "hover"
            bool hoverInZone = (_hoveredCell != null 
                                && _currentZone != null
                                && _zoneCells.Contains(_hoveredCell));

            // Если оружие лучевое (Beam) и мы ховеримся внутри зоны
            if (_currentZone?.WeaponType == "Beam" && hoverInZone)
            {
                var hoverLine = _fieldController.GetLine(_currentZone.Center, _hoveredCell);
                foreach (var cell in hoverLine)
                {
                    cell.SpriteCompound.SetProfile(WeaponHoverZoneProfile);
                }
            }

            // Подсветка самой hovered-клетки
            if (hoverInZone)
            {
                // Если это зона перемещения, используем MoveHoverProfile, иначе WeaponHoverProfile
                string hoverState = (_currentZone.Type == MoveZone) ? MoveHoverProfile : WeaponHoverProfile;
                _hoveredCell.SpriteCompound.SetProfile(hoverState);

                // Доп. правило: если это MoveZone, проверяем «двухклеточный корабль»
                if (_currentZone.Type == MoveZone && _gameController != null)
                {
                    var playerShip = _gameController.GetPlayerShip();
                    if (playerShip != null)
                    {
                        var shapeCells = _gameController.GetShapeCells(playerShip.ShipShape, _hoveredCell);

                        // Если форма пустая, просто оставляем клетку как "zone"
                        if (shapeCells.Count == 0)
                        {
                            _hoveredCell.SpriteCompound.SetProfile(ZoneProfile);
                        }
                        else
                        {
                            bool entireShapeInZone = true;
                            foreach (var sc in shapeCells)
                            {
                                if (!_zoneCells.Contains(sc))
                                {
                                    entireShapeInZone = false;
                                    break;
                                }
                            }
                            if (entireShapeInZone)
                            {
                                // Все клетки формы выделяем "move-hover"
                                foreach (var sc in shapeCells)
                                {
                                    sc.SpriteCompound.SetProfile(MoveHoverProfile);
                                }
                            }
                            else
                            {
                                // Если форма не влезает — показываем обычный профиль зоны
                                _hoveredCell.SpriteCompound.SetProfile(ZoneProfile);
                            }
                        }
                    }
                }
            }

            // 4) Статические клетки (MoveActiveProfile, WeaponActiveProfile, и т.д.)
            foreach (var staticCell in _staticCells.Values)
            {
                // staticCell.Type, например, "move-active" или "weapon-active"
                staticCell.Cell.SpriteCompound.SetProfile(staticCell.Type);
            }
        }

        /// <summary>
        /// Устанавливает зону (например, для перемещения).
        /// </summary>
        public void SetZone(CellController center, int radius, string type, string weaponType)
        {
            Debug.Log($"SetZone called {center} radius={radius}, type={type}");
            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = type,
                WeaponType = weaponType
            };
            _zoneCells.Clear();

            // Если это зона перемещения
            if (type == MoveZone && _gameController != null)
            {
                var playerShip = _gameController.GetPlayerShip();
                if (playerShip != null)
                {
                    var shipCells = _gameController.GetShipCells(playerShip);
                    var unionSet = new HashSet<CellController>();
                    // Расширяем зону от каждой клетки корабля
                    foreach (var cellInShip in shipCells)
                    {
                        var neighbors = _fieldController.GetNeighbors(cellInShip, radius);
                        neighbors.Add(cellInShip);
                        unionSet.UnionWith(neighbors);
                    }
                    _zoneCells = unionSet;
                }
            }
            // Если это зона оружия
            else if (type == WeaponZone && _gameController != null)
            {
                var playerShip = _gameController.GetPlayerShip();
                if (playerShip != null)
                {
                    var shipCells = _gameController.GetShipCells(playerShip);
                    var unionSet = new HashSet<CellController>();
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

        /// <summary>
        /// Новый метод: устанавливает зону атаки (WeaponZone) 
        /// для позиций, которые корабль займёт после перемещения.
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

        /// <summary>
        /// Добавляет "статическую" клетку с определённым профилем (типом).
        /// Например: AddStaticCell("id1", cell, CellsStateController.MoveActiveProfile);
        /// </summary>
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

        // ------------------ Вспомогательные классы ------------------

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
