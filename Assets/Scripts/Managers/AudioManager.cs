using UnityEngine;

/// <summary>
/// Quản lý toàn bộ hệ thống Âm thanh (Audio / SFX / BGM) trong Đấu trường GAM301.
/// Cung cấp Singleton AudioManager điều khiển nhạc nền vòng lặp và các hiệu ứng âm thanh chiến đấu.
/// Hỗ trợ đầy đủ các hook bắn súng, trúng đạn, nổ tung, ghi điểm và tử trận.
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== CÁC NGUỒN PHÁT ÂM THANH (AUDIO SOURCES) ===")]
    [Tooltip("AudioSource dành cho hiệu ứng âm thanh (SFX)")]
    [SerializeField] private AudioSource sfxSource;

    [Header("=== DANH SÁCH AUDIO CLIPS CHIẾN ĐẤU (SFX ONLY) ===")]
    [Tooltip("Âm thanh tiếng súng nổ khi bắn")]
    [SerializeField] private AudioClip shootClip;

    [Tooltip("Âm thanh hiệu ứng nổ tung")]
    [SerializeField] private AudioClip explosionClip;

    [Tooltip("Âm thanh va chạm, trúng đạn kim loại")]
    [SerializeField] private AudioClip hitClip;

    [Tooltip("Âm thanh ghi điểm leng keng")]
    [SerializeField] private AudioClip scoreClip;

    [Tooltip("Âm thanh khi người chơi tử trận (Game Over)")]
    [SerializeField] private AudioClip gameOverClip;

    [Header("=== CẤU HÌNH ÂM LƯỢNG (VOLUME) ===")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Sound toàn màn hình
        }

        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        // Đã gỡ bỏ toàn bộ nhạc nền BGM
    }

    /// <summary>
    /// Bỏ BGM theo yêu cầu người dùng.
    /// </summary>
    public void PlayBGM(AudioClip clip = null)
    {
        // Đã tắt BGM hoàn toàn
    }

    /// <summary>
    /// Dừng phát nhạc nền.
    /// </summary>
    public void StopBGM()
    {
        // Đã gỡ bỏ BGM
    }

    /// <summary>
    /// Phát một âm thanh SFX duy nhất.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchRandomness = 0.05f)
    {
        if (clip == null || sfxSource == null) return;

        // Thêm độ biến thiên pitch nhẹ để âm thanh sinh động, tránh lặp lại khô khan
        if (pitchRandomness > 0f)
        {
            sfxSource.pitch = 1.0f + Random.Range(-pitchRandomness, pitchRandomness);
        }
        else
        {
            sfxSource.pitch = 1.0f;
        }

        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
    }

    // === CÁC SHORTCUTS TIỆN LỢI CHO GAMEPLAY ===

    public void PlayShoot()
    {
        PlaySFX(shootClip, 0.9f, 0.08f);
    }

    public void PlayExplosion()
    {
        PlaySFX(explosionClip, 1.0f, 0.05f);
    }

    public void PlayHit()
    {
        PlaySFX(hitClip, 0.85f, 0.1f);
    }

    public void PlayScore()
    {
        PlaySFX(scoreClip, 1.0f, 0.0f);
    }

    public void PlayGameOver()
    {
        StopBGM();
        PlaySFX(gameOverClip, 1.0f, 0.0f);
    }
}
