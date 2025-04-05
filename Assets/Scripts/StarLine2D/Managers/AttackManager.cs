using System.Collections.Generic;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Factories;
using StarLine2D.Models;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class AttackManager : MonoBehaviour
    {
        [SerializeField] private FieldController fieldController;
        [SerializeField] private CellsStateManager cellsStateManager;
        [SerializeField] private ShipFactory shipFactory;
        [SerializeField] private PositionManager positionManager;

        private int _currentWeapon = -1;

        private void Awake()
        {
            if (!fieldController) Debug.LogError($"[{name}] FieldController не назначен в AttackManager.");
            if (!cellsStateManager) Debug.LogError($"[{name}] CellsStateManager не назначен в AttackManager.");
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory не назначен в AttackManager.");
            if (!positionManager) Debug.LogError($"[{name}] PositionManager не назначен в AttackManager.");
        }

        public void OnPositionClicked()
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
                Debug.LogError("Отсутствует ShipController у PlayerController.");
                return;
            }
            player.FlushShoots();
            player.MoveCell = null;
            if (cellsStateManager)
            {
                cellsStateManager.ClearStaticCells();
                cellsStateManager.SetZone(player.PositionCell, player.MoveDistance, CellsStateManager.MoveZone, "");
            }
        }

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
                Debug.LogError("Отсутствует ShipController у PlayerController.");
                return;
            }
            if (!player.MoveCell)
            {
                Debug.Log("Сначала выберите, куда перемещается корабль!");
                return;
            }
            var shapeCells = GetShapeCells(player.ShipShape, player.MoveCell);
            if (shapeCells.Count == 0)
            {
                Debug.Log("Невозможно построить форму на будущей клетке.");
                return;
            }
            var weapon = player.Weapons[index];
            if (cellsStateManager)
            {
                cellsStateManager.SetWeaponZoneForFuturePosition(shapeCells, weapon.Range, weapon.Type.ToString());
            }
            _currentWeapon = index;
        }

        public void OnCellClicked(GameObject go)
        {
            if (!cellsStateManager) return;
            if (!go) return;
            if (!fieldController) return;
            if (go == fieldController.gameObject) return;
            var cell = go.GetComponent<CellController>();
            if (!cell) return;
            var zone = cellsStateManager.Zone;
            if (zone == null) return;

            if (zone.Type == CellsStateManager.MoveZone)
            {
                var playerController = shipFactory.GetPlayerShip();
                if (!playerController) return;
                var player = playerController.GetComponent<ShipController>();
                if (!player) return;
                var shapeCells = GetShapeCells(player.ShipShape, cell);
                if (shapeCells.Count == 0)
                {
                    Debug.Log("Невозможно сходить: форма не помещается.");
                    return;
                }
                bool allCellsInZone = shapeCells.All(sc => cellsStateManager.ZoneCells.Contains(sc));
                if (!allCellsInZone)
                {
                    Debug.Log("Слишком далеко!");
                    return;
                }
                if (shapeCells.Any(sc => !positionManager.IsCellFree(sc)))
                {
                    Debug.Log("Невозможно сходить: часть клеток занята препятствием или астероидом.");
                    return;
                }
                for (int i = 0; i < shapeCells.Count; i++)
                {
                    cellsStateManager.AddStaticCell($"Move_{i}", shapeCells[i], CellsStateManager.MoveActiveProfile);
                }
                player.MoveCell = cell;
                cellsStateManager.ClearZone();
                return;
            }

            if (_currentWeapon != -1 && zone.Type == CellsStateManager.WeaponZone)
            {
                cellsStateManager.AddStaticCell(_currentWeapon.ToString(), cell, CellsStateManager.WeaponActiveProfile);
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

        public void Shot(ShipController ship)
        {
            if (!ship)
            {
                Debug.LogWarning("AttackManager.Shot вызван с null-ссылкой на корабль.");
                return;
            }
            if (!fieldController)
            {
                Debug.LogWarning("FieldController отсутствует в AttackManager.");
                return;
            }

            var asteroids = FindObjectsOfType<AsteroidController>();
            var ships = FindObjectsOfType<ShipController>();

            foreach (var shipWeapon in ship.Weapons)
            {
                if (!ship.PositionCell || !shipWeapon.ShootCell) continue;

                var shipCells = GetShipCells(ship);
                if (shipCells.Count == 0) continue;

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

                if (closestCell == null || fieldController.GetDistance(closestCell, shipWeapon.ShootCell) > shipWeapon.Range)
                {
                    continue;
                }

                var shootCells = new HashSet<CellController>();
                if (shipWeapon.Type == WeaponType.Point)
                {
                    shootCells.Add(shipWeapon.ShootCell);
                }
                else if (shipWeapon.Type == WeaponType.Beam)
                {
                    var cellsOnLine = fieldController.GetLine(closestCell, shipWeapon.ShootCell);
                    if (cellsOnLine.Count > 0 && cellsOnLine[0] == closestCell) cellsOnLine.RemoveAt(0);
                    foreach (var lineCell in cellsOnLine) shootCells.Add(lineCell);
                }

                foreach (var cellShot in shootCells)
                {
                    ship.PlayShotAnimation(cellShot);
                }

                var damagedShips = new HashSet<ShipController>();
                foreach (var cellShot in shootCells)
                {
                    var damagedShip = ships.FirstOrDefault(s => s.PositionCell == cellShot);
                    if (damagedShip != null)
                    {
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var dmg = damagedShip.OnDamage(shipWeapon.Damage);
                            if (damagedShip == ship)
                            {
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
                        var asteroid = asteroids.FirstOrDefault(a => a.PositionCell == cellShot);
                        if (asteroid != null)
                        {
                            var damageDone = asteroid.OnDamage(shipWeapon.Damage);
                            if (damageDone > 0 && asteroid.Hp > 0)
                            {
                                ship.AddScore(5);
                            }
                        }
                    }
                }

                shipWeapon.ShootCell = null;
            }
        }

        private List<CellController> GetShapeCells(ShipShape shape, CellController headCell)
        {
            if (!headCell) return new List<CellController>();
            if (!fieldController) return new List<CellController>();

            var result = new List<CellController> { headCell };

            if (shape == ShipShape.HorizontalR)
            {
                var modelLeft = new CubeCellModel(headCell.Q - 1, headCell.R, headCell.S + 1);
                var leftCell = fieldController.FindCellByModel(modelLeft);
                if (!leftCell) return new List<CellController>();
                result.Add(leftCell);
            }
            else if (shape == ShipShape.HorizontalL)
            {
                var modelRight = new CubeCellModel(headCell.Q + 1, headCell.R, headCell.S - 1);
                var rightCell = fieldController.FindCellByModel(modelRight);
                if (!rightCell) return new List<CellController>();
                result.Add(rightCell);
            }

            return result;
        }

        private List<CellController> GetShipCells(ShipController ship)
        {
            var result = new List<CellController>();
            if (!ship) return result;
            if (!fieldController) return result;

            foreach (var model in ship.ShipCellModels)
            {
                var controller = fieldController.FindCellByModel(model);
                if (controller != null) result.Add(controller);
            }

            return result;
        }

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
