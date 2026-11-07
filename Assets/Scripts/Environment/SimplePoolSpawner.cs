using System.Collections;
using UnityEngine;

/// <summary>
/// Script Spawner tự động lấy đối tượng từ ObjectPoolManager để sinh ra trong đấu trường GAM301.
/// Hỗ trợ sinh quả cầu nảy (SphereProjectile) hoặc quái tuần tra (EnemyPatrol).
/// Thiết kế linh hoạt, có cờ autoStart để bật/tắt tức thì khi làm bài thi.
/// </summary>
[DisallowMultipleComponent]
public class SimplePoolSpawner : MonoBehaviour
{
    [Header("=== CẤU HÌNH ĐỐI TƯỢNG TỪ OBJECT POOL ===")]
    [Tooltip("Mã Key của đối tượng trong ObjectPoolManager (Ví dụ: SphereProjectile, EnemyPatrol)")]
    [SerializeField] private string poolKey = "SphereProjectile";

    [Tooltip("Tự động kích hoạt chu kỳ sinh khi game bắt đầu (Start). Tắt đi nếu muốn sinh theo đề thi")]
    [SerializeField] private bool autoStart = false;

    [Tooltip("Khoảng cách thời gian giữa mỗi lần sinh (giây)")]
    [SerializeField] private float spawnInterval = 3.0f;

    [Tooltip("Số lượng tối đa có thể sinh ra (0 = không giới hạn)")]
    [SerializeField] private int maxSpawnCount = 0;

    [Tooltip("Bán kính ngẫu nhiên xung quanh vị trí Spawner")]
    [SerializeField] private float spawnRadius = 1.2f;

    [Header("=== LỰC ĐẨY BAN ĐẦU (CHO BÓNG NẢY / PROJECTILE) ===")]
    [Tooltip("Có tác dụng lực đẩy ngẫu nhiên lên Rigidbody của vật thể vừa sinh không")]
    [SerializeField] private bool applyInitialForce = true;

    [Tooltip("Độ mạnh của lực đẩy")]
    [SerializeField] private float forceMagnitude = 6.0f;

    [Header("=== THEO DÕI TRẠNG THÁI RUNTIME ===")]
    [SerializeField] private bool isSpawning = false;
    [SerializeField] private int currentSpawnedCount = 0;

    private Coroutine _spawnRoutine;

    // Public Getters
    public bool IsSpawning => isSpawning;
    public int CurrentSpawnedCount => currentSpawnedCount;

    private void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    /// <summary>
    /// Bắt đầu chu kỳ sinh đối tượng định kỳ.
    /// </summary>
    [ContextMenu("Start Spawning")]
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = StartCoroutine(SpawnLoopRoutine());
        Debug.Log($"[SimplePoolSpawner] Bắt đầu sinh đối tượng '{poolKey}' tại '{gameObject.name}'");
    }

    /// <summary>
    /// Dừng chu kỳ sinh đối tượng.
    /// </summary>
    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        if (!isSpawning) return;
        isSpawning = false;
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
        Debug.Log($"[SimplePoolSpawner] Đã dừng sinh đối tượng tại '{gameObject.name}'");
    }

    /// <summary>
    /// Sinh ngay lập tức một đối tượng duy nhất từ Pool.
    /// </summary>
    [ContextMenu("Spawn Single Instance")]
    public GameObject SpawnOne()
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning("[SimplePoolSpawner] Không tìm thấy ObjectPoolManager.Instance!");
            return null;
        }

        // Tạo vị trí ngẫu nhiên xung quanh Spawner
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(circle.x, 0.2f, circle.y);
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject spawned = ObjectPoolManager.Instance.Spawn(poolKey, spawnPos, spawnRot);
        if (spawned != null)
        {
            currentSpawnedCount++;

            // Nếu đối tượng có Rigidbody và bật applyInitialForce (thường dùng cho SphereProjectile)
            if (applyInitialForce)
            {
                Rigidbody rb = spawned.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Vector3 forceDir = (Vector3.up * 1.5f + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f))).normalized;
                    rb.AddForce(forceDir * forceMagnitude, ForceMode.Impulse);
                }
            }

            // EXAM HOOK: Hook khi sinh xong 1 đối tượng
            OnSpawnedInstanceHook(spawned);
        }

        return spawned;
    }

    private IEnumerator SpawnLoopRoutine()
    {
        while (isSpawning)
        {
            if (maxSpawnCount > 0 && currentSpawnedCount >= maxSpawnCount)
            {
                Debug.Log($"[SimplePoolSpawner] Đã đạt giới hạn tối đa ({maxSpawnCount}) tại '{gameObject.name}'. Dừng sinh.");
                StopSpawning();
                yield break;
            }

            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    #region EXAM HOOKS
    /// <summary>
    /// Điểm mở rộng khi đối tượng được sinh ra. Sinh viên có thể tùy biến thêm logic (ví dụ set mục tiêu cho quái).
    /// </summary>
    protected virtual void OnSpawnedInstanceHook(GameObject instance)
    {
        // VÍ DỤ: Gán target tuần tra hoặc kích hoạt hiệu ứng xuất hiện
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}
