using System;
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
        [SerializeField] private bool debugAlwaysHit = false;

        private List<ShipController> _ships = new();
        private readonly CompositeDisposable _trash = new();
        private SelectionStates _selectionState = SelectionStates.None;

        private enum SelectionStates
        {
            None,
            Position,
            Attack
        }

        private void Awake()
        {
            Utils.Utils.AddScene("Hud");
        }

        public void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
            _ships = SetPlayersShips();
        }


        public void OnPositionClicked()
        {
            var player = GetPlayerShip();
            ChangeSelectionState(SelectionStates.Position);
            OnCellClicked(player.PositionCell.gameObject);
        }

        public void OnAttackClicked()
        {
            var player = GetPlayerShip();
            ChangeSelectionState(SelectionStates.Attack);
            OnCellClicked(player.PositionCell.gameObject);
        }

        public IEnumerator TurnFinished()
        {
            var playerShip = GetPlayerShip();
            var enemyShip = GetEnemyShip();
            var enemyController = enemyShip.GetComponent<EnemyController>();

            FlushCells();
    
            enemyController?.Move();
            enemyController?.Shot(playerShip);
    
            var isCollision = enemyShip.MoveCell == playerShip.MoveCell;

            var playerMoveCoroutine = StartCoroutine(MoveShip(playerShip));
            var enemyMoveCoroutine = StartCoroutine(MoveShip(enemyShip));

            yield return playerMoveCoroutine;
            yield return enemyMoveCoroutine;

            if (debugAlwaysHit)
            {
                playerShip.ShotCell = enemyShip.PositionCell;
            }

            Shot(playerShip);
            Shot(enemyShip);

            if (isCollision)
            {
                OnShipCollision(playerShip, enemyShip);
            }

            ChangeSelectionState(SelectionStates.None);
        }

        private void OnCellClicked(GameObject go)
        {
            if (go == field.gameObject) return;

            var cell = go.GetComponent<CellController>();

            if (cell.DisplayState.Is("default"))
            {
                HandleDefaultState(cell);
            }
            else if (cell.DisplayState.Is("active"))
            {
                HandleActiveState(cell);
            }
            else if (cell.DisplayState.Is("highlighted"))
            {
                HandleHighlightedState(cell);
            }
        }


        private void HandleDefaultState(CellController cell)
        {
            if (!HasShip(cell)) return;

            var ship = GetShip(cell);
            if (!ship.IsPlayer) return;

            FlushCells();
            var radius = _selectionState == SelectionStates.Attack ? ship.ShootDistance : ship.MoveDistance;
            var neighbors = field.GetNeighbors(cell, radius);
            foreach (var neighbor in neighbors) neighbor.DisplayState.SetState("highlighted");

            cell.DisplayState.SetState("active");
        }

        private void HandleActiveState(CellController cell)
        {
            if (HasShip(cell))
            {
                if (_selectionState != SelectionStates.None)
                {
                    HandleDefaultState(cell);
                }
            }
            else cell.DisplayState.SetState("highlighted");
        }

        private void FlushCells()
        {
            field.Cells.ForEach(item => item.DisplayState.SetState("default"));
        }

        private void HandleHighlightedState(CellController cell)
        {
            foreach (var item in field.Cells)
            {
                if (item.DisplayState.Is("active") && !HasShip(item))
                {
                    item.DisplayState.SetState("highlighted");
                }
            }

            cell.DisplayState.SetState("active");

            var playerShip = GetPlayerShip();

            if (_selectionState == SelectionStates.Position)
                playerShip.MoveCell = cell;
            else
                playerShip.ShotCell = cell;
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
    
            var directionToEnemy = (enemyPosition - playerPosition).normalized;
            var directionToPlayer = (playerPosition - enemyPosition).normalized;
    
            player.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(directionToEnemy.y, directionToEnemy.x) * Mathf.Rad2Deg);
            enemy.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg);
    
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
            {
                yield return ship.MoveController.GoTo(ship.MoveCell.transform); // Дожидаемся завершения перемещения
                ship.PositionCell = ship.MoveCell;
                ship.MoveCell = null;
            }
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }

        private void ChangeSelectionState(SelectionStates newState)
        {
            _selectionState = newState switch
            {
                SelectionStates.Attack when _selectionState == SelectionStates.Attack => SelectionStates.Position,
                SelectionStates.Position when _selectionState == SelectionStates.Position => SelectionStates.Attack,
                _ => _selectionState
            };

            _selectionState = newState;
        }

        private void Shot(ShipController ship)
        {
            var shootCell = ship.ShotCell;
            shootCell?.ShotAnimation();
            if (HasShip(shootCell))
            {
                var damagedShip = GetShip(shootCell);
                var resultedDamage = damagedShip.OnDamage(ship.Damage);
                if (damagedShip == ship)
                {
                    ship.AddScore(-resultedDamage);
                }
                else
                {
                    ship.AddScore(resultedDamage);
                }
            }

            ship.ShotCell = null;
        }

        private void OnShipCollision(ShipController ship1, ShipController ship2)
        {
            int ship1Hp = ship1.Health.Value;
            int ship2Hp = ship2.Health.Value;

            ship1.OnDamage(ship2Hp);
            ship2.OnDamage(ship1Hp);
        }
    }
}