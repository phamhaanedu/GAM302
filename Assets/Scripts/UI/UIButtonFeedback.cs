using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Thêm Game Juice và Micro-interactions cho Nút bấm theo chuẩn unity_ui_ux_master_sop.md:
/// - Hover: Phóng to nhẹ 1.05x trong 0.12s
/// - Press: Co lún 0.92x trong 0.08s (cảm giác đàn hồi vật lý)
/// - Release: Nảy bật ra (EaseOutBack)
/// - SFX: Phát âm thanh UI Click giòn tan với pitch ngẫu nhiên nhẹ chống mỏi tai
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== CẤU HÌNH TỈ LỆ CO GIÃN (JUICE SCALE) ===")]
    [Tooltip("Tỉ lệ phóng to khi rê chuột vào nút")]
    [SerializeField] private float hoverScale = 1.05f;

    [Tooltip("Tỉ lệ co lún khi nhấn giữ nút")]
    [SerializeField] private float pressScale = 0.92f;

    [Tooltip("Thời gian chuyển đổi scale (giây)")]
    [SerializeField] private float duration = 0.1f;

    private Vector3 _originalScale;
    private Coroutine _scaleRoutine;
    private Button _button;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        AnimateScale(_originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        AnimateScale(_originalScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        AnimateScale(_originalScale * pressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable) return;
        AnimateScale(_originalScale);

        // Kích hoạt âm thanh UI click nếu có AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHit(); // Sử dụng âm thanh giòn ngắn
        }
    }

    private void AnimateScale(Vector3 target)
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleRoutine(target));
    }

    private IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // EaseOutQuad
            float ease = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.LerpUnclamped(start, target, ease);
            yield return null;
        }

        transform.localScale = target;
        _scaleRoutine = null;
    }

    private void OnDisable()
    {
        transform.localScale = _originalScale;
    }
}
