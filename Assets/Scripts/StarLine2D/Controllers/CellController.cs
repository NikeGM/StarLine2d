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
        [SerializeField] private ParticleSystem shotAnimation;
        [SerializeField] private ParticleSystem explosionAnimation;
        [SerializeField] private bool debugEnabled = true;

        [SerializeField] private int q = 0;
        [SerializeField] private int r = 0;
        [SerializeField] private int s = 0;

        public int Q => q;
        public int R => r;
        public int S => s;

        private SpriteCompoundComponent _spriteCompound;
        private OnClickComponent _onClick;

        public SpriteCompoundComponent SpriteCompound => _spriteCompound;
        public OnClickComponent OnClick => _onClick;

        private TextMeshPro _text;
        private bool _initialized = false;

        // >>> Новое: препятствие <<<
        private ObstacleController _obstacle;
        public bool HasObstacle => _obstacle != null;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;

            _spriteCompound = GetComponent<SpriteCompoundComponent>();
            _spriteCompound.SetProfile("default");

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
            PlayAnimation(shotAnimation);
        }

        public void ExplosionAnimation()
        {
            PlayAnimation(explosionAnimation);
        }

        private void PlayAnimation(ParticleSystem particleAnimation)
        {
            if (particleAnimation is null) return;
            var instance = Instantiate(particleAnimation, transform.position, Quaternion.identity) as ParticleSystem;

            instance.transform.SetParent(transform);
            instance.Play();

            Destroy(instance.gameObject, instance.main.duration);
        }

        private void Update()
        {
            if (!_text) return;
            _text.text = $"{Q}, {R}, {S}";
            _text.enabled = debugEnabled;
        }

        // >>> Новое: назначить препятствие <<<
        public void SetObstacle(ObstacleController obstacle)
        {
            _obstacle = obstacle;
        }
    }
}
