using System;
using UnityEngine;

/// <summary>
/// [ĐỀ THI GAM301 - CÂU 4]
/// Script quả cầu trong hệ thống Object Pooling:
/// Được bắn lên không trung, khi rơi xuống mặt đất thì biến mất (trả về Pool).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class ExamPooledSphere : MonoBehaviour
{
    [Header("=== CẤU HÌNH VA CHẠM MẶT ĐẤT ===")]
    [Tooltip("Tag của mặt đất")]
    [SerializeField] private string groundTag = "Ground";

    [Tooltip("Độ cao tối thiểu, rơi dưới mức này tự thu hồi về Pool (chống rớt vực)")]
    [SerializeField] private float minimumYLimit = -0.5f;

    [Tooltip("Thời gian tồn tại tối đa để phòng ngừa (giây)")]
    [SerializeField] private float maxLifetime = 8.0f;

    private Rigidbody _rb;
    private Action<GameObject> _returnToPoolAction;
    private float _spawnTime;
    private bool _isReturned = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        var col = GetComponent<SphereCollider>();
        if (col != null) col.isTrigger = false; // Phải là Collider vật lý để va chạm nảy/chạm đất
    }

    /// <summary>
    /// Khởi tạo khi lấy quả cầu ra từ Object Pool.
    /// </summary>
    public void Initialize(Action<GameObject> returnAction)
    {
        _returnToPoolAction = returnAction;
        _isReturned = false;
        _spawnTime = Time.time;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (_isReturned) return;

        // An toàn 1: Rơi quá thấp dưới mặt đất thì tự biến mất
        if (transform.position.y <= minimumYLimit)
        {
            ReturnToPool();
            return;
        }

        // An toàn 2: Quá thời gian tối đa tự thu hồi
        if (Time.time - _spawnTime > maxLifetime)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Khi quả cầu rơi xuống mặt đất thì biến mất (thu hồi về Pool).
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_isReturned) return;

        // Kiểm tra va chạm với mặt đất (theo Tag, Layer hoặc tên đối tượng)
        bool isGround = collision.collider.CompareTag(groundTag) ||
                        collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                        collision.gameObject.name.ToLower().Contains("ground");

        if (isGround)
        {
            // Quả cầu chạm mặt đất -> biến mất ngay lập tức
            ReturnToPool();
        }
    }

    /// <summary>
    /// Thu hồi đối tượng về Object Pool tái sử dụng, không làm rác bộ nhớ GC.
    /// </summary>
    public void ReturnToPool()
    {
        if (_isReturned) return;
        _isReturned = true;

        if (_returnToPoolAction != null)
        {
            _returnToPoolAction.Invoke(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
