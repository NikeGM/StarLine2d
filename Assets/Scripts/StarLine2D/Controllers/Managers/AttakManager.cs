using UnityEngine;
using StarLine2D.Components;
using StarLine2D.Models; // Важно: чтобы видеть CubeCellModel и ShipShape
using System.Collections.Generic;
using System.Linq;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Менеджер атак и обработки нажатий Attack/Position.
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

        /// <summary>
        /// Кнопка "Position" - включаем зону перемещения
        /// </summary>
        public void OnPositionClicked()
        {
            if (_gameController == null) return;

            var player = _gameController.GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            // Сбрасываем данные о выстрелах и движении
            player.FlushShoots();
            player.MoveCell = null;

            // Очищаем статические клетки и задаём зону перемещения
            _cellsStateController.ClearStaticCells();
            _cellsStateController.SetZone(
                player.PositionCell,
                player.MoveDistance,
                CellsStateController.MoveZone,
                ""
            );
        }

        /// <summary>
        /// Кнопка "Attack" - включаем зону атаки для оружия (index).
        /// </summary>
        public void OnAttackClicked(int index)
        {
            if (_gameController == null) return;
            var player = _gameController.GetPlayerShip();
            if (player == null)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            // Проверяем, выбрано ли место перемещения
            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }

            // Формируем список клеток, которые займет корабль в новой позиции
            var shapeCells = GetShapeCells(shape: player.ShipShape, headCell: player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке (выход за границы?)");
                return;
            }

            // Берём оружие игрока и создаём зону стрельбы
            var weapon = player.Weapons[index];
            _cellsStateController.SetWeaponZoneForFuturePosition(
                shapeCells,
                weapon.Range,
                weapon.Type.ToString()
            );

            _currentWeapon = index;
        }

        /// <summary>
        /// Обработка клика по клетке (когда активна какая-либо зона).
        /// Вызывается при OnMouseDown у клетки или другими способами.
        /// </summary>
        public void OnCellClicked(GameObject go)
        {
            if (_gameController == null || _cellsStateController == null) return;
            if (go == _gameController.Field.gameObject) return;

            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;

            // --- ЗОНА ПЕРЕМЕЩЕНИЯ ---
            if (zone.Type == CellsStateController.MoveZone)
            {
                var player = _gameController.GetPlayerShip();
                if (player == null) return;

                // Получаем клетки, которые займёт корабль при голове в cell
                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: часть корабля окажется за границей поля!");
                    return;
                }

                // 1) Проверка: все ли клетки формы входят в зону
                // (Если зона хранится в _cellsStateController.ZoneCells)
                bool allCellsInZone = shapeCells.All(sc => _cellsStateController.ZoneCells.Contains(sc));
                if (!allCellsInZone)
                {
                    Debug.Log("Слишком далеко! Не все клетки формы корабля попадают в зону перемещения.");
                    return;
                }

                // 2) Проверка: нет ли препятствий
                if (shapeCells.Any(sc => sc.HasObstacle))
                {
                    Debug.Log("Невозможно сходить: часть клеток занята препятствием!");
                    return;
                }

                // 3) Если всё в порядке — добавляем статические клетки и выставляем MoveCell
                for (int i = 0; i < shapeCells.Count; i++)
                {
                    _cellsStateController.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateController.MoveActiveProfile);
                }

                player.MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            // --- ЗОНА АТАКИ ---
            if (_currentWeapon != -1 && zone.Type == CellsStateController.WeaponZone)
            {
                // При клике добавляем "активную" ячейку оружия
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
        /// Логика выстрела (раньше была в GameController).
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

                // Ищем ближайшую из клеток корабля к точке выстрела
                foreach (var c in shipCells)
                {
                    int dist = field.GetDistance(c, shipWeapon.ShootCell);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestCell = c;
                    }
                }

                if (closestCell == null) continue;
                if (minDistance > shipWeapon.Range) continue;

                // Собираем клетки, по которым "пройдёт" выстрел
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
                    foreach (var lineCell in cellsOnLine)
                    {
                        shootCells.Add(lineCell);
                    }
                }

                // Игровая анимация выстрела
                foreach (var cellShot in shootCells)
                {
                    cellShot.ShotAnimation();
                }

                // Применяем урон
                var damagedShips = new HashSet<ShipController>();
                foreach (var cellShot in shootCells)
                {
                    var damagedShip = ships.Find(s => s.PositionCell == cellShot);
                    if (damagedShip != null)
                    {
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var resultedDamage = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
                                // Самострел
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
                        // Возможно, попали в астероид
                        var asteroid = asteroids.FirstOrDefault(a => a.PositionCell == cellShot);
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

                // Сброс ShootCell после выстрела
                shipWeapon.ShootCell = null;
            }
        }

        // --------------------------------------------------------------------------------
        // ВНУТРЕННИЕ методы для расчёта формы корабля и занятых клеток.
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
                    // Одноклеточный - ничего не добавляем
                    break;

                case ShipShape.HorizontalR:
                {
                    // Вправо: (Q-1,R,S+1)
                    var modelLeft = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                    var leftCell = field.FindCellByModel(modelLeft);
                    if (leftCell == null)
                        return new List<CellController>();
                    result.Add(leftCell);
                    break;
                }
                case ShipShape.HorizontalL:
                {
                    // Влево: (Q+1,R,S-1)
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
        // ПУБЛИЧНЫЕ методы для "обёртки" в GameController (если нужно).
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
