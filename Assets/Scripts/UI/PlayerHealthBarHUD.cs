using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Thanh Máu HUD người chơi chuẩn AAA & Game Juice theo unity_ui_ux_master_sop.md:
/// - Thanh máu kép: Main Slider (tụt tức thời) + Ghost Slider (tụt trễ mượt mà).
/// - Dynamic Color: Xanh Emerald (>50%) -> Vàng Amber (25-50%) -> Đỏ Neon (<25%).
/// - TextMeshProUGUI hiển thị chỉ số máu rõ ràng: "HP: 1000 / 1000".
/// - Hiệu ứng rung nhẹ (Punch Shake) khi dính sát thương tạo cảm giác đòn đánh lực.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealthBarHUD : MonoBehaviour
{
    public static PlayerHealthBarHUD Instance { get; private set; }

    [Header("=== CÁC THÀNH PHẦN SLIDERS ===")]
    [Tooltip("Thanh trượt máu chính")]
    [SerializeField] private Slider mainSlider;

    [Tooltip("Thanh trượt ma (Ghost) tụt chậm tạo độ trễ sát thương")]
    [SerializeField] private Slider ghostSlider;

    [Tooltip("Image tô màu của thanh trượt chính")]
    [SerializeField] private Image mainFillImage;

    [Tooltip("Text hiển thị con số máu (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI hpNumberText;

    [Header("=== MÀU SẮC ĐỘNG THEO LƯỢNG MÁU (DYNAMIC PALETTE) ===")]
    [SerializeField] private Color fullHealthColor = new Color(0.06f, 0.73f, 0.51f, 1f);   // Emerald #10B981
    [SerializeField] private Color mediumHealthColor = new Color(0.96f, 0.62f, 0.04f, 1f); // Amber #F59E0B
    [SerializeField] private Color lowHealthColor = new Color(0.94f, 0.27f, 0.27f, 1f);    // Red #EF4444

    [Header("=== CẤU HÌNH ĐỘ TRỄ GHOST SLIDER ===")]
    [SerializeField] private float ghostDelay = 0.35f;
    [SerializeField] private float ghostSpeed = 3.0f;

    private Coroutine _ghostRoutine;
    private Coroutine _shakeRoutine;
    private RectTransform _rectTransform;
    private Vector2 _originalAnchoredPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null) _originalAnchoredPos = _rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Cập nhật giá trị máu, màu sắc và hiệu ứng trôi Ghost Slider.
    /// </summary>
    public void SetHealth(float current, float max)
    {
        if (max <= 0f) max = 100f;
        float percent = Mathf.Clamp01(current / max);

        // 1. Cập nhật Slider chính
        if (mainSlider != null)
        {
            mainSlider.maxValue = max;
            mainSlider.value = current;
        }

        if (ghostSlider != null)
        {
            ghostSlider.maxValue = max;
        }

        // 2. Đổi màu thanh máu theo mức độ nguy hiểm
        if (mainFillImage != null)
        {
            if (percent > 0.5f)
                mainFillImage.color = Color.Lerp(mediumHealthColor, fullHealthColor, (percent - 0.5f) * 2f);
            else
                mainFillImage.color = Color.Lerp(lowHealthColor, mediumHealthColor, percent * 2f);
        }

        // 3. Cập nhật con số hiển thị
        if (hpNumberText != null)
        {
            hpNumberText.text = $"HP: <color=#FFFFFF>{Mathf.CeilToInt(current)}</color> / {Mathf.CeilToInt(max)}";
        }

        // 4. Kích hoạt hiệu ứng trôi chậm của Ghost Slider
        if (ghostSlider != null && gameObject.activeInHierarchy)
        {
            if (_ghostRoutine != null) StopCoroutine(_ghostRoutine);
            _ghostRoutine = StartCoroutine(GhostDrainRoutine(current));
        }

        // 5. Rung nhẹ thanh máu khi dính đòn
        if (gameObject.activeInHierarchy && current < max)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(PunchShakeRoutine());
        }
    }

    private IEnumerator GhostDrainRoutine(float targetValue)
    {
        yield return new WaitForSeconds(ghostDelay);

        while (ghostSlider != null && Mathf.Abs(ghostSlider.value - targetValue) > 0.5f)
        {
            ghostSlider.value = Mathf.MoveTowards(ghostSlider.value, targetValue, Time.deltaTime * ghostSlider.maxValue * ghostSpeed);
            yield return null;
        }

        if (ghostSlider != null) ghostSlider.value = targetValue;
        _ghostRoutine = null;
    }

    private IEnumerator PunchShakeRoutine()
    {
        if (_rectTransform == null) yield break;

        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Mathf.Sin(elapsed * 45f) * 4f * (1f - elapsed / duration);
            _rectTransform.anchoredPosition = _originalAnchoredPos + new Vector2(offsetX, 0f);
            yield return null;
        }

        _rectTransform.anchoredPosition = _originalAnchoredPos;
        _shakeRoutine = null;
    }
}
