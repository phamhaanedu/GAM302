using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Kích hoạt chuyển đổi góc nhìn Camera sát mặt đất (Ground Cam) khi người chơi bước vào vùng Trigger.
/// Khi rời khỏi vùng Trigger, trả lại góc nhìn camera trước đó hoặc góc nhìn thứ 3 mặc định.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GroundCamTrigger : MonoBehaviour
{
    [Header("=== CẤU HÌNH VÙNG TRIGGER GROUND CAM ===")]
    [Tooltip("Cinemachine Camera phụ đặt sát mặt đất nếu có riêng, hoặc chế độ camera chỉ định")]
    [SerializeField] private CinemachineVirtualCameraBase customGroundVcam;

    [Tooltip("Chế độ Camera muốn chuyển sang khi bước vào Trigger")]
    [SerializeField] private CameraManager.CameraViewMode enterCameraMode = CameraManager.CameraViewMode.FirstPerson;

    [Tooltip("Chế độ Camera muốn hoàn trả khi bước ra khỏi Trigger")]
    [SerializeField] private CameraManager.CameraViewMode exitCameraMode = CameraManager.CameraViewMode.ThirdPerson;

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
            Debug.Log("[GroundCamTrigger] Người chơi bước vào vùng Trigger_GroundCam!");

            // Chuyển chế độ camera
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetCameraMode(enterCameraMode);
            }

            if (customGroundVcam != null)
            {
                customGroundVcam.Priority = 25;
            }

            // EXAM HOOK: Điểm mở rộng khi vào vùng camera sát đất
            OnEnterGroundCamHook(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[GroundCamTrigger] Người chơi rời khỏi vùng Trigger_GroundCam!");

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetCameraMode(exitCameraMode);
            }

            if (customGroundVcam != null)
            {
                customGroundVcam.Priority = 10;
            }

            // EXAM HOOK: Điểm mở rộng khi thoát vùng camera sát đất
            OnExitGroundCamHook(other.gameObject);
        }
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    protected virtual void OnEnterGroundCamHook(GameObject player)
    {
        // VÍ DỤ: Làm chậm tốc độ chạy của nhân vật khi bò sát đất
    }

    protected virtual void OnExitGroundCamHook(GameObject player)
    {
        // VÍ DỤ: Khôi phục tốc độ di chuyển bình thường
    }
    #endregion
}
