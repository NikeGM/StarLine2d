using StarLine2D.Components;
using TMPro;
using UnityEngine;

namespace StarLine2D.Controllers
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteCompoundComponent))]
    [RequireComponent(typeof(OnClickComponent))]
    public class CellController : MonoBehaviour
    {
        [SerializeField] private bool debugEnabled = true;
        [SerializeField] private int q;
        [SerializeField] private int r;
        [SerializeField] private int s;

        private SpriteCompoundComponent _spriteCompound;
        private OnClickComponent _onClick;
        private TextMeshPro _text;
        private bool _initialized;

        public int Q => q;
        public int R => r;
        public int S => s;

        public SpriteCompoundComponent SpriteCompound => _spriteCompound;
        public OnClickComponent OnClick => _onClick;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;

            _spriteCompound = GetComponent<SpriteCompoundComponent>();
            if (!_spriteCompound)
            {
                Debug.LogError($"[{name}] SpriteCompoundComponent not found.");
            }
            else
            {
                _spriteCompound.SetProfile("default");
            }

            _onClick = GetComponent<OnClickComponent>();
            if (!_onClick)
            {
                Debug.LogError($"[{name}] OnClickComponent not found.");
            }

            _text = GetComponentInChildren<TextMeshPro>(true);
            _initialized = true;
        }

        public void SetCoords(int inputQ, int inputR, int inputS)
        {
            q = inputQ;
            r = inputR;
            s = inputS;
        }

        private void Update()
        {
            if (!_text) return;
            _text.text = $"{Q}, {R}, {S}";
            _text.enabled = debugEnabled;
        }
    }
}
