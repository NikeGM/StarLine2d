using StarLine2D.Components;
using TMPro;
using UnityEngine;

namespace StarLine2D.Controllers
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(DisplayStateComponent))]
    [RequireComponent(typeof(OnClickComponent))]
    public class CellController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem shotAnimation;
        [SerializeField] private bool debugEnabled = true;
        
        [SerializeField] private int q = 0;
        [SerializeField] private int r = 0;
        [SerializeField] private int s = 0;

        public int Q => q;
        public int R => r;
        public int S => s;

        private DisplayStateComponent _displayState;
        private OnClickComponent _onClick;

        public DisplayStateComponent DisplayState => _displayState;
        public OnClickComponent OnClick => _onClick;

        private TextMeshPro _text;
        private bool _initialized = false;

        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            if (_initialized) return;
            
            _displayState = GetComponent<DisplayStateComponent>();
            _displayState.SetState("default");
            
            _onClick = GetComponent<OnClickComponent>();
            
            _text = GetComponentInChildren<TextMeshPro>(true);

            _initialized = true;
        }

        public void SetCoords(int inputQ, int inputR, int inputS)
        {
            q = inputQ;
            r = inputR;
            s = inputS;
        }
        
        public void ShotAnimation()
        {
            if (shotAnimation == null) return;
            var instance = Instantiate(shotAnimation, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity) as ParticleSystem;
            
            instance.transform.SetParent(transform);
            instance.transform.localPosition = new Vector3(0, 0.5f, 0);
            
            instance.Play();
            
            Destroy(instance.gameObject, instance.main.duration);
        }
        
        private void Update()
        {
            if (!_text) return;
            _text.text = $"{Q}, {R}, {S}";
            _text.enabled = debugEnabled;
        }
    }
}
