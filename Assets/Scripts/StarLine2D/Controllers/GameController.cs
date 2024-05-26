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
        private SelectionStates _selectionState = SelectionStates.None;
        
        private enum SelectionStates
        {
            None,
            Position,
            Attack
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
            player.PositionCell.OnClick.OnClick();
        }
        
        public void OnAttackClicked()
        {
            var player = GetPlayerShip();
            ChangeSelectionState(SelectionStates.Attack);
            player.PositionCell.OnClick.OnClick();
        }
        
        public IEnumerator TurnFinished()
        {
            var playerShip = GetPlayerShip();
            FlushCells();
            
            yield return StartCoroutine(MoveShip(playerShip));
            
            
            var enemyShip = GetEnemyShip();
            var enemyController = enemyShip.GetComponent<EnemyController>();
            enemyController.Move();
            enemyController.Shot();
            
            yield return StartCoroutine(MoveShip(enemyShip));
            
            Shot(playerShip);
            Shot(enemyShip);
            
            
            ChangeSelectionState(SelectionStates.None);
        }
        
        
        private void OnCellClicked(GameObject go)
        {
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
            
            playerPosition.y = 0.3f;
            enemyPosition.y = 0.3f;
            
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
            yield return ship.MoveController.GoTo(ship.MoveCell.transform); // Дожидаемся завершения перемещения
            ship.PositionCell = ship.MoveCell;
            ship.MoveCell = null;
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
            shootCell.ShotAnimation();
            ship.ShotCell = null;
        }
    }
}