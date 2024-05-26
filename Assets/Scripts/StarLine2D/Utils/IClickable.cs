using UnityEngine;

namespace StarLine2D.Utils
{
    public interface IClickable
    {
        public void OnPerformed(GameObject target)
        {
        }

        public void OnStarted(GameObject target)
        {
        }

        public void OnCancelled(GameObject target)
        {
        }
    }
}