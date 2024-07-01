using UnityEngine;

namespace StarLine2D.UI
{
    public class ShowWindowComponent : MonoBehaviour
    {
        [SerializeField] private string path;
        [SerializeField] private bool unique = true;
        public void Show()
        {
            if (unique) AnimatedWindow.OpenUnique(path);
            else AnimatedWindow.Open(path);
        }

        public void Switch()
        {
            AnimatedWindow.Switch(path);
        }
    }
}