using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý Popup Game Over xuất hiện khi nhân vật tử trận trong GAM301.
/// Tuân thủ quy tắc thẩm mỹ UI/UX AGENTS.md:
/// - Tỷ lệ 60-30-10, nền tối và Card glassmorphism.
/// - TextMeshProUGUI sắc nét.
/// - Nút bấm Restart và Quit mượt mà.
/// - Mở khóa chuột để người chơi tương tác.
/// </summary>
[DisallowMultipleComponent]
public class GameOverPopupUI : MonoBehaviour
{
    [Header("=== CÁC THÀNH PHẦN UI CỦA POPUP ===")]
    [Tooltip("Panel cha chứa toàn bộ popup (bật/tắt)")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("CanvasGroup để thực hiện hiệu ứng fade in mượt mà")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Text hiển thị điểm số tổng kết cuối trận")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Tooltip("Nút bấm chơi lại màn đấu")]
    [SerializeField] private Button restartButton;

    [Tooltip("Nút bấm thoát trò chơi")]
    [SerializeField] private Button quitButton;

    [Header("=== CẤU HÌNH HIỆU ỨNG (JUICE) ===")]
    [Tooltip("Thời gian xuất hiện (giây)")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        if (canvasGroup == null) canvasGroup = popupRoot.GetComponent<CanvasGroup>();

        // Thiết lập sự kiện cho các nút bấm
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Mặc định ẩn popup ban đầu
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void Start()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.onGameOver.AddListener(ShowGameOverPopup);
        }
    }

    private void OnDestroy()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.onGameOver.RemoveListener(ShowGameOverPopup);
        }

        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    /// <summary>
    /// Kích hoạt hiển thị Popup Game Over và mở khóa chuột.
    /// </summary>
    [ContextMenu("Show Popup (Test)")]
    public void ShowGameOverPopup()
    {
        if (popupRoot == null) return;

        popupRoot.SetActive(true);

        // Mở khóa chuột để người chơi tương tác bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cập nhật điểm cuối cùng
        int finalScore = GameMaster.Instance != null ? GameMaster.Instance.CurrentScore : 0;
        if (finalScoreText != null)
        {
            finalScoreText.text = $"FINAL SCORE: <color=#38BDF8>{finalScore:D4}</color>";
        }

        // Chạy hiệu ứng Fade In
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        Debug.Log("[GameOverPopupUI] Đã hiển thị Popup Game Over!");
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        Transform cardTransform = popupRoot.transform.Find("Card_Container");
        Vector3 initialScale = new Vector3(0.3f, 0.3f, 1f);

        if (cardTransform != null) cardTransform.localScale = initialScale;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // 1. Fade alpha backdrop
            if (canvasGroup != null)
            {
                canvasGroup.alpha = t;
            }

            // 2. EaseOutBack spring bounce cho hộp popup
            if (cardTransform != null)
            {
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                cardTransform.localScale = Vector3.LerpUnclamped(initialScale, Vector3.one, ease);
            }

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (cardTransform != null) cardTransform.localScale = Vector3.one;
        _fadeRoutine = null;
    }

    /// <summary>
    /// Xử lý tải lại màn chơi hiện tại.
    /// </summary>
    public void OnRestartClicked()
    {
        Debug.Log("[GameOverPopupUI] Đang tải lại màn đấu...");
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Xử lý thoát trò chơi.
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("[GameOverPopupUI] Thoát game!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
