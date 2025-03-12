using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Controllers
{
    public class CollisionManager : MonoBehaviour
    {
        private GameController _gc;

        public void Init(GameController gameController)
        {
            _gc = gameController;
        }

        public void CheckShipCollisions()
        {
            if (_gc == null) return;
            var ships = _gc.Ships;

            for (int i = 0; i < ships.Count; i++)
            {
                for (int j = i + 1; j < ships.Count; j++)
                {
                    var s1 = ships[i];
                    var s2 = ships[j];
                    if (s1 && s2 && s1.PositionCell == s2.PositionCell && s1.PositionCell != null)
                    {
                        OnShipCollision(s1, s2);
                    }
                }
            }
        }

        private void OnShipCollision(ShipController ship1, ShipController ship2)
        {
            if (ship1 == null || ship2 == null) return;

            int ship1Hp = ship1.Health.Value;
            int ship2Hp = ship2.Health.Value;

            ship1.OnDamage(ship2Hp);
            ship2.OnDamage(ship1Hp);
        }

        /// <summary>
        /// Удаляем «мертвые» (уничтоженные) астероиды из списка
        /// </summary>
        public void CleanupAsteroids()
        {
            if (_gc == null) return;
            var asteroids = _gc.Asteroids;

            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                if (!asteroids[i]) // == null или missing
                {
                    asteroids.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Аналогично удаляем уничтоженные корабли из списка _gc.Ships
        /// </summary>
        public void CleanupShips()
        {
            if (_gc == null) return;
            var ships = _gc.Ships;

            for (int i = ships.Count - 1; i >= 0; i--)
            {
                if (!ships[i]) // == null или missing
                {
                    ships.RemoveAt(i);
                }
            }
        }
    }
}
