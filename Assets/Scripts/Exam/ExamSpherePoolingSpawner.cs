using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [ĐỀ THI GAM301 - CÂU 4]
/// Viết script sử dụng Pooling Object để từ vị trí X (vị trí bất kỳ do sinh viên tự chọn)
/// liên tục tạo ra các quả cầu bắn lên theo hướng ngẫu nhiên.
/// Khi quả cầu rơi xuống mặt đất thì biến mất.
/// </summary>
[DisallowMultipleComponent]
public class ExamSpherePoolingSpawner : MonoBehaviour
{
    [Header("=== CẤU HÌNH OBJECT POOLING ===")]
    [Tooltip("Prefab quả cầu (nếu để trống, script sẽ tự động tạo một quả cầu 3D có Mesh + Rigidbody)")]
    [SerializeField] private GameObject spherePrefab;

    [Tooltip("Kích thước khởi tạo ban đầu của Pool")]
    [SerializeField] private int initialPoolSize = 20;

    [Tooltip("Sức chứa tối đa của Pool")]
    [SerializeField] private int maxPoolSize = 50;

    [Header("=== THÔNG SỐ BẮN QUẢ CẦU ===")]
    [Tooltip("Tự động kích hoạt chu kỳ bắn liên tục khi bắt đầu")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("Khoảng cách thời gian giữa mỗi lần bắn (giây)")]
    [SerializeField] private float launchInterval = 0.45f;

    [Tooltip("Lực bắn lên tối thiểu")]
    [SerializeField] private float launchForceMin = 7.0f;

    [Tooltip("Lực bắn lên tối đa")]
    [SerializeField] private float launchForceMax = 11.0f;

    [Tooltip("Độ phân tán góc ngẫu nhiên theo phương ngang XZ")]
    [SerializeField] private float horizontalSpread = 1.0f;

    [Header("=== TRẠNG THÁI RUNTIME ===")]
    [SerializeField] private bool isSpawning = false;
    [SerializeField] private int activeSphereCount = 0;

    private readonly Queue<GameObject> _poolQueue = new Queue<GameObject>();
    private Transform _poolContainer;
    private Coroutine _spawnRoutine;

    public bool IsSpawning => isSpawning;
    public int ActiveSphereCount => activeSphereCount;

    private void Awake()
    {
        // Tạo container gom các đối tượng pool cho sạch Hierarchy
        GameObject containerGo = new GameObject("Exam_SpherePool_Container");
        containerGo.transform.SetParent(transform);
        _poolContainer = containerGo.transform;

        InitializePool();
    }

    private void Start()
    {
        if (autoStart)
        {
            StartContinuousSpawning();
        }
    }

    /// <summary>
    /// Khởi tạo sẵn một lượng quả cầu trong Pool (Object Pooling Pattern).
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject sphere = CreateNewSphereInstance();
            sphere.SetActive(false);
            _poolQueue.Enqueue(sphere);
        }
        Debug.Log($"<color=cyan>[Câu 4] Đã khởi tạo Object Pool với {initialPoolSize} quả cầu tại '{gameObject.name}'</color>");
    }

    /// <summary>
    /// Tạo một GameObject quả cầu chuẩn 3D vật lý.
    /// </summary>
    private GameObject CreateNewSphereInstance()
    {
        GameObject obj;
        if (spherePrefab != null)
        {
            obj = Instantiate(spherePrefab, _poolContainer);
        }
        else
        {
            // Tự sinh Sphere nếu chưa kéo Prefab
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Exam_Pooled_Sphere";
            obj.transform.localScale = Vector3.one * 0.6f;
            obj.transform.SetParent(_poolContainer);

            // Gán Material màu neon rực rỡ để dễ nhận biết
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1f, 0.45f, 0.05f); // Cam Neon rực rỡ
                rend.sharedMaterial = mat;
            }
        }

        // Đảm bảo có Rigidbody và ExamPooledSphere
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        ExamPooledSphere pooledComponent = obj.GetComponent<ExamPooledSphere>();
        if (pooledComponent == null) pooledComponent = obj.AddComponent<ExamPooledSphere>();

        return obj;
    }

    /// <summary>
    /// Bắt đầu liên tục bắn quả cầu lên theo hướng ngẫu nhiên.
    /// </summary>
    [ContextMenu("Start Continuous Spawning")]
    public void StartContinuousSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = StartCoroutine(ContinuousSpawnRoutine());
        Debug.Log("<color=green>[Câu 4] BẮT ĐẦU BẮN QUẢ CẦU LIÊN TỤC TỪ VỊ TRÍ X!</color>");
    }

    /// <summary>
    /// Dừng bắn quả cầu.
    /// </summary>
    [ContextMenu("Stop Continuous Spawning")]
    public void StopContinuousSpawning()
    {
        if (!isSpawning) return;
        isSpawning = false;
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
        Debug.Log("<color=yellow>[Câu 4] Đã dừng bắn quả cầu.</color>");
    }

    private IEnumerator ContinuousSpawnRoutine()
    {
        while (isSpawning)
        {
            LaunchOneSphere();
            yield return new WaitForSeconds(launchInterval);
        }
    }

    /// <summary>
    /// Lấy một quả cầu từ Pool và bắn lên theo hướng ngẫu nhiên.
    /// </summary>
    [ContextMenu("Launch One Sphere")]
    public GameObject LaunchOneSphere()
    {
        GameObject sphere;
        if (_poolQueue.Count > 0)
        {
            sphere = _poolQueue.Dequeue();
        }
        else
        {
            sphere = CreateNewSphereInstance();
        }

        // Đặt tại vị trí X của Spawner
        sphere.transform.position = transform.position;
        sphere.transform.rotation = Quaternion.identity;
        sphere.SetActive(true);
        activeSphereCount++;

        // Khởi tạo component quản lý rơi xuống đất
        ExamPooledSphere pooledComp = sphere.GetComponent<ExamPooledSphere>();
        if (pooledComp != null)
        {
            pooledComp.Initialize(ReturnSphereToPool);
        }

        // Bắn lên theo hướng ngẫu nhiên (Vector ngẫu nhiên hướng lên trên)
        Rigidbody rb = sphere.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 randomHorizontal = new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                0f,
                Random.Range(-horizontalSpread, horizontalSpread)
            );
            // Luôn có thành phần hướng lên (upward) kết hợp góc ngẫu nhiên
            Vector3 launchDirection = (Vector3.up * 2.2f + randomHorizontal).normalized;
            float force = Random.Range(launchForceMin, launchForceMax);

            rb.AddForce(launchDirection * force, ForceMode.Impulse);
        }

        return sphere;
    }

    /// <summary>
    /// Hàm callback khi quả cầu rơi xuống mặt đất -> biến mất và thu hồi về Pool.
    /// </summary>
    public void ReturnSphereToPool(GameObject sphere)
    {
        if (sphere == null) return;

        sphere.SetActive(false);
        activeSphereCount = Mathf.Max(0, activeSphereCount - 1);

        if (_poolQueue.Count < maxPoolSize)
        {
            _poolQueue.Enqueue(sphere);
        }
        else
        {
            Destroy(sphere);
        }
    }

    private void OnDisable()
    {
        StopContinuousSpawning();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}
