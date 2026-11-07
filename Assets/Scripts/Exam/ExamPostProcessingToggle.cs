using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// [ĐỀ THI GAM301 - CÂU 3]
/// Khi bấm phím H để bật/tắt hiệu ứng sau:
/// - Hình ảnh chuyển sang trắng đen (ColorAdjustments.saturation = -100)
/// - Hiệu ứng Film Grain
/// - Hiệu ứng Vignette
/// Chuẩn hóa theo WEAT_CORE_RULES: Volume độc tài priority 9999, isGlobal = true,
/// điều khiển mượt mà qua thuộc tính weight, chống giật cục.
/// </summary>
[DisallowMultipleComponent]
public class ExamPostProcessingToggle : MonoBehaviour
{
    public static ExamPostProcessingToggle Instance { get; private set; }

    [Header("=== CẤU HÌNH HIỆU ỨNG PHÍM H ===")]
    [Tooltip("Phím kích hoạt (Mặc định: Phím H theo đúng đề bài)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.H;

    [Tooltip("Tốc độ chuyển đổi bật/tắt mượt mà")]
    [SerializeField] private float transitionSpeed = 4.0f;

    [Tooltip("Trạng thái hiện tại của hiệu ứng")]
    [SerializeField] private bool isEffectActive = false;

    private Volume _examVolume;
    private float _targetWeight = 0.0f;

    // Các thành phần hiệu ứng URP
    private ColorAdjustments _colorAdjustments;
    private FilmGrain _filmGrain;
    private Vignette _vignette;

    public bool IsEffectActive => isEffectActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureCameraPostProcessing();
        InitializeExamVolume();
    }

    /// <summary>
    /// Đảm bảo Camera chính luôn bật renderPostProcessing để hiển thị hiệu ứng URP.
    /// </summary>
    private void EnsureCameraPostProcessing()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            var addData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (addData == null) addData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            addData.renderPostProcessing = true;
            addData.volumeLayerMask = ~0;
        }
    }

    /// <summary>
    /// Khởi tạo Volume URP độc tài riêng biệt chứa 3 hiệu ứng theo đúng đề thi.
    /// </summary>
    private void InitializeExamVolume()
    {
        _examVolume = GetComponent<Volume>();
        if (_examVolume == null) _examVolume = gameObject.AddComponent<Volume>();

        _examVolume.isGlobal = true;
        _examVolume.priority = 99999f; // Đè bẹp mọi hiệu ứng khác trong Scene khi kích hoạt
        _examVolume.weight = 0.0f;      // Bắt đầu ở trạng thái TẮT (0) theo đúng logic

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Exam_BW_Grain_Vignette_Profile";

        // 1. HÌNH ẢNH TRẮNG ĐEN (Saturation = -100)
        _colorAdjustments = profile.Add<ColorAdjustments>(true);
        _colorAdjustments.saturation.Override(-100f); // Triệt tiêu toàn bộ màu sắc, chuyển thành trắng đen thuần khiết
        _colorAdjustments.contrast.Override(25f);      // Tăng độ tương phản cho sắc nét như ảnh chụp phim cổ điển

        // 2. HIỆU ỨNG FILM GRAIN (Hạt nhiễu máy quay phim)
        _filmGrain = profile.Add<FilmGrain>(true);
        _filmGrain.type.Override(FilmGrainLookup.Large01);
        _filmGrain.intensity.Override(1.0f);          // Rõ hạt Film Grain theo hình minh họa đề thi
        _filmGrain.response.Override(0.85f);

        // 3. HIỆU ỨNG VIGNETTE (Viền tối nghệ thuật bao quanh màn hình)
        _vignette = profile.Add<Vignette>(true);
        _vignette.intensity.Override(0.55f);          // Viền tối rõ rệt
        _vignette.smoothness.Override(0.5f);
        _vignette.rounded.Override(true);

        _examVolume.sharedProfile = profile;
        Debug.Log("<color=cyan>[ExamPostProcessingToggle] Đã tạo Volume độc tài cho Câu 3 (Trắng đen + Film Grain + Vignette)!</color>");
    }

    private void Update()
    {
        // Kiểm tra bấm phím H (hỗ trợ cả New Input System lẫn Legacy Input)
        bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            keyPressed = true;
        }
#endif
        if (!keyPressed && Input.GetKeyDown(toggleKey))
        {
            keyPressed = true;
        }

        if (keyPressed)
        {
            ToggleEffect();
        }

        // Chuyển đổi trọng số Volume mượt mà, chống giật cục
        if (_examVolume != null)
        {
            _examVolume.weight = Mathf.MoveTowards(_examVolume.weight, _targetWeight, Time.deltaTime * transitionSpeed);
        }
    }

    /// <summary>
    /// Bật / Tắt hiệu ứng khi bấm phím H.
    /// </summary>
    [ContextMenu("Toggle Effect (H Key)")]
    public void ToggleEffect()
    {
        isEffectActive = !isEffectActive;
        _targetWeight = isEffectActive ? 1.0f : 0.0f;

        string stateText = isEffectActive ? "<color=green>BẬT (ON)</color>" : "<color=red>TẮT (OFF)</color>";
        Debug.Log($"<b>[CÂU 3 - PHÍM H] Hiệu ứng Trắng Đen + Film Grain + Vignette: {stateText}</b>");
    }

    /// <summary>
    /// Gán trực tiếp trạng thái hiệu ứng.
    /// </summary>
    public void SetEffectActive(bool active)
    {
        isEffectActive = active;
        _targetWeight = isEffectActive ? 1.0f : 0.0f;
    }
}
