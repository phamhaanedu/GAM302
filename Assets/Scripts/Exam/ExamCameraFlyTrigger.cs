using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// [ĐỀ THI GAM301 - CÂU 2]
/// Khi nhân vật di chuyển tới vị trí X (vị trí bất kỳ do sinh viên tự chọn) thì
/// Camera sẽ bay tới vị trí đối tượng Cube (vị trí cube do sinh viên tự đặt. Lưu ý đặt xa nhân vật).
/// Sau 2 giây thì Camera bay về lại vị trí nhân vật.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ExamCameraFlyTrigger : MonoBehaviour
{
    [Header("=== CẤU HÌNH ĐỐI TƯỢNG CUBE Ở XA ===")]
    [Tooltip("Đối tượng Cube ở xa nhân vật mà Camera sẽ bay tới")]
    [SerializeField] private Transform cubeTarget;

    [Tooltip("Cinemachine Camera dùng để ngắm/bay tới Cube (khuyến nghị dùng chuẩn Cinemachine 3)")]
    [SerializeField] private CinemachineVirtualCameraBase cubeFlyVcam;

    [Header("=== THỜI GIAN THEO ĐỀ BÀI ===")]
    [Tooltip("Thời gian dừng ngắm Cube trước khi bay về nhân vật (Đề bài yêu cầu đúng 2 giây)")]
    [SerializeField] private float holdDurationAtCube = 2.0f;

    [Tooltip("Độ ưu tiên cao để Cinemachine kích hoạt bay tới Cube")]
    [SerializeField] private int flyPriority = 99;

    [Tooltip("Độ ưu tiên thấp khi hoàn trả")]
    [SerializeField] private int returnPriority = 0;

    [Tooltip("Thời gian giãn cách giữa các lần kích hoạt (chống kẹt trigger)")]
    [SerializeField] private float triggerCooldown = 5.0f;

    [Header("=== PHƯƠNG ÁN BAY THỦ CÔNG (FALLBACK NẾU KHÔNG DÙNG CINEMACHINE) ===")]
    [Tooltip("Tốc độ bay của camera thủ công nếu không có Cinemachine")]
    [SerializeField] private float manualFlySpeed = 12f;

    private Collider _triggerCollider;
    private bool _isFlying = false;
    private float _lastTriggerTime = -99f;
    private Coroutine _cameraFlyRoutine;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        if (_triggerCollider != null)
        {
            _triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_isFlying || Time.time - _lastTriggerTime < triggerCooldown)
            {
                return;
            }

            Debug.Log("<color=cyan>[Câu 2] Nhân vật đã di chuyển tới vị trí Trigger X! Bắt đầu bay Camera tới Cube...</color>");
            TriggerCameraFlyToCube(other.transform);
        }
    }

    /// <summary>
    /// Kích hoạt chuỗi hành động: Camera bay tới Cube -> giữ 2 giây -> bay về nhân vật.
    /// </summary>
    public void TriggerCameraFlyToCube(Transform playerTransform)
    {
        if (_cameraFlyRoutine != null) StopCoroutine(_cameraFlyRoutine);
        _cameraFlyRoutine = StartCoroutine(CameraFlySequenceRoutine(playerTransform));
    }

    private IEnumerator CameraFlySequenceRoutine(Transform playerTransform)
    {
        _isFlying = true;
        _lastTriggerTime = Time.time;

        // 1. Kích hoạt Camera bay tới vị trí Cube
        if (cubeFlyVcam != null)
        {
            // Cinemachine tự động Blend bay mượt mà tới vị trí Cube
            cubeFlyVcam.Priority = flyPriority;
            Debug.Log($"<color=yellow>[Câu 2] Cinemachine đang bay tới vị trí Cube: {cubeTarget?.name} (Priority = {flyPriority})</color>");
        }
        else
        {
            // Fallback: Bay camera bằng code Lerp nếu không có VCam
            Camera mainCam = Camera.main;
            if (mainCam != null && cubeTarget != null)
            {
                Vector3 startPos = mainCam.transform.position;
                Quaternion startRot = mainCam.transform.rotation;
                Vector3 targetPos = cubeTarget.position + new Vector3(0f, 2f, -5f);
                Quaternion targetRot = Quaternion.LookRotation(cubeTarget.position - targetPos);

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * (manualFlySpeed / Vector3.Distance(startPos, targetPos));
                    mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
            }
        }

        // 2. Chờ 2 giây tại vị trí đối tượng Cube theo đúng yêu cầu đề bài
        Debug.Log($"<color=yellow>[Câu 2] Camera đã đến Cube! Chờ đúng {holdDurationAtCube} giây theo đề bài...</color>");
        yield return new WaitForSeconds(holdDurationAtCube);

        // 3. Sau 2 giây thì Camera bay về lại vị trí nhân vật
        Debug.Log("<color=green>[Câu 2] Hết 2 giây! Camera đang bay về lại vị trí nhân vật Player...</color>");
        if (cubeFlyVcam != null)
        {
            cubeFlyVcam.Priority = returnPriority;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null && playerTransform != null)
            {
                Vector3 startPos = mainCam.transform.position;
                Quaternion startRot = mainCam.transform.rotation;
                Vector3 targetPos = playerTransform.position + new Vector3(0f, 3f, -5f);
                Quaternion targetRot = Quaternion.LookRotation(playerTransform.position - targetPos);

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * (manualFlySpeed / Vector3.Distance(startPos, targetPos));
                    mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
        _isFlying = false;
        Debug.Log("<color=green><b>[CÂU 2 HOÀN TẤT] Camera đã bay về lại vị trí nhân vật thành công!</b></color>");
    }

    private void OnDisable()
    {
        if (_cameraFlyRoutine != null)
        {
            StopCoroutine(_cameraFlyRoutine);
            _cameraFlyRoutine = null;
        }
        if (cubeFlyVcam != null)
        {
            cubeFlyVcam.Priority = returnPriority;
        }
        _isFlying = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        if (cubeTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, cubeTarget.position);
            Gizmos.DrawWireSphere(cubeTarget.position, 1.0f);
        }
    }
}
