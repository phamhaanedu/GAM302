using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Quản lý toàn bộ hệ thống Camera góc nhìn trong Đấu trường GAM301.
/// Điều khiển chuyển đổi linh hoạt giữa các góc nhìn:
/// - Góc nhìn thứ ba (3rd Person)
/// - Góc nhìn thứ nhất (1st Person)
/// - Góc nhìn tử trận (Death Cam)
/// </summary>
[DisallowMultipleComponent]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public enum CameraViewMode
    {
        ThirdPerson,    // Góc nhìn thứ 3
        FirstPerson,    // Góc nhìn thứ 1
        DeathCam        // Góc nhìn khi tử trận
    }

    [Header("=== THAM CHIẾU CINEMACHINE CAMERAS (CINEMACHINE 3) ===")]
    [Tooltip("Cinemachine Camera cho góc nhìn người thứ ba (mặc định)")]
    [SerializeField] private CinemachineVirtualCameraBase vcam3rdPerson;

    [Tooltip("Cinemachine Camera cho góc nhìn người thứ nhất")]
    [SerializeField] private CinemachineVirtualCameraBase vcam1stPerson;

    [Tooltip("Cinemachine Camera quay cận cảnh khi người chơi tử trận")]
    [SerializeField] private CinemachineVirtualCameraBase vcamDeath;

    [Header("=== CẤU HÌNH ĐỘ ƯU TIÊN (PRIORITY) ===")]
    [Tooltip("Độ ưu tiên khi camera đang được kích hoạt (Active)")]
    [SerializeField] private int activePriority = 20;

    [Tooltip("Độ ưu tiên khi camera ở trạng thái chờ (Inactive)")]
    [SerializeField] private int inactivePriority = 10;

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    [Tooltip("Góc nhìn camera đang hoạt động")]
    [SerializeField] private CameraViewMode currentMode = CameraViewMode.ThirdPerson;

    // Public Getters
    public CameraViewMode CurrentMode => currentMode;
    public CinemachineVirtualCameraBase VCam3rdPerson => vcam3rdPerson;
    public CinemachineVirtualCameraBase VCam1stPerson => vcam1stPerson;
    public CinemachineVirtualCameraBase VCamDeath => vcamDeath;


    [Header("=== CẤU HÌNH THỜI GIAN CHUYỂN CẢNH (BLEND TIME) ===")]
    [Tooltip("Thời gian chuyển đổi mượt mà giữa các góc nhìn camera (giây)")]
    [SerializeField] private float blendDuration = 0.3f;

    [Header("=== CẤU HÌNH PHÍM CHUYỂN GÓC NHÌN ===")]
    [Tooltip("Phím chuyển đổi góc nhìn thứ nhất / thứ ba (Mặc định: Phím V hoặc C)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.V;

    [Tooltip("Cho phép người chơi tự do bấm phím để đổi góc nhìn")]
    [SerializeField] private bool allowToggleHotkey = true;

    [Tooltip("Hiển thị HUD thông báo góc nhìn camera trên góc màn hình")]
    [SerializeField] private bool showModeHud = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoFindCameras();
    }

    private void Start()
    {
        AutoFindCameras();
        // Mặc định khởi động với góc nhìn thứ ba theo đúng yêu cầu GDD
        SetCameraMode(CameraViewMode.ThirdPerson);
    }

    /// <summary>
    /// Tự động quét tìm và liên kết các Virtual Cameras trong Scene nếu chưa được kéo vào Inspector.
    /// </summary>
    public void AutoFindCameras()
    {
        if (vcam3rdPerson == null)
        {
            var go = GameObject.Find("CM_vcam_3rdPerson");
            if (go != null) vcam3rdPerson = go.GetComponent<CinemachineVirtualCameraBase>();
        }
        if (vcam1stPerson == null)
        {
            var go = GameObject.Find("CM_vcam_1stPerson");
            if (go != null) vcam1stPerson = go.GetComponent<CinemachineVirtualCameraBase>();
        }
        if (vcamDeath == null)
        {
            var go = GameObject.Find("CM_vcam_Death");
            if (go != null) vcamDeath = go.GetComponent<CinemachineVirtualCameraBase>();
        }

        // Tìm trong hierarchy con nếu chưa thấy
        if (vcam3rdPerson == null || vcam1stPerson == null || vcamDeath == null)
        {
            var vcams = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
            foreach (var v in vcams)
            {
                if (vcam3rdPerson == null && v.name.Contains("3rd")) vcam3rdPerson = v;
                else if (vcam1stPerson == null && v.name.Contains("1st")) vcam1stPerson = v;
                else if (vcamDeath == null && v.name.Contains("Death")) vcamDeath = v;
            }
        }

        // Cấu hình CinemachineBrain blend time nhanh (0.3s) thay vì mặc định 2s chậm chạp
        var mainCam = Camera.main != null ? Camera.main.gameObject : GameObject.FindWithTag("MainCamera");
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendDuration);
            }
        }
    }

    private void Update()
    {
        if (!allowToggleHotkey) return;

        if (CheckToggleInput())
        {
            ToggleCameraMode();
        }
    }

    /// <summary>
    /// Kiểm tra phím chuyển đổi góc nhìn (Hỗ trợ cả Input System mới và Input Manager cũ).
    /// Hỗ trợ cả 2 phím thông dụng: V và C.
    /// </summary>
    private bool CheckToggleInput()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.vKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame)
            {
                pressed = true;
            }
        }
#endif

        if (!pressed)
        {
            try
            {
                pressed = Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.C);
            }
            catch
            {
                // Bỏ qua lỗi nếu Unity tắt hẳn Input Manager cũ
            }
        }

        return pressed;
    }

    /// <summary>
    /// Chuyển đổi qua lại giữa góc nhìn thứ 3 và góc nhìn thứ 1.
    /// </summary>
    public void ToggleCameraMode()
    {
        // Nếu đang ở góc nhìn tử trận thì không chuyển
        if (currentMode == CameraViewMode.DeathCam) return;

        if (currentMode == CameraViewMode.ThirdPerson)
        {
            SetCameraMode(CameraViewMode.FirstPerson);
        }
        else
        {
            SetCameraMode(CameraViewMode.ThirdPerson);
        }
    }

    /// <summary>
    /// Chuyển đổi góc nhìn camera theo enum CameraViewMode.
    /// </summary>
    /// <param name="mode">Chế độ góc nhìn mong muốn</param>
    public void SetCameraMode(CameraViewMode mode)
    {
        currentMode = mode;

        if (vcam3rdPerson == null || vcam1stPerson == null || vcamDeath == null)
        {
            AutoFindCameras();
        }

        // Hạ độ ưu tiên của toàn bộ camera xuống mức Inactive
        if (vcam3rdPerson != null)
        {
            vcam3rdPerson.Priority = (mode == CameraViewMode.ThirdPerson) ? activePriority : inactivePriority;
            vcam3rdPerson.enabled = true;
        }
        if (vcam1stPerson != null)
        {
            vcam1stPerson.Priority = (mode == CameraViewMode.FirstPerson) ? activePriority : inactivePriority;
            vcam1stPerson.enabled = true;
        }
        if (vcamDeath != null)
        {
            vcamDeath.Priority = (mode == CameraViewMode.DeathCam) ? activePriority : inactivePriority;
            vcamDeath.enabled = true;
        }

        Debug.Log($"[CameraManager] Đã chuyển đổi góc nhìn sang: {mode} (v3 Pri={vcam3rdPerson?.Priority.Value}, v1 Pri={vcam1stPerson?.Priority.Value})");

        // EXAM HOOK: Kích hoạt callback khi góc nhìn thay đổi
        OnCameraModeChangedHook(mode);
    }

    /// <summary>
    /// Thiết lập mục tiêu theo dõi và ngắm bắn (Follow & LookAt) cho các camera.
    /// </summary>
    public void SetTargets(Transform playerTransform, Transform muzzlePoint, Transform deathTarget)
    {
        if (vcam3rdPerson != null && playerTransform != null)
        {
            vcam3rdPerson.Follow = playerTransform;
            vcam3rdPerson.LookAt = playerTransform;
        }

        if (vcam1stPerson != null && playerTransform != null)
        {
            vcam1stPerson.Follow = playerTransform;
            vcam1stPerson.LookAt = playerTransform;
        }

        if (vcamDeath != null && deathTarget != null)
        {
            vcamDeath.LookAt = deathTarget;
        }
    }

    private void OnGUI()
    {
        if (!showModeHud) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.cyan;
        style.alignment = TextAnchor.MiddleCenter;

        string modeName = currentMode == CameraViewMode.ThirdPerson ? "GÓC NHÌN THỨ 3 (3rd Person)" :
                          currentMode == CameraViewMode.FirstPerson ? "GÓC NHÌN THỨ 1 (1st Person)" : "TỬ TRẬN (Death Cam)";

        // Căn giữa phía dưới đáy màn hình để không đè lên thanh máu ở Top-Left
        float boxWidth = 320f;
        float boxHeight = 36f;
        float posX = (Screen.width - boxWidth) * 0.5f;
        float posY = Screen.height - boxHeight - 12f;

        GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), $"🎥 [{modeName}]\nBấm [V] hoặc [C] để đổi góc nhìn", style);
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    /// <summary>
    /// Hook gọi khi camera đổi chế độ. Sinh viên có thể tùy biến FOV, ẩn/hiện crosshair, v.v.
    /// </summary>
    protected virtual void OnCameraModeChangedHook(CameraViewMode newMode)
    {
        // VÍ DỤ: if (newMode == CameraViewMode.FirstPerson) Crosshair.SetActive(true);
    }
    #endregion
}
