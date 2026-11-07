using UnityEngine;

/// <summary>
/// Điều khiển quả cầu nảy vật lý ngẫu nhiên (PFB_SphereProjectile) trong GAM301.
/// Được phóng ra từ điểm Point_SpherePool với lực đẩy ngẫu nhiên, nảy trên mặt sàn và tường.
/// Tự động thu hồi về Pool sau một khoảng thời gian nhất định.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BouncingSphere : MonoBehaviour, IPoolable
{
    [Header("=== CẤU HÌNH VẬT LÝ NẢY NGẪU NHIÊN ===")]
    [Tooltip("Lực đẩy ban đầu khi được bắn ra")]
    [SerializeField] private float initialImpulseForce = 12.0f;

    [Tooltip("Góc nảy ngẫu nhiên theo trục ngang (độ)")]
    [SerializeField] private float randomAngleSpread = 45.0f;

    [Tooltip("Thời gian tồn tại tối đa trước khi thu hồi (giây)")]
    [SerializeField] private float lifetime = 5.0f;

    [Tooltip("Pool key của đối tượng trong ObjectPoolManager")]
    [SerializeField] private string poolKey = "SphereProjectile";

    [Header("=== SÁT THƯƠNG VA CHẠM ===")]
    [Tooltip("Sát thương gây ra cho nhân vật khi va chạm trúng")]
    [SerializeField] private float impactDamage = 20.0f;

    private Rigidbody rb;
    private float lifeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        lifeTimer = 0f;
        ApplyRandomBounceForce();
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Tạo lực đẩy và hướng bay ngẫu nhiên khi xuất hiện.
    /// </summary>
    public void ApplyRandomBounceForce()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Sinh vector hướng ngẫu nhiên dựa trên randomAngleSpread
        float spreadRad = randomAngleSpread * Mathf.Deg2Rad;
        float randomX = Random.Range(-Mathf.Sin(spreadRad), Mathf.Sin(spreadRad));
        float randomZ = Random.Range(-Mathf.Sin(spreadRad), Mathf.Sin(spreadRad));
        Vector3 randomDir = new Vector3(randomX, Random.Range(0.6f, 1.2f), randomZ).normalized;

        rb.AddForce(randomDir * initialImpulseForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Gây sát thương nếu nảy trúng Player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(impactDamage);
            }
        }
    }

    private void Despawn()
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
        lifeTimer = 0f;
        ApplyRandomBounceForce();
    }

    public void OnReturnToPool()
    {
        lifeTimer = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    #endregion
}
