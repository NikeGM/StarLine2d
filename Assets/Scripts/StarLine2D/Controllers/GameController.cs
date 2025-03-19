using UnityEngine;
using StarLine2D.Managers;
using StarLine2D.Utils.Disposable;

namespace StarLine2D.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("Ссылки на поле")]
        [SerializeField] private FieldController field;
        
        [SerializeField] private AttackManager attackManager;
        [SerializeField] private TurnManager turnManager;

        private readonly CompositeDisposable _trash = new();

        private void Awake()
        {
            Utils.Utils.AddScene("Hud");
        }

        private void Start()
        {
            field.Initialize();
            _trash.Retain(field.OnClick.Subscribe(OnCellClicked));
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }
        public void OnPositionClicked()
        {
            attackManager?.OnPositionClicked();
        }

        public void OnAttackClicked(int weaponIndex)
        {
            attackManager?.OnAttackClicked(weaponIndex);
        }
        
        private void OnCellClicked(GameObject go)
        {
            attackManager?.OnCellClicked(go);
        }
        
        public System.Collections.IEnumerator TurnFinished()
        {
            if (turnManager != null)
            {
                yield return StartCoroutine(turnManager.TurnFinished());
            }
        }
    }
}
