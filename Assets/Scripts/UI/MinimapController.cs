using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller quản lý hoạt động của Minimap:
/// - Điều khiển Minimap Camera bám theo Player ở góc nhìn trực giao từ trên cao.
/// - Đồng bộ hướng quay của Player Marker trên giao diện UI.
/// - Cập nhật tọa độ thời gian thực (X, Z).
/// - Khử rung lắc bằng LateUpdate().
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("=== TARGET & CAMERA SETTINGS ===")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private float cameraHeight = 70f;
    [SerializeField] private float defaultZoom = 35f;

    [Header("=== UI REFERENCES ===")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private TextMeshProUGUI coordsText;
    [SerializeField] private RawImage mapDisplay;

    private void Awake()
    {
        FindPlayerIfNeeded();
    }

    private void Start()
    {
        FindPlayerIfNeeded();

        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = defaultZoom;
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            FindPlayerIfNeeded();
            if (playerTarget == null) return;
        }

        Vector3 pPos = playerTarget.position;

        // 1. Cập nhật vị trí Minimap Camera nhìn vuông góc từ trên xuống
        if (minimapCamera != null)
        {
            minimapCamera.transform.position = new Vector3(pPos.x, pPos.y + cameraHeight, pPos.z);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // 2. Cập nhật góc quay của con trỏ Player trên Minimap UI
        if (playerMarker != null)
        {
            // Camera cố định hướng Bắc (0 độ), con trỏ quay theo góc nhìn thực của Player
            float angle = playerTarget.eulerAngles.y;
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // 3. Cập nhật tọa độ HUD
        if (coordsText != null)
        {
            coordsText.text = $"X:<color=#38BDF8>{pPos.x:0}</color> Z:<color=#38BDF8>{pPos.z:0}</color>";
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null) return;

        var hero = GameObject.Find("Player_Hero");
        if (hero != null)
        {
            playerTarget = hero.transform;
            return;
        }

        var tagged = GameObject.FindWithTag("Player");
        if (tagged != null)
        {
            playerTarget = tagged.transform;
        }
    }

    /// <summary>
    /// Thay đổi mức độ thu phóng (Zoom) của Minimap
    /// </summary>
    public void SetZoom(float zoomSize)
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = Mathf.Clamp(zoomSize, 15f, 80f);
        }
    }
}

