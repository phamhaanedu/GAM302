using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

/// <summary>
/// Tool thay thế Environment hiện tại bằng toàn bộ sân đấu Brawl Arena (ShowDown_Night)
/// Tự động gắn Colliders (Ground, Wall), Tags, Layers và Bake NavMesh.
/// Chuẩn hóa theo tôn chỉ Apex System Agent [W]eat.
/// </summary>
public static class BrawlArenaEnvironmentReplacer
{
    private const string SOURCE_SCENE_PATH = "Assets/Import/Brawl Arena/Scenes/ShowDown_Night.unity";

    [MenuItem("GAM301/Replace Environment with Brawl Arena", false, 10)]
    public static void ReplaceEnvironment()
    {
        Debug.Log("=== [BRAWL ARENA REPLACER] BẮT ĐẦU THAY THẾ SÂN ĐẤU ===");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            Debug.LogError("[BrawlArenaReplacer] Không có scene active!");
            return;
        }

        // 1. Mở Scene nguồn ShowDown_Night Additive
        var sourceScene = EditorSceneManager.OpenScene(SOURCE_SCENE_PATH, OpenSceneMode.Additive);
        GameObject sourceArenaRoot = null;
        foreach (var root in sourceScene.GetRootGameObjects())
        {
            if (root.name == "ShowDown_Night")
            {
                sourceArenaRoot = root;
                break;
            }
        }

        if (sourceArenaRoot == null)
        {
            Debug.LogError("[BrawlArenaReplacer] Không tìm thấy 'ShowDown_Night' trong scene nguồn!");
            EditorSceneManager.CloseScene(sourceScene, true);
            return;
        }

        // 2. Clone ShowDown_Night sang active scene
        GameObject newArena = Object.Instantiate(sourceArenaRoot);
        newArena.name = "Environment";
        EditorSceneManager.MoveGameObjectToScene(newArena, activeScene);

        // Đóng scene nguồn
        EditorSceneManager.CloseScene(sourceScene, true);

        // 3. Tìm root section -- ENVIRONMENT & TRIGGERS --
        GameObject rootEnvSection = GameObject.Find("-- ENVIRONMENT & TRIGGERS --");
        if (rootEnvSection == null)
        {
            rootEnvSection = new GameObject("-- ENVIRONMENT & TRIGGERS --");
            EditorSceneManager.MoveGameObjectToScene(rootEnvSection, activeScene);
        }

        // 4. Xóa Environment cũ nếu có
        for (int i = rootEnvSection.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = rootEnvSection.transform.GetChild(i);
            if (child.gameObject != newArena && child.name.Contains("Environment"))
            {
                Debug.Log($"[BrawlArenaReplacer] Xóa Environment cũ: {child.name}");
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // 5. Đặt newArena làm con của -- ENVIRONMENT & TRIGGERS --
        newArena.transform.SetParent(rootEnvSection.transform, false);
        newArena.transform.position = Vector3.zero;
        newArena.transform.rotation = Quaternion.identity;
        newArena.transform.localScale = Vector3.one;

        // 6. Cấu hình Tags và Layers
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1) groundLayer = 0;
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer == -1) wallLayer = 0;

        NavMeshSurface navMeshSurface = null;

        // Xử lý từng đối tượng con trong sân đấu
        for (int i = 0; i < newArena.transform.childCount; i++)
        {
            Transform tr = newArena.transform.GetChild(i);
            string name = tr.name;

            // Xử lý Sàn đấu chính (Night_Plane)
            if (name.StartsWith("Night_Plane"))
            {
                tr.tag = "Ground";
                tr.gameObject.layer = groundLayer;

                var mc = tr.GetComponent<MeshCollider>();
                if (mc == null) mc = tr.gameObject.AddComponent<MeshCollider>();
                var mf = tr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                }

                // Gắn NavMeshSurface lên Night_Plane
                navMeshSurface = tr.GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = tr.gameObject.AddComponent<NavMeshSurface>();
                }
                navMeshSurface.collectObjects = CollectObjects.All;
            }
            // Xử lý Tường gạch (Wall_Tile_Block1)
            else if (name.StartsWith("Wall_Tile_Block1"))
            {
                tr.tag = "Wall";
                tr.gameObject.layer = wallLayer;

                var bc = tr.GetComponent<BoxCollider>();
                if (bc == null) bc = tr.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.24f, 0f);
                bc.size = new Vector3(2.13f, 2.48f, 2.13f);
            }
            // Xử lý Hòm rương (Chest)
            else if (name.StartsWith("Chest"))
            {
                tr.tag = "Wall";
                tr.gameObject.layer = wallLayer;

                var bc = tr.GetComponent<BoxCollider>();
                if (bc == null) bc = tr.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.24f, 0f);
                bc.size = new Vector3(2.09f, 2.56f, 2.0f);
            }
            // Xử lý Đá chóp (PolyPyramid)
            else if (name.StartsWith("PolyPyramid"))
            {
                tr.tag = "Wall";
                tr.gameObject.layer = wallLayer;

                var mc = tr.GetComponent<MeshCollider>();
                if (mc == null) mc = tr.gameObject.AddComponent<MeshCollider>();
                var mf = tr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            // Xử lý Đá tảng phẳng (RockFlat3, RockFlat4, RockFlat5, RockFlat6)
            else if (name.StartsWith("RockFlat"))
            {
                tr.tag = "Wall";
                tr.gameObject.layer = wallLayer;

                var mc = tr.GetComponent<MeshCollider>();
                if (mc == null) mc = tr.gameObject.AddComponent<MeshCollider>();
                var mf = tr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            // Cỏ (Grass_Tile_Night) và Xương (Bone, Skull) -> giữ nguyên không có collider để đi xuyên mượt mà
        }

        // 7. Bake NavMesh
        if (navMeshSurface != null)
        {
            Debug.Log("[BrawlArenaReplacer] Đang Bake NavMesh Surface cho sân đấu mới...");
            navMeshSurface.BuildNavMesh();
            Debug.Log("[BrawlArenaReplacer] Bake NavMesh thành công!");
        }

        // 8. Đặt lại vị trí Player ở trung tâm sàn đấu an toàn
        GameObject playerObj = GameObject.Find("Player_Hero");
        if (playerObj != null)
        {
            playerObj.transform.position = new Vector3(0f, 1.0f, 0f);
            Debug.Log("[BrawlArenaReplacer] Đã định vị Player_Hero tại (0, 1, 0) trên sàn đấu mới.");
        }

        // 9. Lưu scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("=== [BRAWL ARENA REPLACER] THAY THẾ SÂN ĐẤU BRAWL ARENA HOÀN TẤT 100% ===");
    }
}

