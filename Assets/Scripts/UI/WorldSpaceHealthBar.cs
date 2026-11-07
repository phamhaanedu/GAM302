using UnityEngine;
using UnityEngine.UI;

namespace GAM301.UI
{
    /// <summary>
    /// Thanh máu World-Space 3D hiển thị trên đỉnh đầu của quái vật trong GAM301.
    /// Áp dụng chuẩn Billboard xoay theo Camera và hiệu ứng trễ máu (Ghost Damaged Fill) như game AAA.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        [Header("=== THAM CHIẾU THANH MÁU ===")]
        [Tooltip("Thanh trượt máu chính")]
        [SerializeField] private Slider mainSlider;

        [Tooltip("Thanh trượt máu trễ (hiệu ứng tụt máu vàng cam)")]
        [SerializeField] private Slider ghostSlider;

        [Header("=== THÔNG SỐ VỊ TRÍ & BILLBOARD ===")]
        [Tooltip("Độ lệch độ cao so với gốc quái vật")]
        [SerializeField] private Vector3 heightOffset = new Vector3(0f, 3.2f, 0f);

        [Tooltip("Tốc độ tụt của vạch máu trễ (Ghost Fill)")]
        [SerializeField] private float ghostSpeed = 1.5f;

        [Tooltip("Tự động ẩn thanh máu khi đầy 100% HP")]
        [SerializeField] private bool hideWhenFull = false;

        private float _maxHealth = 100f;
        private float _currentHealth = 100f;
        private Camera _cam;
        private CanvasGroup _canvasGroup;
        private Transform _targetTransform;

        public Slider MainSlider => mainSlider;
        public Slider GhostSlider => ghostSlider;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Tắt Raycast Target toàn bộ để tránh cản đường đạn
            var graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.raycastTarget = false;
            }
        }

        private void Start()
        {
            _cam = Camera.main;
            if (transform.parent != null)
            {
                _targetTransform = transform.parent;
            }
        }

        /// <summary>
        /// Khởi tạo giá trị ban đầu cho thanh máu.
        /// </summary>
        public void Initialize(float maxHealth, Vector3 offset)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            heightOffset = offset;

            if (mainSlider != null)
            {
                mainSlider.minValue = 0f;
                mainSlider.maxValue = _maxHealth;
                mainSlider.value = _currentHealth;
            }

            if (ghostSlider != null)
            {
                ghostSlider.minValue = 0f;
                ghostSlider.maxValue = _maxHealth;
                ghostSlider.value = _currentHealth;
            }

            UpdateVisibility();
        }

        /// <summary>
        /// Cập nhật lượng máu hiện tại khi nhận sát thương.
        /// </summary>
        public void SetHealth(float currentHealth)
        {
            _currentHealth = Mathf.Clamp(currentHealth, 0f, _maxHealth);

            if (mainSlider != null)
            {
                mainSlider.value = _currentHealth;
            }

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (_canvasGroup == null) return;

            if (hideWhenFull && Mathf.Approximately(_currentHealth, _maxHealth))
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                _canvasGroup.alpha = _currentHealth > 0f ? 1f : 0f;
            }
        }

        private void LateUpdate()
        {
            // 1. Cập nhật vị trí bám đỉnh đầu mục tiêu
            if (_targetTransform != null)
            {
                transform.position = _targetTransform.position + heightOffset;
            }

            // 2. Chuẩn Billboard xoay hướng mặt về Camera (Chuẩn AGENTS.md)
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                transform.forward = _cam.transform.forward;
            }

            // 3. Hiệu ứng tụt máu trễ (Ghost Fill Animation)
            if (ghostSlider != null && mainSlider != null)
            {
                if (ghostSlider.value > mainSlider.value)
                {
                    ghostSlider.value = Mathf.MoveTowards(
                        ghostSlider.value,
                        mainSlider.value,
                        Time.deltaTime * ghostSpeed * _maxHealth
                    );
                }
                else if (ghostSlider.value < mainSlider.value)
                {
                    ghostSlider.value = mainSlider.value;
                }
            }
        }
    }
}
