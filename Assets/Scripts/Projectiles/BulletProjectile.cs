using UnityEngine;

/// <summary>
/// Quản lý viên đạn bay (Bullet Projectile) cho cả Player và Enemy trong GAM301.
/// Tự động bay thẳng theo hướng bắn, phát hiện va chạm với Ground, Wall hoặc Target/Enemy.
/// Tương thích hoàn toàn với ObjectPoolManager và phát sinh hiệu ứng nổ (VFX) khi va chạm.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class BulletProjectile : MonoBehaviour, IPoolable
{
    [Header("=== THÔNG SỐ VIÊN ĐẠN ===")]
    [Tooltip("Tốc độ bay của viên đạn xé gió cực lực (m/s)")]
    [SerializeField] private float speed = 45.0f;

    [Tooltip("Lượng sát thương gây ra khi trúng đích")]
    [SerializeField] private float damage = 25.0f;

    [Tooltip("Thời gian tồn tại tối đa trước khi tự thu hồi về Pool (giây)")]
    [SerializeField] private float lifetime = 3.0f;

    [Tooltip("Tag của đối tượng bắn ra viên đạn này (để tránh tự bắn trúng bản thân)")]
    [SerializeField] private string shooterTag = "Player";

    [Header("=== HIỆU ỨNG VA CHẠM (VFX HOOKS) ===")]
    [Tooltip("Pool key của hiệu ứng tia lửa khi chạm Đất hoặc Tường")]
    [SerializeField] private string sparkVFXKey = "VFX_Spark";

    [Tooltip("Pool key của hiệu ứng nổ khi chạm trúng quái/mục tiêu")]
    [SerializeField] private string explosionVFXKey = "VFX_Explosion";

    [Tooltip("Pool key của chính loại đạn này để tự hoàn trả")]
    [SerializeField] private string poolKey = "PlayerBullet";

    private Vector3 moveDirection;
    private float currentLifeTimer;
    private Rigidbody rb;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Khởi tạo tham số cho viên đạn khi bắn ra.
    /// </summary>
    public void Initialize(Vector3 direction, float bulletSpeed, string shooter)
    {
        moveDirection = direction.normalized;
        speed = bulletSpeed > 0f ? bulletSpeed : 45.0f;
        shooterTag = shooter;
        currentLifeTimer = 0f;

        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection;
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    private void Update()
    {
        // Di chuyển đạn về phía trước
        transform.position += moveDirection * (speed * Time.deltaTime);

        // Tự động thu hồi khi hết thời gian sống
        currentLifeTimer += Time.deltaTime;
        if (currentLifeTimer >= lifetime)
        {
            DespawnSelf();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Bỏ qua va chạm với chính người bắn
        if (SafeCompareTag(other, shooterTag)) return;

        // Xử lý va chạm với Ground hoặc Wall
        if (SafeCompareTag(other, "Ground") || SafeCompareTag(other, "Wall") || other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            SpawnVFX(sparkVFXKey, transform.position, Quaternion.identity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
            DespawnSelf();
            return;
        }

        // Xử lý va chạm với Enemy khi đạn là của Player
        if (shooterTag == "Player" && (SafeCompareTag(other, "Enemy") || other.GetComponent<EnemyPatrolFSM>() != null || other.GetComponent<EnemyStationaryBT>() != null || other.GetComponent<EnemyTurretBT>() != null))
        {
            SpawnVFX(explosionVFXKey, transform.position, Quaternion.identity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

            // Gây sát thương nếu quái có script
            EnemyPatrolFSM patrol = other.GetComponent<EnemyPatrolFSM>();
            if (patrol != null) patrol.TakeDamage(damage);

            EnemyStationaryBT stationary = other.GetComponent<EnemyStationaryBT>();
            if (stationary != null) stationary.TakeDamage(damage);

            EnemyTurretBT turret = other.GetComponent<EnemyTurretBT>();
            if (turret != null) turret.TakeDamage(damage);

            DespawnSelf();
            return;
        }

        // Xử lý va chạm với Player khi đạn là của Enemy
        if (shooterTag == "Enemy" && SafeCompareTag(other, "Player"))
        {
            SpawnVFX(explosionVFXKey, transform.position, Quaternion.identity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null) player.TakeDamage(damage);

            DespawnSelf();
            return;
        }
    }

    /// <summary>
    /// Kiểm tra Tag an toàn, bắt ngoại lệ nếu Tag chưa được khai báo trong TagManager.
    /// </summary>
    private bool SafeCompareTag(Collider col, string tagToCheck)
    {
        if (col == null || string.IsNullOrEmpty(tagToCheck)) return false;
        try
        {
            return col.CompareTag(tagToCheck);
        }
        catch
        {
            return col.gameObject.name.Contains(tagToCheck);
        }
    }

    private void SpawnVFX(string vfxKey, Vector3 pos, Quaternion rot)
    {
        if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(vfxKey))
        {
            ObjectPoolManager.Instance.Spawn(vfxKey, pos, rot);
        }
    }

    private void DespawnSelf()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Despawn(poolKey, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #region IPoolable Implementation
    public void OnSpawnFromPool()
    {
        currentLifeTimer = 0f;
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    public void OnReturnToPool()
    {
        currentLifeTimer = 0f;
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
    #endregion
}
