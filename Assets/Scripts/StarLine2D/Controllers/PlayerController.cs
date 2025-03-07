using UnityEngine;

namespace StarLine2D.Controllers
{
    /// <summary>
    /// Логика управления "кораблём игрока".
    /// Пока что минимальный пример, который можно расширять.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class PlayerController : MonoBehaviour
    {
        private ShipController ship;

        /// <summary>
        /// Инициализация, если нужно передавать ссылки извне.
        /// </summary>
        public void Initialize(ShipController ship)
        {
            this.ship = ship;
        }

        /// <summary>
        /// Пример вызова при ходе игрока: здесь можно прописать действия и логику.
        /// </summary>
        public void Move()
        {
            // При желании управлять вручную (клик на клетке и т.п.)
            // Или оставить пустым, если GameController сам задаёт MoveCell
        }

        public void Shot()
        {
            // При желании — логика выбора цели (пока в GameController она тоже есть)
        }
    }
}