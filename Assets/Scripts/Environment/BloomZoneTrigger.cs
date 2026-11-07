using UnityEngine;

/// <summary>
/// Kích hoạt hiệu ứng rực sáng Bloom khi người chơi bước vào vùng Trigger_BloomZone.
/// Tương tác trực tiếp với PostProcessingController để tăng/giảm cường độ Bloom mượt mà.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class BloomZoneTrigger : MonoBehaviour
{
    [Header("=== CẤU HÌNH VÙNG TRIGGER BLOOM ===")]
    [Tooltip("Hiển thị vùng Wireframe trong Scene View để dễ canh chỉnh")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Màu sắc Gizmo trong Scene View")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.92f, 0.016f, 0.4f);

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BloomZoneTrigger] Người chơi bước vào vùng Trigger_BloomZone -> Kích hoạt Bloom rực rỡ!");

            if (PostProcessingController.Instance != null)
            {
                PostProcessingController.Instance.SetBloomZoneActive(true);
            }

            // EXAM HOOK: Điểm mở rộng khi vào vùng ánh sáng rực
            OnPlayerEnterBloomZoneHook(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BloomZoneTrigger] Người chơi rời khỏi vùng Trigger_BloomZone -> Khôi phục Bloom mặc định!");

            if (PostProcessingController.Instance != null)
            {
                PostProcessingController.Instance.SetBloomZoneActive(false);
            }

            // EXAM HOOK: Điểm mở rộng khi rời vùng ánh sáng rực
            OnPlayerExitBloomZoneHook(other.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    protected virtual void OnPlayerEnterBloomZoneHook(GameObject player)
    {
        // VÍ DỤ: Tăng tốc độ hồi máu khi đứng trong vùng thánh địa ánh sáng
    }

    protected virtual void OnPlayerExitBloomZoneHook(GameObject player)
    {
        // VÍ DỤ: Dừng hồi máu khi bước ra ngoài
    }
    #endregion
}
