using StarLine2D.Controllers;
using UnityEngine;

namespace StarLine2D.Managers
{
    public class CollisionManager : MonoBehaviour
    {
        [SerializeField] private ShipFactory shipFactory;

        public void CheckShipCollisions()
        {
            var ships = shipFactory.GetSpawnedShips();

            for (int i = 0; i < ships.Count; i++)
            {
                for (int j = i + 1; j < ships.Count; j++)
                {
                    var s1 = ships[i];
                    var s2 = ships[j];
                    if (s1 && s2 && s1.PositionCell && s2.PositionCell)
                    {
                        if (s1.PositionCell == s2.PositionCell)
                        {
                            OnShipCollision(s1, s2);
                        }
                    }
                }
            }
        }

        private void OnShipCollision(ShipController ship1, ShipController ship2)
        {
            if (!ship1 || !ship2) return;

            int ship1Hp = ship1.Health.Value;
            int ship2Hp = ship2.Health.Value;

            // Каждый наносит другому урон, равный своему HP
            ship1.OnDamage(ship2Hp);
            ship2.OnDamage(ship1Hp);
        }

        /// <summary>
        /// Удаляем «мертвые» (уничтоженные) астероиды.
        /// </summary>
        public void CleanupAsteroids()
        {
            // Если вы где-то хранили список, можно там чистить.
            // FindObjectsOfType не возвращает null-объекты, так что обычно ничего не нужно.
        }

        /// <summary>
        /// Удаляем уничтоженные корабли.
        /// </summary>
        public void CleanupShips()
        {
            // Аналогично
        }
    }
}
