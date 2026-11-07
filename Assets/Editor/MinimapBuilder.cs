using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tool dựng tự động hệ thống Minimap (Minimap Camera + UI HUD)
/// Nằm ngay dưới HUD_ScoreCard theo chuẩn visual AAA.
/// </summary>
public static class MinimapBuilder
{
    private const string TEXTURE_DIR = "Assets/Textures/Minimap";
    private const string RT_PATH = "Assets/Textures/Minimap/RT_Minimap.renderTexture";
    private const string ARROW_SPRITE_PATH = "Assets/Textures/Minimap/Sprite_PlayerArrow.png";

    [MenuItem("GAM301/Build Tactical Minimap Under Score", false, 11)]
    public static void BuildMinimap()
    {
        Debug.Log("=== [MINIMAP BUILDER] BẮT ĐẦU DỰNG MINIMAP ===");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            Debug.LogError("[MinimapBuilder] Không có scene nào đang active!");
            return;
        }

        EnsureDirectories();
        RenderTexture rt = GetOrCreateRenderTexture();
        Sprite arrowSprite = GetOrCreateArrowSprite();

        // 1. Tạo hoặc cập nhật Minimap Camera
        Camera minimapCam = SetupMinimapCamera(rt);

        // 2. Tạo hoặc cập nhật Minimap UI trong Canvas
        SetupMinimapUI(minimapCam, rt, arrowSprite);

        // 3. Đánh dấu lưu Scene & Assets
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("=== [MINIMAP BUILDER] DỰNG MINIMAP HOÀN TẤT 100% ===");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(TEXTURE_DIR))
            AssetDatabase.CreateFolder("Assets/Textures", "Minimap");
    }

    private static RenderTexture GetOrCreateRenderTexture()
    {
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
        if (rt == null)
        {
            rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
            {
                name = "RT_Minimap",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            AssetDatabase.CreateAsset(rt, RT_PATH);
        }
        return rt;
    }

    private static Sprite GetOrCreateArrowSprite()
    {
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(ARROW_SPRITE_PATH);
        if (sp != null) return sp;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);

        // Xóa nền trong suốt
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        // Vẽ mũi tên tam giác con thoi hướng lên trên (0 độ)
        Vector2 tip = new Vector2(32f, 58f);
        Vector2 leftWing = new Vector2(12f, 8f);
        Vector2 rightWing = new Vector2(52f, 8f);
        Vector2 inner = new Vector2(32f, 20f);

        Color mainColor = new Color(0.0f, 0.94f, 1.0f, 1.0f);   // Neon Cyan
        Color edgeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);   // Viền trắng
        Color shadowColor = new Color(0.04f, 0.08f, 0.16f, 0.8f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);

                // Kiểm tra nằm trong cánh trái (tip, leftWing, inner) hoặc cánh phải (tip, inner, rightWing)
                bool inLeft = IsPointInTriangle(p, tip, leftWing, inner);
                bool inRight = IsPointInTriangle(p, tip, inner, rightWing);

                if (inLeft || inRight)
                {
                    // Tính cự ly tới cạnh để vẽ viền
                    float dLeft = DistanceToSegment(p, tip, leftWing);
                    float dRight = DistanceToSegment(p, tip, rightWing);
                    float dBottomL = DistanceToSegment(p, leftWing, inner);
                    float dBottomR = DistanceToSegment(p, rightWing, inner);
                    float minEdge = Mathf.Min(dLeft, Mathf.Min(dRight, Mathf.Min(dBottomL, dBottomR)));

                    if (minEdge < 1.5f)
                    {
                        tex.SetPixel(x, y, edgeColor);
                    }
                    else
                    {
                        // Gradient nhẹ từ trên xuống
                        float t = (y - 8f) / 50f;
                        Color c = Color.Lerp(new Color(0.1f, 0.7f, 1f, 1f), mainColor, t);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(ARROW_SPRITE_PATH, bytes);
        AssetDatabase.ImportAsset(ARROW_SPRITE_PATH, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(ARROW_SPRITE_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ARROW_SPRITE_PATH);
    }

    private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = Sign(pt, v1, v2);
        float d2 = Sign(pt, v2, v3);
        float d3 = Sign(pt, v3, v1);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    private static Camera SetupMinimapCamera(RenderTexture rt)
    {
        GameObject camMgr = GameObject.Find("CameraManager");
        GameObject camObj = GameObject.Find("Minimap_Camera");
        if (camObj == null)
        {
            camObj = new GameObject("Minimap_Camera");
            if (camMgr != null)
                camObj.transform.SetParent(camMgr.transform, false);
        }

        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera cam = camObj.GetComponent<Camera>();
        if (cam == null) cam = camObj.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.14f, 1f); // Nền tối đêm phù hợp map Brawl Night
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200f;
        cam.targetTexture = rt;

        // Render mọi layer NGOẠI TRỪ UI (layer 5)
        cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));

        // Cấu hình UniversalAdditionalCameraData để camera minimap cực nhẹ
        var urpData = camObj.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null) urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = false;
        urpData.antialiasing = AntialiasingMode.None;

        return cam;
    }

    private static void SetupMinimapUI(Camera minimapCam, RenderTexture rt, Sprite arrowSprite)
    {
        GameObject canvasObj = GameObject.Find("UI_Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("[MinimapBuilder] Không tìm thấy UI_Canvas!");
            return;
        }

        // Tìm font TextMeshPro có sẵn trong HUD_ScoreCard
        TMP_FontAsset font = null;
        var scoreText = GameObject.Find("Text_Score")?.GetComponent<TextMeshProUGUI>();
        if (scoreText != null) font = scoreText.font;

        // Xóa HUD_Minimap cũ nếu có
        Transform oldMinimap = canvasObj.transform.Find("HUD_Minimap");
        if (oldMinimap != null)
        {
            Object.DestroyImmediate(oldMinimap.gameObject);
        }

        // ==========================================
        // 1. TẠO CARD CONTAINER CHÍNH
        // ==========================================
        GameObject minimapCard = new GameObject("HUD_Minimap");
        minimapCard.transform.SetParent(canvasObj.transform, false);

        RectTransform rtCard = minimapCard.AddComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(1f, 1f);
        rtCard.anchorMax = new Vector2(1f, 1f);
        rtCard.pivot = new Vector2(1f, 1f);
        // ScoreCard ở (-35, -35), size 240x44 -> đáy ở -79.
        // Đặt Minimap ngay dưới với khoảng đệm 7px -> Y = -86.
        rtCard.anchoredPosition = new Vector2(-35f, -86f);
        rtCard.sizeDelta = new Vector2(240f, 216f);

        // Nền Card đồng bộ ScoreCard: RGBA(0.060, 0.090, 0.160, 0.920)
        Image bgCard = minimapCard.AddComponent<Image>();
        bgCard.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);
        bgCard.raycastTarget = false;

        Outline cardOutline = minimapCard.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.2f, 0.28f, 0.42f, 0.8f);
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // ==========================================
        // 2. HEADER BAR (Badge + Tiêu đề + Tọa độ)
        // ==========================================
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(minimapCard.transform, false);
        RectTransform rtHeader = headerObj.AddComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0f, 1f);
        rtHeader.anchorMax = new Vector2(1f, 1f);
        rtHeader.pivot = new Vector2(0.5f, 1f);
        rtHeader.anchoredPosition = new Vector2(0f, -4f);
        rtHeader.sizeDelta = new Vector2(-16f, 26f);

        // Badge icon radar (đồng bộ Badge_Star của ScoreCard)
        GameObject badgeObj = new GameObject("Badge_Radar");
        badgeObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtBadge = badgeObj.AddComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(0f, 0.5f);
        rtBadge.anchorMax = new Vector2(0f, 0.5f);
        rtBadge.pivot = new Vector2(0f, 0.5f);
        rtBadge.anchoredPosition = new Vector2(0f, 0f);
        rtBadge.sizeDelta = new Vector2(20f, 20f);

        Image imgBadge = badgeObj.AddComponent<Image>();
        imgBadge.color = new Color(0.96f, 0.62f, 0.04f, 1f); // Màu cam vàng hổ phách
        imgBadge.raycastTarget = false;

        GameObject badgeTextObj = new GameObject("Icon");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        RectTransform rtBadgeText = badgeTextObj.AddComponent<RectTransform>();
        rtBadgeText.anchorMin = Vector2.zero;
        rtBadgeText.anchorMax = Vector2.one;
        rtBadgeText.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmpBadge = badgeTextObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpBadge.font = font;
        tmpBadge.text = "◎";
        tmpBadge.fontSize = 14;
        tmpBadge.alignment = TextAlignmentOptions.Center;
        tmpBadge.color = new Color(0.06f, 0.09f, 0.16f, 1f);
        tmpBadge.raycastTarget = false;

        // Tiêu đề TACTICAL RADAR
        GameObject titleObj = new GameObject("Text_Title");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 0f);
        rtTitle.anchorMax = new Vector2(0.55f, 1f);
        rtTitle.pivot = new Vector2(0f, 0.5f);
        rtTitle.anchoredPosition = new Vector2(26f, 0f);
        rtTitle.sizeDelta = new Vector2(-26f, 0f);

        TextMeshProUGUI tmpTitle = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpTitle.font = font;
        tmpTitle.text = "RADAR";
        tmpTitle.fontSize = 12;
        tmpTitle.fontStyle = FontStyles.Bold;
        tmpTitle.color = Color.white;
        tmpTitle.alignment = TextAlignmentOptions.MidlineLeft;
        tmpTitle.raycastTarget = false;

        // Tọa độ real-time POS: X, Z
        GameObject coordsObj = new GameObject("Text_Coords");
        coordsObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtCoords = coordsObj.AddComponent<RectTransform>();
        rtCoords.anchorMin = new Vector2(0.5f, 0f);
        rtCoords.anchorMax = new Vector2(1f, 1f);
        rtCoords.pivot = new Vector2(1f, 0.5f);
        rtCoords.anchoredPosition = Vector2.zero;
        rtCoords.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpCoords = coordsObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpCoords.font = font;
        tmpCoords.text = "X:<color=#38BDF8>0</color> Z:<color=#38BDF8>0</color>";
        tmpCoords.fontSize = 11;
        tmpCoords.color = new Color(0.7f, 0.8f, 0.9f, 0.9f);
        tmpCoords.alignment = TextAlignmentOptions.MidlineRight;
        tmpCoords.raycastTarget = false;

        // ==========================================
        // 3. MAP VIEWPORT & RAWIMAGE
        // ==========================================
        GameObject viewportObj = new GameObject("Map_Viewport");
        viewportObj.transform.SetParent(minimapCard.transform, false);
        RectTransform rtViewport = viewportObj.AddComponent<RectTransform>();
        rtViewport.anchorMin = new Vector2(0.5f, 0f);
        rtViewport.anchorMax = new Vector2(0.5f, 0f);
        rtViewport.pivot = new Vector2(0.5f, 0f);
        rtViewport.anchoredPosition = new Vector2(0f, 8f);
        rtViewport.sizeDelta = new Vector2(224f, 168f);

        // Khung viền bản đồ
        Image bgViewport = viewportObj.AddComponent<Image>();
        bgViewport.color = new Color(0.04f, 0.06f, 0.1f, 1f);
        bgViewport.raycastTarget = false;
        viewportObj.AddComponent<RectMask2D>();

        // RawImage hiển thị RenderTexture của Camera
        GameObject mapRawObj = new GameObject("RawImage_Map");
        mapRawObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtMap = mapRawObj.AddComponent<RectTransform>();
        rtMap.anchorMin = Vector2.zero;
        rtMap.anchorMax = Vector2.one;
        rtMap.sizeDelta = Vector2.zero;

        RawImage rawImg = mapRawObj.AddComponent<RawImage>();
        rawImg.texture = rt;
        rawImg.raycastTarget = false;

        // Đường kẻ chữ thập trung tâm (Crosshair) mờ nhẹ
        GameObject crosshairH = new GameObject("Crosshair_H");
        crosshairH.transform.SetParent(viewportObj.transform, false);
        RectTransform rtCH = crosshairH.AddComponent<RectTransform>();
        rtCH.anchorMin = new Vector2(0f, 0.5f);
        rtCH.anchorMax = new Vector2(1f, 0.5f);
        rtCH.sizeDelta = new Vector2(0f, 1f);
        Image imgCH = crosshairH.AddComponent<Image>();
        imgCH.color = new Color(0.22f, 0.74f, 0.97f, 0.18f);
        imgCH.raycastTarget = false;

        GameObject crosshairV = new GameObject("Crosshair_V");
        crosshairV.transform.SetParent(viewportObj.transform, false);
        RectTransform rtCV = crosshairV.AddComponent<RectTransform>();
        rtCV.anchorMin = new Vector2(0.5f, 0f);
        rtCV.anchorMax = new Vector2(0.5f, 1f);
        rtCV.sizeDelta = new Vector2(1f, 0f);
        Image imgCV = crosshairV.AddComponent<Image>();
        imgCV.color = new Color(0.22f, 0.74f, 0.97f, 0.18f);
        imgCV.raycastTarget = false;

        // Ký hiệu hướng Bắc "N"
        GameObject northObj = new GameObject("Compass_North");
        northObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtNorth = northObj.AddComponent<RectTransform>();
        rtNorth.anchorMin = new Vector2(0.5f, 1f);
        rtNorth.anchorMax = new Vector2(0.5f, 1f);
        rtNorth.pivot = new Vector2(0.5f, 1f);
        rtNorth.anchoredPosition = new Vector2(0f, -2f);
        rtNorth.sizeDelta = new Vector2(24f, 16f);

        TextMeshProUGUI tmpNorth = northObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpNorth.font = font;
        tmpNorth.text = "N";
        tmpNorth.fontSize = 11;
        tmpNorth.fontStyle = FontStyles.Bold;
        tmpNorth.color = new Color(0.95f, 0.25f, 0.25f, 0.9f);
        tmpNorth.alignment = TextAlignmentOptions.Center;
        tmpNorth.raycastTarget = false;

        // ==========================================
        // 4. CON TRỎ NGƯỜI CHƠI (PLAYER MARKER)
        // ==========================================
        GameObject markerObj = new GameObject("Player_Marker");
        markerObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtMarker = markerObj.AddComponent<RectTransform>();
        rtMarker.anchorMin = new Vector2(0.5f, 0.5f);
        rtMarker.anchorMax = new Vector2(0.5f, 0.5f);
        rtMarker.pivot = new Vector2(0.5f, 0.5f);
        rtMarker.anchoredPosition = Vector2.zero;
        rtMarker.sizeDelta = new Vector2(22f, 22f);

        Image imgMarker = markerObj.AddComponent<Image>();
        if (arrowSprite != null)
        {
            imgMarker.sprite = arrowSprite;
        }
        else
        {
            imgMarker.color = new Color(0.0f, 0.94f, 1.0f, 1.0f);
        }
        imgMarker.raycastTarget = false;

        // ==========================================
        // 5. GẮN COMPONENT MINIMAP CONTROLLER
        // ==========================================
        MinimapController controller = minimapCard.AddComponent<MinimapController>();
        SerializedObject so = new SerializedObject(controller);
        var hero = GameObject.Find("Player_Hero");
        if (hero != null)
            so.FindProperty("playerTarget").objectReferenceValue = hero.transform;
        so.FindProperty("minimapCamera").objectReferenceValue = minimapCam;
        so.FindProperty("playerMarker").objectReferenceValue = rtMarker;
        so.FindProperty("coordsText").objectReferenceValue = tmpCoords;
        so.FindProperty("mapDisplay").objectReferenceValue = rawImg;
        so.FindProperty("cameraHeight").floatValue = 70f;
        so.FindProperty("defaultZoom").floatValue = 35f;
        so.ApplyModifiedProperties();

        Debug.Log("[MinimapBuilder] Gắn kết cấu trúc UI và MinimapController thành công!");
    }
}

