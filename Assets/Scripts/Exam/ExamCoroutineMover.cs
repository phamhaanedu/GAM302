using System.Collections;
using UnityEngine;

/// <summary>
/// [ĐỀ THI GAM301 - CÂU 1]
/// Viết script sử dụng Coroutines để thực hiện công việc sau:
/// Di chuyển đối tượng từ vị trí A tới vị trí B.
/// Khi đối tượng đến vị trí B thì chờ 1 giây sau đó quay 90 độ (hướng xoay bất kỳ do sinh viên tự chọn)
/// sau đó chờ 1 giây rồi in ra cửa sổ console thông báo "Fire".
/// </summary>
[DisallowMultipleComponent]
public class ExamCoroutineMover : MonoBehaviour
{
    [Header("=== CẤU HÌNH VỊ TRÍ A VÀ B ===")]
    [Tooltip("Điểm xuất phát A")]
    [SerializeField] private Transform pointA;

    [Tooltip("Điểm đến B")]
    [SerializeField] private Transform pointB;

    [Tooltip("Tọa độ A dự phòng nếu không kéo Transform")]
    [SerializeField] private Vector3 fallbackPosA = new Vector3(-8f, 1f, -8f);

    [Tooltip("Tọa độ B dự phòng nếu không kéo Transform")]
    [SerializeField] private Vector3 fallbackPosB = new Vector3(-8f, 1f, 2f);

    [Header("=== THÔNG SỐ DI CHUYỂN & XOAY ===")]
    [Tooltip("Tốc độ di chuyển từ A sang B (m/s)")]
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("Góc xoay tại điểm B (độ)")]
    [SerializeField] private float rotateAngle = 90f;

    [Tooltip("Tốc độ quay (độ/giây)")]
    [SerializeField] private float rotateSpeed = 180f;

    [Tooltip("Tự động lặp lại quy trình để giám khảo tiện quan sát")]
    [SerializeField] private bool loopSequence = true;

    [Tooltip("Thời gian chờ trước khi lặp lại (giây)")]
    [SerializeField] private float loopDelay = 2f;

    [Header("=== HIỆU ỨNG THÊM (NẾU CÓ) ===")]
    [Tooltip("Particle hoặc nòng súng phụt lửa khi Fire (tùy chọn)")]
    [SerializeField] private ParticleSystem muzzleEffect;

    private Coroutine _moverRoutine;
    private Quaternion _initialRotation;

    public Vector3 TargetPosA => pointA != null ? pointA.position : fallbackPosA;
    public Vector3 TargetPosB => pointB != null ? pointB.position : fallbackPosB;

    private void Start()
    {
        _initialRotation = transform.rotation;
        StartMoveSequence();
    }

    /// <summary>
    /// Kích hoạt chuỗi hành động bằng Coroutine theo đúng yêu cầu đề bài.
    /// </summary>
    [ContextMenu("Execute Move Sequence")]
    public void StartMoveSequence()
    {
        if (_moverRoutine != null) StopCoroutine(_moverRoutine);
        _moverRoutine = StartCoroutine(MoveSequenceRoutine());
    }

    /// <summary>
    /// Coroutine thực hiện chính xác quy trình đề bài:
    /// 1. Di chuyển A -> B
    /// 2. Đến B chờ 1 giây
    /// 3. Quay 90 độ
    /// 4. Chờ 1 giây
    /// 5. In console thông báo "Fire"
    /// </summary>
    private IEnumerator MoveSequenceRoutine()
    {
        do
        {
            Vector3 posA = TargetPosA;
            Vector3 posB = TargetPosB;

            // Đặt đối tượng tại vị trí A
            transform.position = posA;
            transform.rotation = _initialRotation;
            Debug.Log($"<color=cyan>[Câu 1] Đối tượng bắt đầu di chuyển từ A ({posA}) tới B ({posB})...</color>");

            // 1. Di chuyển đối tượng từ vị trí A tới vị trí B
            while ((transform.position - posB).sqrMagnitude > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, posB, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = posB;
            Debug.Log("<color=yellow>[Câu 1] Đã đến vị trí B! Đang chờ 1 giây...</color>");

            // 2. Khi đối tượng đến vị trí B thì chờ 1 giây
            yield return new WaitForSeconds(1.0f);

            // 3. Sau đó quay 90 độ (hướng xoay quanh trục Y)
            Debug.Log("<color=yellow>[Câu 1] Đang quay 90 độ...</color>");
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, rotateAngle, 0f);

            while (Quaternion.Angle(transform.rotation, targetRot) > 0.1f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                yield return null;
            }
            transform.rotation = targetRot;

            // 4. Sau đó chờ 1 giây
            Debug.Log("<color=yellow>[Câu 1] Đã quay xong 90 độ! Đang chờ 1 giây trước khi Fire...</color>");
            yield return new WaitForSeconds(1.0f);

            // 5. In ra cửa sổ console thông báo "Fire" (ĐÚNG NGUYÊN VĂN YÊU CẦU ĐỀ THI)
            Debug.Log("Fire");
            Debug.Log("<color=red><b>[CÂU 1 HOÀN TẤT] >>> Fire <<<</b></color>");

            if (muzzleEffect != null)
            {
                muzzleEffect.Play();
            }

            if (loopSequence)
            {
                yield return new WaitForSeconds(loopDelay);
            }

        } while (loopSequence);
    }

    private void OnDisable()
    {
        if (_moverRoutine != null)
        {
            StopCoroutine(_moverRoutine);
            _moverRoutine = null;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 a = TargetPosA;
        Vector3 b = TargetPosB;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(a, 0.4f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(b, 0.4f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);
    }
}
