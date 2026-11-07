using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tool nâng cấp đạn bay laser uy lực, gắn vệt sáng TrailRenderer phát quang HDR,
/// đổi Skybox sang Space_AnotherPlanet, và hoàn thiện Popup Game Over chuẩn AAA.
/// </summary>
public static class ApexBulletAndSkyboxUpgrade
{
    [MenuItem("Tools/GAM301/Upgrade Bullet, Skybox & Game Over UI")]
    public static void ExecuteUpgrade()
    {
        Debug.Log("<color=cyan>=== BẮT ĐẦU NÂNG CẤP BULLET, SKYBOX & GAME OVER UI ===</color>");

        UpgradeSkybox();
        UpgradeBulletPrefabs();
        EnsureGameOverUIPopup();

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>=== HOÀN TẤT NÂNG CẤP TOÀN DIỆN! ===</color>");
    }

    /// <summary>
    /// 1. Đổi Skybox của màn đấu sang Space_AnotherPlanet
    /// </summary>
    private static void UpgradeSkybox()
    {
        string skyMatPath = "Assets/Import/AllSkyFree/Space_AnotherPlanet/AllSky_Space_AnotherPlanet.mat";
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
        if (skyMat != null)
        {
            RenderSettings.skybox = skyMat;
            DynamicGI.UpdateEnvironment();
            Debug.Log($"[Skybox] Đã đổi Skybox sang: {skyMatPath}");
        }
        else
        {
            Debug.LogError($"[Skybox] Không tìm thấy file: {skyMatPath}");
        }

        // Đảm bảo MainCamera ClearFlags = Skybox
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    /// <summary>
    /// 2. Nâng cấp viên đạn Player và Enemy thành đạn Laser uy lực, vệt sáng xé gió & Point Light phát quang
    /// </summary>
    private static void UpgradeBulletPrefabs()
    {
        // 2.1 Tạo Material cho Trail phát quang HDR nếu chưa có
        Material trailMatPlayer = GetOrCreateTrailMaterial("Assets/Materials/Mat_Trail_Player.mat", new Color(0f, 0.95f, 1f, 1f) * 2.5f);
        Material trailMatEnemy = GetOrCreateTrailMaterial("Assets/Materials/Mat_Trail_Enemy.mat", new Color(1f, 0.25f, 0.1f, 1f) * 2.5f);

        // 2.2 Nâng cấp PFB_PlayerBullet
        UpgradeSingleBullet("Assets/Prefabs/PFB_PlayerBullet.prefab", trailMatPlayer, new Color(0f, 0.95f, 1f), "PlayerBullet", 45f);

        // 2.3 Nâng cấp PFB_EnemyBullet
        UpgradeSingleBullet("Assets/Prefabs/PFB_EnemyBullet.prefab", trailMatEnemy, new Color(1f, 0.25f, 0.1f), "EnemyBullet", 35f);
    }

    private static Material GetOrCreateTrailMaterial(string path, Color hdrColor)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            mat = new Material(shader);
            mat.name = Path.GetFileNameWithoutExtension(path);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdrColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", hdrColor);

            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdrColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", hdrColor);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static void UpgradeSingleBullet(string prefabPath, Material trailMat, Color glowColor, string poolKey, float bulletSpeed)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        // Mở prefab để chỉnh sửa
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);

        // A. Tinh chỉnh Mesh đầu đạn (vuốt thon laser)
        root.transform.localScale = new Vector3(0.2f, 0.2f, 0.65f);

        // B. Cấu hình BulletProjectile
        BulletProjectile proj = root.GetComponent<BulletProjectile>();
        if (proj == null) proj = root.AddComponent<BulletProjectile>();
        var so = new SerializedObject(proj);
        so.FindProperty("speed").floatValue = bulletSpeed;
        so.FindProperty("poolKey").stringValue = poolKey;
        so.ApplyModifiedProperties();

        // C. Nâng cấp TrailRenderer (Vệt sáng uy lực theo Câu 3)
        TrailRenderer tr = root.GetComponent<TrailRenderer>();
        if (tr == null) tr = root.AddComponent<TrailRenderer>();
        tr.material = trailMat;
        tr.time = 0.25f; // Thời gian vệt sáng
        tr.minVertexDistance = 0.05f;

        // Đường cong độ dày vệt sáng: Đầu to 0.25m -> Đuôi vuốt nhọn về 0.0m
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.28f);
        widthCurve.AddKey(1f, 0.0f);
        tr.widthCurve = widthCurve;

        // Gradient màu phát sáng rực rỡ
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0.0f),
                new GradientColorKey(Color.white, 0.4f),
                new GradientColorKey(glowColor * 0.7f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        tr.colorGradient = grad;

        // D. Gắn Point Light động phát sáng mặt đất/tường khi bay qua
        Light light = root.GetComponent<Light>();
        if (light == null) light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 3.5f;
        light.intensity = 2.5f;
        light.color = glowColor;

        // E. Nâng cấp Material đầu đạn có Emission HDR để ăn Bloom
        MeshRenderer mr = root.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            Material bMat = mr.sharedMaterial;
            bMat.EnableKeyword("_EMISSION");
            if (bMat.HasProperty("_EmissionColor"))
            {
                bMat.SetColor("_EmissionColor", glowColor * 3.0f);
            }
            if (bMat.HasProperty("_BaseColor"))
            {
                bMat.SetColor("_BaseColor", glowColor);
            }
            EditorUtility.SetDirty(bMat);
        }

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[Bullet] Đã nâng cấp Prefab uy lực: {prefabPath}");
    }

    /// <summary>
    /// 3. Đảm bảo Popup Game Over luôn đầy đủ linh kiện, đẹp mắt và sẵn sàng xuất hiện
    /// </summary>
    private static void EnsureGameOverUIPopup()
    {
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas == null) return;

        var panelGO = canvas.transform.Find("Panel_GameOver")?.gameObject;
        if (panelGO == null) return;

        // Đảm bảo có GameOverPopupUI
        var comp = panelGO.GetComponent<GameOverPopupUI>();
        if (comp == null) comp = panelGO.AddComponent<GameOverPopupUI>();

        // Nối các component
        var so = new SerializedObject(comp);
        so.FindProperty("popupRoot").objectReferenceValue = panelGO;
        so.FindProperty("canvasGroup").objectReferenceValue = panelGO.GetComponent<CanvasGroup>();
        
        var txtScore = panelGO.transform.Find("Card_Container/Text_FinalScore")?.GetComponent<TextMeshProUGUI>();
        if (txtScore != null) so.FindProperty("finalScoreText").objectReferenceValue = txtScore;

        var btnRestart = panelGO.transform.Find("Card_Container/Button_Restart")?.GetComponent<Button>();
        if (btnRestart != null) so.FindProperty("restartButton").objectReferenceValue = btnRestart;

        var btnQuit = panelGO.transform.Find("Card_Container/Button_Quit")?.GetComponent<Button>();
        if (btnQuit != null) so.FindProperty("quitButton").objectReferenceValue = btnQuit;

        so.ApplyModifiedProperties();

        // Đảm bảo Panel_GameOver ban đầu activeSelf = false
        panelGO.SetActive(false);

        Debug.Log("[GameOverUI] Đã cấu hình và kiểm tra liên kết GameOverPopupUI thành công!");
    }
}
