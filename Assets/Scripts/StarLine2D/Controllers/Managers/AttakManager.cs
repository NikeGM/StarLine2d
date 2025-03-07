using UnityEngine;
using StarLine2D.Components;
using StarLine2D.Models; // Важно: чтобы видеть CubeCellModel и ShipShape
using System.Collections.Generic;
using System.Linq;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Менеджер атак и обработки нажатий Attack/Position.
    /// Добавлены публичные методы GetShapeCells_Public, GetShipCells_Public,
    /// чтобы их мог вызвать GameController (в "обёртках").
    /// </summary>
    public class AttackManager : MonoBehaviour
    {
        private GameController _gameController;
        private CellsStateController _cellsStateController;
        private int _currentWeapon = -1;

        public void Init(GameController controller)
        {
            _gameController = controller;
            if (_gameController != null && _gameController.Field != null)
            {
                _cellsStateController = _gameController.Field.GetComponent<CellsStateController>();
            }
        }

        public void OnPositionClicked()
        {
            if (_gameController == null) return;

            var player = _gameController.GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            player.FlushShoots();
            player.MoveCell = null;

            _cellsStateController.ClearStaticCells();
            _cellsStateController.SetZone(
                player.PositionCell,
                player.MoveDistance,
                CellsStateController.MoveZone,
                ""
            );
        }

        public void OnAttackClicked(int index)
        {
            if (_gameController == null) return;
            var player = _gameController.GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }

            var shapeCells = GetShapeCells(shape: player.ShipShape, headCell: player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке (выход за границы?)");
                return;
            }

            var weapon = player.Weapons[index];
            _cellsStateController.SetWeaponZoneForFuturePosition(
                shapeCells,
                weapon.Range,
                weapon.Type.ToString()
            );

            _currentWeapon = index;
        }

        public void OnCellClicked(GameObject go)
        {
            if (_gameController == null || _cellsStateController == null) return;
            if (go == _gameController.Field.gameObject) return;

            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;

            // Зона перемещения
            if (zone.Type == CellsStateController.MoveZone)
            {
                var player = _gameController.GetPlayerShip();
                if (player == null) return;

                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: часть корабля окажется за границей поля!");
                    return;
                }

                for (int i = 0; i < shapeCells.Count; i++)
                {
                    _cellsStateController.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateController.MoveActiveProfile);
                }

                player.MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            // Зона атаки
            if (_currentWeapon != -1 && zone.Type == CellsStateController.WeaponZone)
            {
                _cellsStateController.AddStaticCell(
                    _currentWeapon.ToString(),
                    cell,
                    CellsStateController.WeaponActiveProfile
                );
                var player = _gameController.GetPlayerShip();
                if (player)
                {
                    player.Weapons[_currentWeapon].ShootCell = cell;
                }
                _cellsStateController.ClearZone();
            }
        }

        /// <summary>
        /// Выстрел кораблём (старая логика из GameController).
        /// </summary>
        public void Shot(ShipController ship)
        {
            if (ship == null) return;
            if (_gameController == null) return;

            var field = _gameController.Field;
            var asteroids = _gameController.Asteroids;
            var ships = _gameController.Ships;

            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell)
                    continue;

                var shipCells = GetShipCells(ship);

                CellController closestCell = null;
                int minDistance = int.MaxValue;

                foreach (var cell in shipCells)
                {
                    int dist = field.GetDistance(cell, shipWeapon.ShootCell);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestCell = cell;
                    }
                }

                if (closestCell == null) continue;
                if (minDistance > shipWeapon.Range) continue;

                // Набор клеток, по которым летит выстрел
                var shootCells = new HashSet<CellController>();
                if (shipWeapon.Type == WeaponType.Point)
                {
                    shootCells.Add(shipWeapon.ShootCell);
                }
                else if (shipWeapon.Type == WeaponType.Beam)
                {
                    var cellsOnLine = field.GetLine(closestCell, shipWeapon.ShootCell);
                    if (cellsOnLine.Count > 0 && cellsOnLine[0] == closestCell)
                    {
                        cellsOnLine.RemoveAt(0);
                    }
                    foreach (var c in cellsOnLine)
                        shootCells.Add(c);
                }

                // Анимация выстрела
                foreach (var c in shootCells)
                {
                    c.ShotAnimation();
                }

                // Урон
                var damagedShips = new HashSet<ShipController>();
                foreach (var cell in shootCells)
                {
                    var damagedShip = ships.Find(s => s.PositionCell == cell);
                    if (damagedShip != null)
                    {
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var resultedDamage = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
                                ship.AddScore(-resultedDamage);
                            }
                            else
                            {
                                ship.AddScore(resultedDamage);
                                damagedShips.Add(damagedShip);
                            }
                        }
                    }
                    else
                    {
                        var asteroid = asteroids.FirstOrDefault(a => a.PositionCell == cell);
                        if (asteroid != null)
                        {
                            var damageDone = asteroid.OnDamage(shipWeapon.Damage);
                            if (damageDone > 0 && asteroid.HP > 0)
                            {
                                ship.AddScore(5);
                            }
                        }
                    }
                }

                // Сброс ShootCell
                shipWeapon.ShootCell = null;
            }
        }

        // --------------------------------------------------------------------------------
        // ВНУТРЕННИЕ методы для расчёта формы корабля, занятых клеток и т.д. (как раньше).
        // --------------------------------------------------------------------------------
        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (headCell == null) 
                return new List<CellController>();
            if (_gameController == null) 
                return new List<CellController>();

            var field = _gameController.Field;
            var result = new List<CellController> { headCell };

            switch (shape)
            {
                case ShipShape.Single:
                    // Одноклеточный
                    break;

                case ShipShape.HorizontalR:
                {
                    var modelLeft = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                    var leftCell = field.FindCellByModel(modelLeft);
                    if (leftCell == null)
                        return new List<CellController>();
                    result.Add(leftCell);
                    break;
                }
                case ShipShape.HorizontalL:
                {
                    var modelRight = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                    var rightCell = field.FindCellByModel(modelRight);
                    if (rightCell == null)
                        return new List<CellController>();
                    result.Add(rightCell);
                    break;
                }
            }
            return result;
        }

        private List<CellController> GetShipCells(ShipController ship)
        {
            if (ship == null || _gameController == null) 
                return new List<CellController>();

            var result = new List<CellController>();
            var field = _gameController.Field;
            var modelCells = ship.ShipCellModels;

            foreach (var model in modelCells)
            {
                var controller = field.FindCellByModel(model);
                if (controller != null)
                {
                    result.Add(controller);
                }
            }
            return result;
        }

        // --------------------------------------------------------------------------------
        // ПУБЛИЧНЫЕ методы для "обёртки" в GameController.
        // CellStateController вызывает gameController.GetShapeCells(...), 
        // а там внутри - эти методы, чтобы остальной код не ломался.
        // --------------------------------------------------------------------------------
        public List<CellController> GetShapeCells_Public(ShipShape shape, CellController headCell)
        {
            return GetShapeCells(shape, headCell);
        }

        public List<CellController> GetShipCells_Public(ShipController ship)
        {
            return GetShipCells(ship);
        }
    }
}
