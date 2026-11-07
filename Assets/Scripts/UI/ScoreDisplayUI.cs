using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý hiển thị điểm số trên Canvas UI trong Đấu trường GAM301 theo chuẩn unity_ui_ux_master_sop.md:
/// - Tự động lắng nghe sự kiện onScoreChanged từ GameMaster.
/// - TextMeshProUGUI chuẩn sắc nét và định dạng số Tabular Figures.
/// - Game Juice: Nảy nhẹ (Punch Scale 1.15x) mỗi khi điểm nhảy số, tạo phản hồi thị giác thỏa mãn!
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class ScoreDisplayUI : MonoBehaviour
{
    [Header("=== CẤU HÌNH HIỂN THỊ ĐIỂM SỐ ===")]
    [Tooltip("Tiền tố hiển thị trước điểm")]
    [SerializeField] private string prefix = "SCORE: ";

    [Tooltip("Số chữ số tối thiểu (hiển thị dạng 0000)")]
    [SerializeField] private int minDigits = 4;

    [Tooltip("Mã màu Hex cho con số điểm (Mặc định: Neon Cyan #38BDF8)")]
    [SerializeField] private string numberColorHex = "#38BDF8";

    [Header("=== GAME JUICE ANIMATION ===")]
    [SerializeField] private float punchScaleAmount = 1.15f;
    [SerializeField] private float punchDuration = 0.15f;

    private TextMeshProUGUI _scoreText;
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Coroutine _punchRoutine;
    private int _lastScore = -1;

    private void Awake()
    {
        _scoreText = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
    }

    private void Start()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.onScoreChanged.AddListener(UpdateScore);
            UpdateScore(GameMaster.Instance.CurrentScore);
        }
        else
        {
            UpdateScore(0);
        }
    }

    private void OnDestroy()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.onScoreChanged.RemoveListener(UpdateScore);
        }
    }

    /// <summary>
    /// Cập nhật hiển thị điểm số trên màn hình kèm hiệu ứng giật nảy.
    /// </summary>
    public void UpdateScore(int newScore)
    {
        if (_scoreText == null) _scoreText = GetComponent<TextMeshProUGUI>();
        if (_scoreText == null) return;

        string format = new string('0', minDigits);
        string scoreFormatted = newScore.ToString(format);
        _scoreText.text = $"{prefix}<color={numberColorHex}>{scoreFormatted}</color>";

        // Kích hoạt hiệu ứng nảy nhẹ khi điểm thay đổi (Juice!)
        if (_lastScore != -1 && newScore > _lastScore && gameObject.activeInHierarchy)
        {
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchScaleRoutine());
        }

        _lastScore = newScore;
    }

    private IEnumerator PunchScaleRoutine()
    {
        if (_rectTransform == null) yield break;

        float elapsed = 0f;
        Vector3 targetScale = _originalScale * punchScaleAmount;

        // Phóng to
        while (elapsed < punchDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            _rectTransform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / (punchDuration * 0.4f));
            yield return null;
        }

        elapsed = 0f;
        // Nảy về bằng EaseOutBack
        while (elapsed < punchDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (punchDuration * 0.6f);
            _rectTransform.localScale = Vector3.Lerp(targetScale, _originalScale, t);
            yield return null;
        }

        _rectTransform.localScale = _originalScale;
        _punchRoutine = null;
    }
}
