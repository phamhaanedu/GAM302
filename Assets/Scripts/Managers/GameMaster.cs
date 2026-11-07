using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý luồng chính của trò chơi (GameMaster).
/// Đóng vai trò là Singleton điều phối điểm số, trạng thái trận đấu (bắt đầu, kết thúc, tạm dừng).
/// Cung cấp các Hook mở rộng để sinh viên GAM301 hoàn thành các yêu cầu thi trong 60 phút.
/// </summary>
[DisallowMultipleComponent]
public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance { get; private set; }

    public enum GameState
    {
        Ready,      // Sẵn sàng
        Playing,    // Đang chơi
        GameOver,   // Thua cuộc
        Victory     // Chiến thắng
    }

    [Header("=== CẤU HÌNH TRẠNG THÁI TRẬN ĐẤU ===")]
    [Tooltip("Trạng thái hiện tại của trận đấu")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Tooltip("Điểm số hiện tại của người chơi")]
    [SerializeField] private int currentScore = 0;

    [Tooltip("Thời gian tồn tại của trận đấu (tính bằng giây)")]
    [SerializeField] private float gameTimer = 0f;

    [Header("=== CÁC SỰ KIỆN TOÀN CỤC (GLOBAL EVENTS / HOOKS) ===")]
    [Tooltip("Sự kiện kích hoạt khi trận đấu bắt đầu")]
    public UnityEvent onGameStart;

    [Tooltip("Sự kiện kích hoạt khi thay đổi điểm số. Tham số truyền vào: int newScore")]
    public UnityEvent<int> onScoreChanged;

    [Tooltip("Sự kiện kích hoạt khi trò chơi kết thúc (Game Over)")]
    public UnityEvent onGameOver;

    [Tooltip("Sự kiện kích hoạt khi người chơi chiến thắng (Victory)")]
    public UnityEvent onVictory;

    // Public Getters
    public GameState CurrentState => currentState;
    public int CurrentScore => currentScore;
    public float GameTimer => gameTimer;

    private void Awake()
    {
        // Khởi tạo Singleton pattern bảo đảm chỉ có một instance duy nhất
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tự động kiểm tra và nâng cấp EventSystem sang InputSystemUIInputModule nếu cần
        AutoFixEventSystemInputModule();
    }

    /// <summary>
    /// Tự động thay thế StandaloneInputModule bằng InputSystemUIInputModule
    /// để ngăn chặn ngoại lệ InvalidOperationException khi chạy trên Input System package mới.
    /// </summary>
    private void AutoFixEventSystemInputModule()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current ?? FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
#if ENABLE_INPUT_SYSTEM
                Destroy(standalone);
                if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("[GameMaster] Đã tự động nâng cấp EventSystem sang InputSystemUIInputModule thành công!");
                }
#endif
            }
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// Bắt đầu hoặc làm mới trận đấu.
    /// </summary>
    public void StartGame()
    {
        currentState = GameState.Playing;
        currentScore = 0;
        gameTimer = 0f;
        onGameStart?.Invoke();
        onScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Cộng điểm cho người chơi và phát tín hiệu cho UI.
    /// </summary>
    /// <param name="amount">Số điểm cần cộng thêm</param>
    public void AddScore(int amount)
    {
        if (currentState != GameState.Playing) return;

        currentScore += amount;
        onScoreChanged?.Invoke(currentScore);

        // EXAM HOOK: Sinh viên có thể bổ sung điều kiện đạt mốc điểm chiến thắng tại đây
        OnScoreUpdatedHook(currentScore);
    }

    /// <summary>
    /// Kích hoạt trạng thái thua trận (Game Over).
    /// </summary>
    public void TriggerGameOver()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        Debug.Log("[GameMaster] Người chơi đã tử trận - GAME OVER!");
        onGameOver?.Invoke();

        // Đảm bảo Popup Game Over luôn xuất hiện
        var popup = FindAnyObjectByType<GameOverPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.ShowGameOverPopup();
        }

        // EXAM HOOK: Xử lý dừng thời gian, hiện bảng tổng kết điểm hoặc âm thanh thua trận
        OnGameOverHook();
    }

    /// <summary>
    /// Kích hoạt trạng thái chiến thắng.
    /// </summary>
    public void TriggerVictory()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Victory;
        Debug.Log("[GameMaster] Chúc mừng! Đã hoàn thành mục tiêu - VICTORY!");
        onVictory?.Invoke();

        // EXAM HOOK: Xử lý mở cửa ải tiếp theo hoặc lưu điểm cao
        OnVictoryHook();
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    /// <summary>
    /// Hook gọi mỗi khi điểm số cập nhật. Sinh viên override hoặc viết thêm logic kiểm tra.
    /// </summary>
    protected virtual void OnScoreUpdatedHook(int newScore)
    {
        // Kích hoạt âm thanh ghi điểm leng keng
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayScore();
        }
    }

    /// <summary>
    /// Hook gọi khi game over.
    /// </summary>
    protected virtual void OnGameOverHook()
    {
        // Kích hoạt âm thanh tử trận và dừng BGM
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }
    }

    /// <summary>
    /// Hook gọi khi người chơi chiến thắng.
    /// </summary>
    protected virtual void OnVictoryHook()
    {
        // VÍ DỤ: Sinh viên có thể kích hoạt hiệu ứng pháo hoa, vfx chiến thắng
    }
    #endregion
}
