using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Quản lý hiệu ứng hậu kỳ (Post-Processing) tối thượng cho GAM301 theo chuẩn WEAT_CORE_RULES:
/// - Tự động sinh Volume độc tài (Priority 9999, isGlobal = true) ngầm trong Start.
/// - Không phụ thuộc tài nguyên ngoài, tự tạo Profile Bloom + Color Adjustments + Chromatic Aberration + Vignette.
/// - Điều khiển mượt mà thông qua thuộc tính Volume.weight (tránh giật cục, chống lỗi asset).
/// - Hiệu ứng Bloom rực rỡ, nhìn là thấy ngay lập tức sự khác biệt!
/// </summary>
[DisallowMultipleComponent]
public class PostProcessingController : MonoBehaviour
{
    public static PostProcessingController Instance { get; private set; }

    [Header("=== CẤU HÌNH VOLUME HẬU KỲ ĐỘC TÀI ===")]
    [Tooltip("Volume URP điều khiển hiệu ứng")]
    [SerializeField] private Volume apexVolume;

    [Header("=== THÔNG SỐ BLOOM & MÀU SẮC KHI RỰC RỠ ===")]
    [Tooltip("Trọng số Volume bình thường khi ở ngoài vùng Trigger")]
    [Range(0f, 1f)] [SerializeField] private float baseWeight = 0.2f;

    [Tooltip("Trọng số Volume cực đại khi bước vào vùng Trigger_BloomZone")]
    [Range(0f, 1f)] [SerializeField] private float boostedWeight = 1.0f;

    [Tooltip("Tốc độ chuyển đổi trọng số (êm ái, chống giật cục)")]
    [SerializeField] private float transitionSpeed = 3.5f;

    private float _targetWeight;
    private Bloom _bloom;
    private ColorAdjustments _colorAdj;
    private ChromaticAberration _chromatic;
    private Vignette _vignette;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Đảm bảo MainCamera có UniversalAdditionalCameraData để render Post-Processing
        EnsureCameraPostProcessing();

        _targetWeight = baseWeight;
        InitializeApexVolume();
    }

    /// <summary>
    /// Đảm bảo Camera chính luôn bật renderPostProcessing.
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
            addData.volumeTrigger = cam.transform;
        }
    }

    /// <summary>
    /// Tự động tạo hoặc nâng cấp Volume theo chuẩn WEAT_CORE_RULES (Priority 9999, isGlobal = true).
    /// </summary>
    private void InitializeApexVolume()
    {
        if (apexVolume == null) apexVolume = GetComponent<Volume>();
        if (apexVolume == null) apexVolume = gameObject.AddComponent<Volume>();

        apexVolume.isGlobal = true;
        apexVolume.priority = 9999f;
        apexVolume.weight = baseWeight;

        // Tạo Profile in-memory độc quyền để không phụ thuộc file asset
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Apex_Runtime_BloomProfile";

        // 1. BLOOM RỰC RỠ: Threshold thấp (0.15) để bắt sáng toàn diện, Intensity cao, Scatter rộng
        _bloom = profile.Add<Bloom>(true);
        _bloom.intensity.Override(7.0f);
        _bloom.threshold.Override(0.2f); // Bắt mọi vùng sáng vừa phải
        _bloom.scatter.Override(0.85f);  // Tỏa sáng mộng ảo
        _bloom.tint.Override(new Color(1f, 0.95f, 0.85f));

        // 2. COLOR ADJUSTMENTS: Tăng phơi sáng, tương phản và độ rực rỡ
        _colorAdj = profile.Add<ColorAdjustments>(true);
        _colorAdj.postExposure.Override(0.65f); // Sáng bừng cả khung hình
        _colorAdj.contrast.Override(25f);
        _colorAdj.saturation.Override(35f);     // Màu sắc rực rỡ, đậm đà

        // 3. CHROMATIC ABERRATION: Tán sắc quang học như phim điện ảnh
        _chromatic = profile.Add<ChromaticAberration>(true);
        _chromatic.intensity.Override(0.5f);

        // 4. VIGNETTE: Bo góc màn hình tạo chiều sâu
        _vignette = profile.Add<Vignette>(true);
        _vignette.intensity.Override(0.35f);
        _vignette.smoothness.Override(0.6f);

        apexVolume.sharedProfile = profile;
        Debug.Log("<color=cyan>[PostProcessingController] Đã khởi tạo Apex Volume độc tài (Bloom + Exposure + ColorAdjustments + Chromatic)!</color>");
    }

    private void Update()
    {
        if (apexVolume != null)
        {
            // Mượt mà: Lerp trọng số Volume theo thời gian, cấm giật cục
            apexVolume.weight = Mathf.MoveTowards(apexVolume.weight, _targetWeight, Time.deltaTime * transitionSpeed);
        }
    }

    /// <summary>
    /// Kích hoạt chế độ rực sáng Bloom khi bước vào Trigger_BloomZone.
    /// </summary>
    /// <param name="active">True: bật Bloom bừng sáng, False: về bình thường</param>
    public void SetBloomZoneActive(bool active)
    {
        _targetWeight = active ? boostedWeight : baseWeight;
        Debug.Log($"<color=yellow>[PostProcessingController] Bloom Zone: {(active ? "BẬT RỰC RỠ (Weight = 1.0)" : "TẮT (Weight = " + baseWeight + ")")}</color>");

        // EXAM HOOK: Điểm mở rộng khi đổi hiệu ứng ánh sáng
        OnBloomStateChangedHook(active);
    }

    /// <summary>
    /// Điều chỉnh hiệu ứng Vignette khi dính sát thương.
    /// </summary>
    public void SetVignetteIntensity(float intensity)
    {
        if (_vignette != null)
        {
            _vignette.intensity.Override(Mathf.Clamp01(intensity));
        }
    }

    #region EXAM HOOKS
    protected virtual void OnBloomStateChangedHook(bool isBoosted)
    {
        // Hook mở rộng cho đề thi
    }
    #endregion
}
