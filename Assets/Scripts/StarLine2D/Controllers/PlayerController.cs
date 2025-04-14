using UnityEngine;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(ShipController))]
    public class PlayerController : MonoBehaviour
    {
        private ShipController ship;

        public void Initialize(ShipController ship)
        {
            this.ship = ship;
        }

        public void Move()
        {
        }

        public void Shot()
        {
        }
    }
}