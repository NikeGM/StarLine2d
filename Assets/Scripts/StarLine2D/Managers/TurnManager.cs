using System.Collections;
using System.Linq;
using StarLine2D.Controllers;
using StarLine2D.Factories;
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

        private void Awake()
        {
            if (!movement) Debug.LogError($"[{name}] MovementManager не назначен.");
            if (!attack) Debug.LogError($"[{name}] AttackManager не назначен.");
            if (!collision) Debug.LogError($"[{name}] CollisionManager не назначен.");
            if (!fieldController) Debug.LogError($"[{name}] FieldController не назначен.");
            if (!shipFactory) Debug.LogError($"[{name}] ShipFactory не назначен.");
        }

        public IEnumerator TurnFinished()
        {
            Debug.Log("=== TurnFinished() START ===");
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

            if (movement)
            {
                Debug.Log("MovementManager: MoveAllShipsAndAsteroids...");
                yield return StartCoroutine(movement.MoveAllShipsAndAsteroids());
            }

            if (attack)
            {
                Debug.Log("=== Attack phase ===");
                var allShips = shipFactory.GetSpawnedShips();
                foreach (var s in allShips.Where(s => s))
                {
                    Debug.Log($"AttackManager: Shot(...) for ship: {s.name}");
                    attack.Shot(s);
                }
            }

            if (collision)
            {
                Debug.Log("Collision: CollectPotentialCollisions()...");
                collision.CollectPotentialCollisions();

                Debug.Log("Collision: ProcessCollisions()...");
                collision.ProcessCollisions();

                Debug.Log("Collision: CheckShipCollisions...");
                collision.CheckShipCollisions();

                collision.CleanupAsteroids();
                collision.CleanupShips();
            }

            Debug.Log("Clear static cells in CellStateManager...");
            var cellsStateManager = fieldController.CellStateManager;
            cellsStateManager.ClearStaticCells();

            if (playerShip)
            {
                Debug.Log("Reset player MoveCell...");
                playerShip.MoveCell = null;
            }

            Debug.Log("=== TurnFinished() END ===");
        }
    }
}
