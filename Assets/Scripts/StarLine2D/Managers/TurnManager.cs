using System.Collections;
using StarLine2D.Controllers;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private MovementManager movement;
        [SerializeField] private AttackManager attack;
        [SerializeField] private CollisionManager collision;
        [SerializeField] private FieldController fieldController;
        [SerializeField] private ShipFactory shipFactory;

        public IEnumerator TurnFinished()
{
    Debug.Log("=== TurnFinished() START ===");

    // 1) Ход врагов
    var enemyShips = shipFactory.GetEnemies();
    var playerShip = shipFactory.GetPlayerShip();
    Debug.Log($"Enemies count: {enemyShips.Count}. Player found? {(playerShip ? "Yes" : "No")}");

    foreach (var eship in enemyShips)
    {
        var eCtrl = eship.GetComponent<EnemyController>();
        if (eCtrl)
        {
            Debug.Log($"Enemy [{eship.name}] move and shot...");
            eCtrl.Move();
            eCtrl.Shot(playerShip);
        }
    }

    // 2) Ход союзников
    var allyShips = shipFactory.GetAllies();
    Debug.Log($"Allies count: {allyShips.Count}");
    foreach (var aShip in allyShips)
    {
        var aCtrl = aShip.GetComponent<AllyController>();
        if (aCtrl)
        {
            Debug.Log($"Ally [{aShip.name}] move and shot...");
            aCtrl.Move();
            var enemiesForAllies = shipFactory.GetEnemies();
            aCtrl.Shot(enemiesForAllies);
        }
    }

    // 3) Движение (корутина MovementManager)
    if (movement)
    {
        Debug.Log("MovementManager: MoveAllShipsAndAsteroids...");
        yield return StartCoroutine(movement.MoveAllShipsAndAsteroids());
    }

    // 4) Атака для всех кораблей
    if (attack)
    {
        Debug.Log("=== Attack phase: calling AttackManager.Shot(...) for each ship ===");
        var allShips = shipFactory.GetSpawnedShips();
        foreach (var s in allShips)
        {
            if (!s) continue;
            Debug.Log($"Calling Shot for ship: {s.name}");
            attack.Shot(s);
        }
    }

    // 5) Проверяем коллизии
    if (collision)
    {
        Debug.Log("Collision: CheckShipCollisions...");
        collision.CheckShipCollisions();

        Debug.Log("Collision: CleanupAsteroids & CleanupShips...");
        collision.CleanupAsteroids();
        collision.CleanupShips();
    }

    // 6) Сбрасываем статику подсветки клеток
    Debug.Log("Clear static cells in CellStateManager...");
    var cellsStateManager = fieldController.CellStateManager;
    cellsStateManager.ClearStaticCells();

    if (playerShip != null)
    {
        Debug.Log("Reset player MoveCell...");
        playerShip.MoveCell = null;
    }

    Debug.Log("=== TurnFinished() END ===");
}

    }
}
