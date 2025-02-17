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

        [SerializeField] private GameController _gameController;

        // Текущая зона (MoveZone / WeaponZone и т.п.)
        private ZoneModel _currentZone;

        // Все клетки, входящие в текущую «зону»
        private HashSet<CellController> _zoneCells;

        // Статические клетки (MovePoint, ShootPoint и т.д.)
        private Dictionary<string, StaticCellModel> _staticCells;

        // Текущая клетка под курсором
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

            if (_gameController == null)
            {
                _gameController = FindObjectOfType<GameController>();
                if (_gameController == null)
                {
                    Debug.LogError("GameController not found in scene.");
                }
            }

            _staticCells = new Dictionary<string, StaticCellModel>();
            _zoneCells = new HashSet<CellController>();
        }

        /// <summary>
        /// Отрисовывает все клетки: зона, ховер, статические метки и т.п.
        /// </summary>
        public void Render()
        {
            // 1) Сброс состояния
            foreach (var cell in _fieldController.Cells)
            {
                cell.DisplayState.SetState("default");
            }

            // 2) Подсветка клеток зоны (если есть)
            if (_currentZone != null && _zoneCells != null)
            {
                foreach (var cell in _zoneCells)
                {
                    cell.DisplayState.SetState(_currentZone.Type);
                }
            }

            // 3) Логика "hover"
            bool hoverInZone = (_hoveredCell != null 
                                && _currentZone != null
                                && _zoneCells.Contains(_hoveredCell));

            // Для лучевого оружия (Beam) подсветка линии
            if (_currentZone?.WeaponType == "Beam" && hoverInZone)
            {
                var hoverLine = _fieldController.GetLine(_currentZone.Center, _hoveredCell);
                foreach (var cell in hoverLine)
                {
                    cell.DisplayState.SetState("hover-zone");
                }
            }

            // Подсветка самой hovered-клетки
            if (hoverInZone)
            {
                string hoverState = (_currentZone.Type == MoveZone) ? "move-hover" : "weapon-hover";
                _hoveredCell.DisplayState.SetState(hoverState);

                // Доп. правило для MoveZone (двухклеточный корабль) 
                // проверяем, что вся форма умещается в зоне
                if (_currentZone.Type == MoveZone && _gameController != null)
                {
                    var playerShip = _gameController.GetPlayerShip();
                    if (playerShip != null)
                    {
                        var shapeCells = _gameController.GetShapeCells(playerShip.ShipShape, _hoveredCell);

                        // Если форма пустая, уходим
                        if (shapeCells.Count == 0)
                        {
                            _hoveredCell.DisplayState.SetState(MoveZone);
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
                                foreach (var sc in shapeCells)
                                {
                                    sc.DisplayState.SetState("move-hover");
                                }
                            }
                            else
                            {
                                _hoveredCell.DisplayState.SetState(MoveZone);
                            }
                        }
                    }
                }
            }

            // 4) Статические клетки (MovePoint, ShootPoint, и т.д.)
            foreach (var staticCell in _staticCells.Values)
            {
                staticCell.Cell.DisplayState.SetState(staticCell.Type);
            }
        }

        /// <summary>
        /// Обычный способ установить зону для перемещения (MoveZone).
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

            // Если MoveZone: расширяем по всем клеткам корабля (двухклеточного)
            if (type == MoveZone && _gameController != null)
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
            else if (type == WeaponZone && _gameController != null)
            {
                // Если используете этот метод (SetZone) и для атаки,
                // то по умолчанию берется "center" (можно доработать),
                // но обычно Attack мы будем вызывать иначе - см. ниже SetWeaponZoneForFuturePosition.
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
        /// Новый метод: устанавливает зону атаки (WeaponZone) для позиций,
        /// которые корабль занимает в будущем (после перемещения).
        /// shapeCells - это клетки, которые корабль будет занимать.
        /// </summary>
        public void SetWeaponZoneForFuturePosition(List<CellController> shapeCells, int radius, string weaponType)
        {
            // Берем первую клетку как "центральную" формально (это чтоб хранить в объекте _currentZone).
            // Можем взять shapeCells[0] или любую другую из списка. 
            // Визуально "центр" не сильно важен, так как мы все равно строим _zoneCells "union".
            var center = (shapeCells.Count > 0) ? shapeCells[0] : null;

            _currentZone = new ZoneModel
            {
                Center = center,
                Radius = radius,
                Type = WeaponZone,
                WeaponType = weaponType
            };
            _zoneCells.Clear();

            // Собираем объединение радиусов от всех клеток формы
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
