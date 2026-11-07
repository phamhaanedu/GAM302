using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tái sử dụng đối tượng (Object Pooling) đa năng cho dự án GAM301.
/// Tránh tình trạng Instantiate/Destroy liên tục gây rác bộ nhớ (Garbage Collection spikes).
/// Hỗ trợ nạp sẵn đạn người chơi, đạn quái, quả cầu nảy, quái tuần tra và các hiệu ứng VFX.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Serializable]
    public class PoolItem
    {
        [Tooltip("Mã định danh duy nhất (Key) cho loại đối tượng trong pool")]
        public string poolKey;

        [Tooltip("Prefab gốc cần tạo bản sao")]
        public GameObject prefab;

        [Tooltip("Số lượng đối tượng nạp sẵn ban đầu khi khởi động game")]
        public int initialSize = 10;

        [Tooltip("Có cho phép tự động nở thêm khi hết đối tượng có sẵn không")]
        public bool expandable = true;
    }

    [Header("=== CẤU HÌNH DANH SÁCH OBJECT POOLS ===")]
    [Tooltip("Danh sách các đối tượng cần khởi tạo trước")]
    [SerializeField] private List<PoolItem> poolConfigurations = new List<PoolItem>();

    // Dictionary lưu trữ hàng đợi đối tượng rảnh rỗi cho từng poolKey
    private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, PoolItem> configDictionary = new Dictionary<string, PoolItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePools();
    }

    /// <summary>
    /// Khởi tạo các pool đã được cấu hình trong Inspector.
    /// </summary>
    private void InitializePools()
    {
        foreach (var item in poolConfigurations)
        {
            if (item.prefab == null || string.IsNullOrEmpty(item.poolKey)) continue;

            if (!poolDictionary.ContainsKey(item.poolKey))
            {
                poolDictionary[item.poolKey] = new Queue<GameObject>();
                configDictionary[item.poolKey] = item;

                // Tạo Transform cha để gom gọn Hierarchy trong Scene
                GameObject parentObj = new GameObject($"Pool_{item.poolKey}");
                parentObj.transform.SetParent(this.transform);

                for (int i = 0; i < item.initialSize; i++)
                {
                    GameObject obj = Instantiate(item.prefab, parentObj.transform);
                    obj.name = $"{item.poolKey}_{i:00}";
                    obj.SetActive(false);
                    poolDictionary[item.poolKey].Enqueue(obj);
                }
            }
        }
    }

    /// <summary>
    /// Lấy một đối tượng từ Pool theo key định danh.
    /// </summary>
    /// <param name="poolKey">Khóa định danh đối tượng</param>
    /// <param name="position">Vị trí muốn đặt đối tượng</param>
    /// <param name="rotation">Góc xoay muốn đặt đối tượng</param>
    /// <returns>GameObject đang hoạt động hoặc null nếu không thể tạo</returns>
    public GameObject Spawn(string poolKey, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolKey))
        {
            Debug.LogWarning($"[ObjectPoolManager] Không tìm thấy pool có key: '{poolKey}'!");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[poolKey];
        GameObject obj = null;

        // Tìm kiếm đối tượng đang rảnh
        while (queue.Count > 0)
        {
            GameObject candidate = queue.Dequeue();
            if (candidate != null)
            {
                obj = candidate;
                break;
            }
        }

        // Nếu hết đối tượng và cho phép mở rộng (expandable)
        if (obj == null)
        {
            if (configDictionary[poolKey].expandable)
            {
                Transform parent = transform.Find($"Pool_{poolKey}");
                obj = Instantiate(configDictionary[poolKey].prefab, parent);
                obj.name = $"{poolKey}_Expanded_{queue.Count}";
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolKey}' đã cạn và không cho phép nở thêm!");
                return null;
            }
        }

        // Thiết lập vị trí, góc xoay và kích hoạt
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // EXAM HOOK: Gọi hàm tái kích hoạt hoặc reset thông số nếu có interface
        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnSpawnFromPool();

        return obj;
    }

    /// <summary>
    /// Trả đối tượng về Pool để tái sử dụng trong tương lai.
    /// </summary>
    /// <param name="poolKey">Khóa định danh đối tượng</param>
    /// <param name="obj">GameObject muốn thu hồi</param>
    public void Despawn(string poolKey, GameObject obj)
    {
        if (obj == null) return;

        if (!poolDictionary.ContainsKey(poolKey))
        {
            Debug.LogWarning($"[ObjectPoolManager] Không thể hoàn trả. Không tồn tại pool: '{poolKey}'!");
            Destroy(obj);
            return;
        }

        // EXAM HOOK: Gọi hàm dọn dẹp trước khi tắt đối tượng
        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnReturnToPool();

        obj.SetActive(false);
        poolDictionary[poolKey].Enqueue(obj);
    }
}

/// <summary>
/// Interface cho các đối tượng cần reset trạng thái khi được lấy ra hoặc cất vào Pool.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Kích hoạt khi vừa được lấy ra khỏi Pool.
    /// </summary>
    void OnSpawnFromPool();

    /// <summary>
    /// Kích hoạt khi chuẩn bị trả ngược về Pool.
    /// </summary>
    void OnReturnToPool();
}
