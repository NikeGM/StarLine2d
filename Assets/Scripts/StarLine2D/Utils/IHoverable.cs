using UnityEngine;

namespace StarLine2D.Utils
{
    public interface IHoverable
    {
        public void OnHoverStarted(GameObject target)
        {
        }

        public void OnHoverFinished(GameObject target)
        {
        }

        public void OnHovering(GameObject target)
        {
        }
    }
}