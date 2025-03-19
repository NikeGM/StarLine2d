using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Models;
using UnityEngine;
// для CubeCellModel, ShipShape

namespace StarLine2D.Managers
{
    /// <summary>
    /// Менеджер атак и обработки нажатий Attack/Position.
    /// </summary>
    public class AttackManager : MonoBehaviour
    {
        // Ссылки, задаваемые через редактор
        [SerializeField] private FieldController fieldController;
        [SerializeField] private CellsStateManager cellsStateManager;
        [SerializeField] private ShipFactory shipFactory;

        // Индекс текущего оружия
        private int _currentWeapon = -1;

        // --------------------------------------------------------------------
        // Кнопка "Position" — включаем зону перемещения
        // --------------------------------------------------------------------
        public void OnPositionClicked()
        {
            Debug.Log($"OnPositionClicked");
            var playerController = shipFactory.GetPlayerShip();
            if (!playerController)
            {
                Debug.LogError("Не найден корабль игрока! (PlayerController отсутствует)");
                return;
            }

            var player = playerController.GetComponent<ShipController>();
            if (!player)
            {
                Debug.LogError("PlayerController есть, но нет ShipController!");
                return;
            }

            // Сбрасываем данные о выстрелах и движении
            player.FlushShoots();
            player.MoveCell = null;

            // Очищаем статические клетки и задаём зону перемещения
            if (cellsStateManager)
            {
                cellsStateManager.ClearStaticCells();
                cellsStateManager.SetZone(
                    player.PositionCell,
                    player.MoveDistance,
                    CellsStateManager.MoveZone,
                    ""
                );
            }
        }

        // --------------------------------------------------------------------
        // Кнопка "Attack" — включаем зону атаки для оружия (index).
        // --------------------------------------------------------------------
        public void OnAttackClicked(int index)
        {
            var playerController = shipFactory.GetPlayerShip();
            if (!playerController)
            {
                Debug.LogError("Не найден корабль игрока!");
                return;
            }

            var player = playerController.GetComponent<ShipController>();
            if (!player)
            {
                Debug.LogError("PlayerController есть, но нет ShipController!");
                return;
            }

            // Проверяем, выбрано ли место перемещения
            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }

            // Формируем список клеток, которые займет корабль в новой позиции
            var shapeCells = GetShapeCells(player.ShipShape, player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке (выход за границы поля?)");
                return;
            }

            // Берём оружие игрока и создаём зону стрельбы
            var weapon = player.Weapons[index];
            if (cellsStateManager)
            {
                cellsStateManager.SetWeaponZoneForFuturePosition(
                    shapeCells,
                    weapon.Range,
                    weapon.Type.ToString()
                );
            }

            _currentWeapon = index;
        }

        // --------------------------------------------------------------------
        // Обработка клика по клетке (когда активна зона).
        // --------------------------------------------------------------------
        public void OnCellClicked(GameObject go)
        {
            if (!cellsStateManager) return;
            if (!go) return;
            if (!fieldController) return;

            // Проверяем, не кликнули ли мы по самому полю
            if (go == fieldController.gameObject) return;

            var cell = go.GetComponent<CellController>();
            if (cell == null) return;

            var zone = cellsStateManager.Zone;
            if (zone is null) return;

            // --- ЗОНА ПЕРЕМЕЩЕНИЯ ---
            if (zone.Type == CellsStateManager.MoveZone)
            {
                var playerController = shipFactory.GetPlayerShip();
                if (!playerController) return;

                var player = playerController.GetComponent<ShipController>();
                if (!player) return;

                // Получаем клетки, которые займёт корабль при голове в cell
                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: часть корабля окажется за границей поля!");
                    return;
                }

                // Проверяем, все ли клетки формы входят в зону
                bool allCellsInZone = shapeCells.All(sc => cellsStateManager.ZoneCells.Contains(sc));
                if (!allCellsInZone)
                {
                    Debug.Log("Слишком далеко!");
                    return;
                }

                // Проверяем, нет ли препятствий
                if (shapeCells.Any(sc => sc.HasObstacle))
                {
                    Debug.Log("Невозможно сходить: часть клеток занята препятствием!");
                    return;
                }

                // Если всё в порядке — добавляем статические клетки и выставляем MoveCell
                for (int i = 0; i < shapeCells.Count; i++)
                {
                    cellsStateManager.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateManager.MoveActiveProfile);
                }

                player.MoveCell = cell;
                cellsStateManager.ClearZone();
                return;
            }

            // --- ЗОНА АТАКИ ---
            if (_currentWeapon != -1 && zone.Type == CellsStateManager.WeaponZone)
            {
                cellsStateManager.AddStaticCell(
                    _currentWeapon.ToString(),
                    cell,
                    CellsStateManager.WeaponActiveProfile
                );

                var playerController = shipFactory.GetPlayerShip();
                if (playerController)
                {
                    var player = playerController.GetComponent<ShipController>();
                    if (player && _currentWeapon < player.Weapons.Count)
                    {
                        player.Weapons[_currentWeapon].ShootCell = cell;
                    }
                }

                cellsStateManager.ClearZone();
            }
        }

        // --------------------------------------------------------------------
        // Логика выстрела
        // --------------------------------------------------------------------
        public void Shot(ShipController ship)
        {
            if (!ship) return;
            if (!fieldController) return;

            // Ищем все корабли и все астероиды
            var asteroids = FindObjectsOfType<AsteroidController>();
            var ships = FindObjectsOfType<ShipController>();

            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell)
                    continue;

                var shipCells = GetShipCells(ship);

                // Ищем ближайшую клетку корабля к точке выстрела
                CellController closestCell = null;
                int minDistance = int.MaxValue;

                foreach (var c in shipCells)
                {
                    int dist = fieldController.GetDistance(c, shipWeapon.ShootCell);
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
                    var cellsOnLine = fieldController.GetLine(closestCell, shipWeapon.ShootCell);
                    if (cellsOnLine.Count > 0 && cellsOnLine[0] == closestCell)
                    {
                        cellsOnLine.RemoveAt(0);
                    }

                    foreach (var lineCell in cellsOnLine)
                    {
                        shootCells.Add(lineCell);
                    }
                }

                // Анимация выстрела
                foreach (var cellShot in shootCells)
                {
                    cellShot.ShotAnimation();
                }

                // Применяем урон
                var damagedShips = new HashSet<ShipController>();
                foreach (var cellShot in shootCells)
                {
                    // Проверяем, не задели ли мы другой корабль
                    var damagedShip = ships.FirstOrDefault(s => s.PositionCell == cellShot);
                    if (damagedShip != null)
                    {
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var dmg = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
                                // Самострел
                                ship.AddScore(-dmg);
                            }
                            else
                            {
                                ship.AddScore(dmg);
                                damagedShips.Add(damagedShip);
                            }
                        }
                    }
                    else
                    {
                        // Или попали в астероид?
                        var asteroid = asteroids.FirstOrDefault(a => a.PositionCell == cellShot);
                        if (asteroid != null)
                        {
                            var damageDone = asteroid.OnDamage(shipWeapon.Damage);
                            // Например, даём кораблю очки
                            if (damageDone > 0 && asteroid.Hp > 0)
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

        // --------------------------------------------------------------------
        // Вспомогательные методы для формы корабля / клеток
        // --------------------------------------------------------------------
        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (!headCell)
                return new List<CellController>();
            if (!fieldController)
                return new List<CellController>();

            var result = new List<CellController> { headCell };

            switch (shape)
            {
                case ShipShape.Single:
                    // Одноклеточный — ничего добавлять не нужно
                    break;

                case ShipShape.HorizontalR:
                {
                    var modelLeft = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                    var leftCell = fieldController.FindCellByModel(modelLeft);
                    if (!leftCell) return new List<CellController>();
                    result.Add(leftCell);
                    break;
                }
                case ShipShape.HorizontalL:
                {
                    var modelRight = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                    var rightCell = fieldController.FindCellByModel(modelRight);
                    if (!rightCell) return new List<CellController>();
                    result.Add(rightCell);
                    break;
                }
            }

            return result;
        }

        private List<CellController> GetShipCells(ShipController ship)
        {
            if (!ship) return new List<CellController>();
            if (!fieldController) return new List<CellController>();

            var result = new List<CellController>();
            foreach (var model in ship.ShipCellModels)
            {
                var controller = fieldController.FindCellByModel(model);
                if (controller != null)
                {
                    result.Add(controller);
                }
            }

            return result;
        }

        // Публичные «обёртки» (если нужно вызывать снаружи)
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