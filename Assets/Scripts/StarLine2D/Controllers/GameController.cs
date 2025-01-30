using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.Utils.Disposable;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private FieldController field;

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject enemyPrefab;

        private List<ShipController> _ships = new();
        private readonly CompositeDisposable _trash = new();
        private CellsStateController _cellsStateController;

        private int _currentWeapon = -1;

        public void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
            _ships = SetPlayersShips();
            _cellsStateController = field.GetComponent<CellsStateController>();
        }


        public void OnPositionClicked()
        {
            var player = GetPlayerShip();
            player.FlushShoots();
            player.MoveCell = null;
            _cellsStateController.ClearStaticCells();
            _cellsStateController.SetZone(player.PositionCell, player.MoveDistance, CellsStateController.MoveZone, "");
        }

        public void OnAttackClicked(int index)
        {
            var player = GetPlayerShip();
            if (!player.MoveCell) return;
            var weapon = player.Weapons[index];

            _cellsStateController.SetZone(player.MoveCell, weapon.Range, CellsStateController.WeaponZone,
                weapon.Type.ToString());
            _currentWeapon = index;
        }

        public IEnumerator TurnFinished()
        {
            _cellsStateController.ClearZone();
            _cellsStateController.ClearStaticCells();
            var playerShip = GetPlayerShip();



            var enemyShip = GetEnemyShip();
            var enemyController = enemyShip.GetComponent<EnemyController>();
            enemyController.Move();
            enemyController.Shot();

            yield return StartCoroutine(MoveShip(playerShip));
            yield return StartCoroutine(MoveShip(enemyShip));

            Shot(playerShip);
            Shot(enemyShip);
        }


        private void OnCellClicked(GameObject go)
        {
            Debug.Log(go);
            if (go == field.gameObject) return;

            var cell = go.GetComponent<CellController>();
            var zone = _cellsStateController.Zone;
            if (zone is null) return;
            if (!field.GetComponent<FieldController>().IsCellInZone(cell, zone.Center, zone.Radius)) return;

            if (_cellsStateController.Zone.Type == CellsStateController.MoveZone)
            {
                _cellsStateController.AddStaticCell("Move", cell, CellsStateController.MovePoint);
                GetPlayerShip().MoveCell = cell;
                _cellsStateController.ClearZone();
                return;
            }

            if (_currentWeapon == -1) return;

            _cellsStateController.AddStaticCell(_currentWeapon.ToString(), cell, CellsStateController.ShootPoint);
            GetPlayerShip().Weapons[_currentWeapon].ShootCell = cell;
            _cellsStateController.ClearZone();
        }

        private List<ShipController> SetPlayersShips()
        {
            var randomCells = field.GetRandomCells(2);
            var playerCell = randomCells[0];
            var enemyCell = randomCells[1];

            var playerPosition = playerCell.gameObject.transform.position;
            var enemyPosition = enemyCell.gameObject.transform.position;

            var player = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
            var enemy = Instantiate(enemyPrefab, enemyPosition, Quaternion.identity);

            var shipsList = new List<ShipController>();

            var playerShipController = player.GetComponent<ShipController>();
            var enemyShipController = enemy.GetComponent<ShipController>();
            var enemyController = enemy.GetComponent<EnemyController>();

            shipsList.Add(playerShipController);
            shipsList.Add(enemyShipController);

            playerShipController.PositionCell = playerCell;
            enemyShipController.PositionCell = enemyCell;

            enemyController.Initialize(enemyCell, field);

            return shipsList;
        }

        private bool HasShip(CellController cell)
        {
            return _ships.Any(item => item.PositionCell == cell);
        }

        private ShipController GetShip(CellController cell)
        {
            return _ships.Find(item => item.PositionCell == cell);
        }

        private ShipController GetPlayerShip()
        {
            return _ships.Find(item => item.IsPlayer);
        }

        private ShipController GetEnemyShip()
        {
            return _ships.Find(item => !item.IsPlayer);
        }

        private IEnumerator MoveShip(ShipController ship)
        {
            if (ship.MoveCell is not null)
                yield return ship.MoveController.GoTo(ship.MoveCell.transform); // Дожидаемся завершения перемещения
            ship.PositionCell = ship.MoveCell;
            ship.MoveCell = null;
        }


        private void OnDestroy()
        {
            _trash.Dispose();
        }

        private void Shot(ShipController ship)
        {
            foreach (var shipWeapon in ship.Weapons)
            {
                var shootCells = new HashSet<CellController>();
                var distance = field.GetDistance(ship.PositionCell, shipWeapon.ShootCell);
                if (distance - 1 > shipWeapon.Range) continue;

                if (shipWeapon.Type == WeaponType.Point)
                {
                    shootCells.Add(shipWeapon.ShootCell);
                }

                if (shipWeapon.Type == WeaponType.Beam)
                {
                    var cells = field.GetLine(ship.PositionCell, shipWeapon.ShootCell);
                    foreach (var cell in cells)
                    {
                        shootCells.Add(cell);
                    }
                }

                shootCells.ToList().ForEach(cell => cell.ShotAnimation());

                var damagedShips = new HashSet<ShipController>();

                foreach (var shootCell in shootCells)
                {
                    if (HasShip(shootCell))
                    {
                        var damagedShip = GetShip(shootCell);
                        if (!damagedShips.Contains(damagedShip))
                        {
                            var resultedDamage = damagedShip.OnDamage(shipWeapon.Damage);
                            ship.AddScore(resultedDamage);
                            damagedShips.Add(damagedShip);
                        }
                    }
                }

                shipWeapon.ShootCell = null;
            }
        }
    }
}