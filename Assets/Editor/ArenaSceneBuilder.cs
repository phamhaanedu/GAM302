using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Unity.AI.Navigation;

/// <summary>
/// Công cụ Editor tự động tạo lập 100% tài nguyên Prefabs, Materials và Scene Hierarchy
/// cho Arena Dungeon 3D Base Project theo đúng tài liệu GDD GAM301.
/// </summary>
public static class ArenaSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/MainArena_GAM301_Base.unity";
    private const string PREFAB_DIR = "Assets/Prefabs";
    private const string MAT_DIR = "Assets/Materials";

    [MenuItem("GAM301/Build Full Arena Dungeon Project", false, 1)]
    public static void BuildFullProject()
    {
        Debug.Log("=== [GAM301 BUILDER] BẮT ĐẦU DỰNG TOÀN BỘ PROJECT BASE ===");

        EnsureDirectories();
        CreateMaterials();
        CreatePrefabs();
        BuildScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        PrintHierarchyTree();

        Debug.Log("=== [GAM301 BUILDER] HOÀN TẤT XÂY DỰNG 100% THÀNH CÔNG ===");
    }

    [MenuItem("GAM301/Print Hierarchy Tree", false, 2)]
    public static void PrintHierarchyTree()
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        GameObject[] rootObjects = scene.GetRootGameObjects();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"\n=== [SCENE HIERARCHY TREE]: {scene.name} ===");

        foreach (var root in rootObjects)
        {
            PrintGameObjectRecursive(root, 0, sb);
        }

        Debug.Log(sb.ToString());
    }

    [MenuItem("GAM301/Fix EventSystem in Active Scene", false, 3)]
    public static void FixActiveSceneInput()
    {
        var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                EditorSceneManager.MarkSceneDirty(es.gameObject.scene);
                EditorSceneManager.SaveScene(es.gameObject.scene);
                Debug.Log("[GAM301] Đã tự động thay thế StandaloneInputModule bằng InputSystemUIInputModule!");
            }
        }
    }

    private static void PrintGameObjectRecursive(GameObject go, int depth, System.Text.StringBuilder sb)
    {
        string indent = new string(' ', depth * 4);
        string components = string.Join(", ", System.Array.ConvertAll(go.GetComponents<Component>(), c => c != null ? c.GetType().Name : "Missing"));
        sb.AppendLine($"{indent}├── {go.name}  [{components}]");

        for (int i = 0; i < go.transform.childCount; i++)
        {
            PrintGameObjectRecursive(go.transform.GetChild(i).gameObject, depth + 1, sb);
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(PREFAB_DIR)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(MAT_DIR)) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static Material GetOrCreateMaterial(string name, Color color, float metallic = 0.1f, float smoothness = 0.5f)
    {
        string path = $"{MAT_DIR}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            mat = new Material(shader)
            {
                name = name,
                color = color
            };
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static void CreateMaterials()
    {
        GetOrCreateMaterial("Mat_Ground", new Color(0.2f, 0.22f, 0.25f));      // Xám đá tối
        GetOrCreateMaterial("Mat_Wall", new Color(0.12f, 0.14f, 0.16f));        // Tường xám đậm
        GetOrCreateMaterial("Mat_Player", new Color(0.1f, 0.6f, 1.0f));        // Xanh dương hero
        GetOrCreateMaterial("Mat_EnemyPatrol", new Color(0.9f, 0.2f, 0.2f));   // Đỏ quái tuần tra
        GetOrCreateMaterial("Mat_EnemyStationary", new Color(0.8f, 0.4f, 0.1f));// Cam quái đứng yên
        GetOrCreateMaterial("Mat_PlayerBullet", new Color(1.0f, 0.9f, 0.2f));   // Vàng rực đạn ta
        GetOrCreateMaterial("Mat_EnemyBullet", new Color(1.0f, 0.1f, 0.4f));    // Đỏ hồng đạn địch
        GetOrCreateMaterial("Mat_BouncingSphere", new Color(0.3f, 0.9f, 0.4f)); // Xanh lá quả cầu nảy
        GetOrCreateMaterial("Mat_TargetFar", new Color(0.9f, 0.1f, 0.9f));      // Tím mục tiêu xa
    }

    private static void CreatePrefabs()
    {
        CreatePlayerBulletPrefab();
        CreateEnemyBulletPrefab();
        CreateSphereProjectilePrefab();
        CreateEnemyPatrolPrefab();
        CreateVFXExplosionPrefab();
        CreateVFXSparkPrefab();
    }

    private static void CreatePlayerBulletPrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_PlayerBullet.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PFB_PlayerBullet";
        go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetOrCreateMaterial("Mat_PlayerBullet", new Color(1f, 0.9f, 0.2f));

        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Bổ sung Trail Renderer theo yêu cầu Câu 3
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.02f;
        trail.material = rend.sharedMaterial;

        go.AddComponent<BulletProjectile>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateEnemyBulletPrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_EnemyBullet.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PFB_EnemyBullet";
        go.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetOrCreateMaterial("Mat_EnemyBullet", new Color(1f, 0.1f, 0.4f));

        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Bổ sung Trail Renderer theo đúng yêu cầu GDD
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.02f;
        trail.material = rend.sharedMaterial;

        go.AddComponent<BulletProjectile>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateSphereProjectilePrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_SphereProjectile.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PFB_SphereProjectile";
        go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetOrCreateMaterial("Mat_BouncingSphere", new Color(0.3f, 0.9f, 0.4f));

        // Tạo PhysicMaterial nảy cao
        PhysicsMaterial physMat = new PhysicsMaterial("PhysMat_SuperBounce")
        {
            bounciness = 0.92f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            dynamicFriction = 0.1f,
            staticFriction = 0.1f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(physMat, $"{MAT_DIR}/PhysMat_SuperBounce.physicMaterial");

        SphereCollider col = go.GetComponent<SphereCollider>();
        col.material = physMat;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        go.AddComponent<BouncingSphere>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateEnemyPatrolPrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_Enemy_Patrol.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "PFB_Enemy_Patrol";
        go.tag = "Enemy";

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetOrCreateMaterial("Mat_EnemyPatrol", new Color(0.9f, 0.2f, 0.2f));

        NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.stoppingDistance = 0.5f;

        go.AddComponent<EnemyPatrolFSM>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateVFXExplosionPrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_VFX_Explosion.prefab";
        GameObject go = new GameObject("PFB_VFX_Explosion");

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 8f;
        main.startSize = 0.4f;
        main.startColor = new Color(1f, 0.5f, 0.1f);
        main.stopAction = ParticleSystemStopAction.Disable;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateVFXSparkPrefab()
    {
        string path = $"{PREFAB_DIR}/PFB_VFX_Spark.prefab";
        GameObject go = new GameObject("PFB_VFX_Spark");

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 5f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.9f, 0.4f);
        main.stopAction = ParticleSystemStopAction.Disable;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void BuildScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ==========================================
        // 0. ÁNH SÁNG MÔI TRƯỜNG & KHỞI TẠO CÁC NHÓM GỐC
        // ==========================================
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.96f, 0.88f);
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 4 ROOT SECTIONS CHUẨN GDD
        GameObject rootManagers = CreateSectionRoot("-- MANAGERS & GLOBAL SYSTEMS --");
        GameObject rootPlayer = CreateSectionRoot("-- PLAYER & CHARACTERS --");
        GameObject rootEnv = CreateSectionRoot("-- ENVIRONMENT & TRIGGERS --");
        GameObject rootUI = CreateSectionRoot("-- UI CANVAS & VFX ANCHORS --");

        // ==========================================
        // A. NHÓM MANAGERS & GLOBAL SYSTEMS
        // ==========================================
        GameObject gmObj = new GameObject("GameMaster");
        gmObj.transform.SetParent(rootManagers.transform, false);
        GameMaster gm = gmObj.AddComponent<GameMaster>();

        GameObject camMgrObj = new GameObject("CameraManager");
        camMgrObj.transform.SetParent(rootManagers.transform, false);
        CameraManager camMgr = camMgrObj.AddComponent<CameraManager>();

        // Main Camera
        GameObject mainCamObj = new GameObject("MainCamera");
        mainCamObj.transform.SetParent(camMgrObj.transform, false);
        mainCamObj.tag = "MainCamera";
        Camera cam = mainCamObj.AddComponent<Camera>();
        mainCamObj.AddComponent<CinemachineBrain>();
        mainCamObj.AddComponent<AudioListener>();

        // CM_vcam_3rdPerson
        GameObject vcam3rdObj = new GameObject("CM_vcam_3rdPerson");
        vcam3rdObj.transform.SetParent(camMgrObj.transform, false);
        CinemachineCamera vcam3rd = vcam3rdObj.AddComponent<CinemachineCamera>();
        vcam3rd.Priority = 20;
        var follow3rd = vcam3rdObj.AddComponent<CinemachineFollow>();
        follow3rd.FollowOffset = new Vector3(0f, 4.5f, -7.5f);
        follow3rd.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        var aim3rd = vcam3rdObj.AddComponent<CinemachineRotationComposer>();
        aim3rd.TargetOffset = new Vector3(0f, 1.5f, 0f);

        // CM_vcam_1stPerson
        GameObject vcam1stObj = new GameObject("CM_vcam_1stPerson");
        vcam1stObj.transform.SetParent(camMgrObj.transform, false);
        CinemachineCamera vcam1st = vcam1stObj.AddComponent<CinemachineCamera>();
        vcam1st.Priority = 10;
        var follow1st = vcam1stObj.AddComponent<CinemachineFollow>();
        follow1st.FollowOffset = new Vector3(0f, 1.6f, 0.3f);
        follow1st.TrackerSettings.BindingMode = BindingMode.LockToTargetWithWorldUp;
        var aim1st = vcam1stObj.AddComponent<CinemachineRotationComposer>();
        aim1st.TargetOffset = new Vector3(0f, 1.6f, 5f);

        // CM_vcam_Death
        GameObject vcamDeathObj = new GameObject("CM_vcam_Death");
        vcamDeathObj.transform.SetParent(camMgrObj.transform, false);
        CinemachineCamera vcamDeath = vcamDeathObj.AddComponent<CinemachineCamera>();
        vcamDeath.Priority = 10;
        vcamDeathObj.transform.position = new Vector3(0f, 3f, -4f);
        var aimDeath = vcamDeathObj.AddComponent<CinemachineRotationComposer>();
        aimDeath.TargetOffset = Vector3.zero;

        // Gán references vào CameraManager qua SerializedObject
        SerializedObject soCamMgr = new SerializedObject(camMgr);
        soCamMgr.FindProperty("vcam3rdPerson").objectReferenceValue = vcam3rd;
        soCamMgr.FindProperty("vcam1stPerson").objectReferenceValue = vcam1st;
        soCamMgr.FindProperty("vcamDeath").objectReferenceValue = vcamDeath;
        soCamMgr.ApplyModifiedProperties();

        // Global_PostProcessing_Volume
        GameObject ppObj = new GameObject("Global_PostProcessing_Volume");
        ppObj.transform.SetParent(rootManagers.transform, false);
        Volume volume = ppObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/Settings/GlobalArenaVolumeProfile.asset");
        }
        if (!profile.Has<Bloom>())
        {
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(0.9f);
        }
        volume.profile = profile;
        PostProcessingController ppCtrl = ppObj.AddComponent<PostProcessingController>();
        SerializedObject soPP = new SerializedObject(ppCtrl);
        soPP.FindProperty("postProcessVolume").objectReferenceValue = volume;
        soPP.ApplyModifiedProperties();

        // ObjectPoolManager
        GameObject poolObj = new GameObject("ObjectPoolManager");
        poolObj.transform.SetParent(rootManagers.transform, false);
        ObjectPoolManager poolMgr = poolObj.AddComponent<ObjectPoolManager>();
        SetupObjectPoolManagerConfigs(poolMgr);

        // ==========================================
        // B. NHÓM PLAYER & CHARACTERS
        // ==========================================
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "Player_Hero";
        playerObj.tag = "Player";
        playerObj.transform.SetParent(rootPlayer.transform, false);
        playerObj.transform.position = new Vector3(0f, 1f, 0f);
        playerObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Player", new Color(0.1f, 0.6f, 1f));

        Collider defCol = playerObj.GetComponent<Collider>();
        if (defCol != null) Object.DestroyImmediate(defCol);

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0f, 0f);
        cc.height = 2f;
        cc.radius = 0.5f;

        // MuzzlePoint
        GameObject muzzleObj = new GameObject("MuzzlePoint");
        muzzleObj.transform.SetParent(playerObj.transform, false);
        muzzleObj.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);

        // DeathCam_Target
        GameObject deathTargetObj = new GameObject("DeathCam_Target");
        deathTargetObj.transform.SetParent(playerObj.transform, false);
        deathTargetObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();
        SerializedObject soPlayer = new SerializedObject(playerCtrl);
        soPlayer.FindProperty("muzzlePoint").objectReferenceValue = muzzleObj.transform;
        soPlayer.FindProperty("deathCamTarget").objectReferenceValue = deathTargetObj.transform;
        soPlayer.FindProperty("_cam").objectReferenceValue = mainCamObj.transform;
        soPlayer.ApplyModifiedProperties();

        // Gán targets cho CameraManager
        camMgr.SetTargets(playerObj.transform, muzzleObj.transform, deathTargetObj.transform);

        // Enemies Group
        GameObject enemiesGroup = new GameObject("Enemies");
        enemiesGroup.transform.SetParent(rootPlayer.transform, false);

        // Enemy_Patrol_FSM
        GameObject enemyPatrolObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyPatrolObj.name = "Enemy_Patrol_FSM";
        enemyPatrolObj.tag = "Enemy";
        enemyPatrolObj.transform.SetParent(enemiesGroup.transform, false);
        enemyPatrolObj.transform.position = new Vector3(-10f, 1f, 10f);
        enemyPatrolObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_EnemyPatrol", new Color(0.9f, 0.2f, 0.2f));
        NavMeshAgent agent = enemyPatrolObj.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        EnemyPatrolFSM enemyPatrolFSM = enemyPatrolObj.AddComponent<EnemyPatrolFSM>();

        // Enemy_Stationary_BT
        GameObject enemyStatObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        enemyStatObj.name = "Enemy_Stationary_BT";
        enemyStatObj.tag = "Enemy";
        enemyStatObj.transform.SetParent(enemiesGroup.transform, false);
        enemyStatObj.transform.position = new Vector3(12f, 1f, 0f);
        enemyStatObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_EnemyStationary", new Color(0.8f, 0.4f, 0.1f));

        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(enemyStatObj.transform, false);
        firePointObj.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);

        EnemyTurretBT enemyTurretBT = enemyStatObj.AddComponent<EnemyTurretBT>();
        SerializedObject soTurret = new SerializedObject(enemyTurretBT);
        soTurret.FindProperty("attackRange").floatValue = 10.0f; // Đúng 10 đơn vị theo Câu 2
        soTurret.FindProperty("idleRotateSpeed").floatValue = 45.0f; // Xoay tròn tại chỗ
        soTurret.FindProperty("firePoint").objectReferenceValue = firePointObj.transform;
        soTurret.FindProperty("bulletPoolKey").stringValue = "EnemyBullet";
        soTurret.ApplyModifiedProperties();

        // ==========================================
        // C. NHÓM ENVIRONMENT & TRIGGERS
        // ==========================================
        GameObject envGroup = new GameObject("Environment");
        envGroup.transform.SetParent(rootEnv.transform, false);

        // Ground
        GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundObj.name = "Ground";
        groundObj.transform.SetParent(envGroup.transform, false);
        groundObj.transform.position = new Vector3(0f, -0.5f, 0f);
        groundObj.transform.localScale = new Vector3(40f, 1f, 40f);
        groundObj.tag = "Ground";
        groundObj.layer = LayerMask.NameToLayer("Ground");
        groundObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Ground", new Color(0.2f, 0.22f, 0.25f));

        // NavMeshSurface trên sàn đấu
        NavMeshSurface navSurface = groundObj.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.All;
        navSurface.BuildNavMesh();

        // Walls
        GameObject wallsGroup = new GameObject("Walls");
        wallsGroup.transform.SetParent(envGroup.transform, false);
        wallsGroup.tag = "Wall";
        wallsGroup.layer = LayerMask.NameToLayer("Wall");

        CreateWall(wallsGroup.transform, "Wall_North", new Vector3(0f, 2f, 20f), new Vector3(40f, 4f, 1f));
        CreateWall(wallsGroup.transform, "Wall_South", new Vector3(0f, 2f, -20f), new Vector3(40f, 4f, 1f));
        CreateWall(wallsGroup.transform, "Wall_East", new Vector3(20f, 2f, 0f), new Vector3(1f, 4f, 40f));
        CreateWall(wallsGroup.transform, "Wall_West", new Vector3(-20f, 2f, 0f), new Vector3(1f, 4f, 40f));

        // Waypoints
        GameObject wpGroup = new GameObject("Waypoints");
        wpGroup.transform.SetParent(rootEnv.transform, false);
        GameObject wpA = CreateWaypoint("Waypoint_A", new Vector3(-12f, 0.5f, 12f), wpGroup.transform);
        GameObject wpB = CreateWaypoint("Waypoint_B", new Vector3(12f, 0.5f, 12f), wpGroup.transform);
        GameObject wpC = CreateWaypoint("Waypoint_C", new Vector3(0f, 0.5f, -12f), wpGroup.transform);

        // Gán trực tiếp 3 Waypoints vào Enemy_Patrol_FSM trên Inspector
        SerializedObject soPatrol = new SerializedObject(enemyPatrolFSM);
        SerializedProperty wpList = soPatrol.FindProperty("patrolWaypoints");
        wpList.ClearArray();
        wpList.InsertArrayElementAtIndex(0);
        wpList.GetArrayElementAtIndex(0).objectReferenceValue = wpA.transform;
        wpList.InsertArrayElementAtIndex(1);
        wpList.GetArrayElementAtIndex(1).objectReferenceValue = wpB.transform;
        wpList.InsertArrayElementAtIndex(2);
        wpList.GetArrayElementAtIndex(2).objectReferenceValue = wpC.transform;
        soPatrol.ApplyModifiedProperties();

        // Targets
        GameObject targetGroup = new GameObject("Targets");
        targetGroup.transform.SetParent(rootEnv.transform, false);
        GameObject targetCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetCube.name = "Target_Cube_Far";
        targetCube.transform.SetParent(targetGroup.transform, false);
        targetCube.transform.position = new Vector3(0f, 5f, 35f);
        targetCube.transform.localScale = new Vector3(3f, 3f, 3f);
        targetCube.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TargetFar", new Color(0.9f, 0.1f, 0.9f));

        // Triggers
        GameObject triggersGroup = new GameObject("Triggers");
        triggersGroup.transform.SetParent(rootEnv.transform, false);

        // Trigger_GroundCam
        GameObject trigGround = new GameObject("Trigger_GroundCam");
        trigGround.transform.SetParent(triggersGroup.transform, false);
        trigGround.transform.position = new Vector3(-5f, 1f, -5f);
        BoxCollider boxGround = trigGround.AddComponent<BoxCollider>();
        boxGround.isTrigger = true;
        boxGround.size = new Vector3(6f, 3f, 6f);
        GroundCamTrigger groundCamTrigger = trigGround.AddComponent<GroundCamTrigger>();
        SerializedObject soGroundTrig = new SerializedObject(groundCamTrigger);
        soGroundTrig.FindProperty("customGroundVcam").objectReferenceValue = vcam1st;
        soGroundTrig.ApplyModifiedProperties();

        // Trigger_BloomZone
        GameObject trigBloom = new GameObject("Trigger_BloomZone");
        trigBloom.transform.SetParent(triggersGroup.transform, false);
        trigBloom.transform.position = new Vector3(5f, 1f, 5f);
        BoxCollider boxBloom = trigBloom.AddComponent<BoxCollider>();
        boxBloom.isTrigger = true;
        boxBloom.size = new Vector3(6f, 3f, 6f);
        trigBloom.AddComponent<BloomZoneTrigger>();

        // SpawnPoints
        GameObject spawnGroup = new GameObject("SpawnPoints");
        spawnGroup.transform.SetParent(rootEnv.transform, false);
        CreateSpawnPoint("Point_SpherePool", new Vector3(0f, 1f, 14f), spawnGroup.transform);
        CreateSpawnPoint("Point_EnemySpawner", new Vector3(-14f, 1f, 0f), spawnGroup.transform);

        // ==========================================
        // D. NHÓM UI CANVAS & VFX ANCHORS
        // ==========================================
        BuildUICanvas(playerCtrl, rootUI.transform);

        GameObject vfxGroup = new GameObject("VFX_Anchors");
        vfxGroup.transform.SetParent(rootUI.transform, false);
        CreateVFXFireAnchor("VFX_Fire_Instance", new Vector3(-18f, 1f, 18f), vfxGroup.transform);
        CreateVFXSubEmitterAnchor("VFX_SubEmitter_Instance", new Vector3(18f, 1f, 18f), vfxGroup.transform);

        // LƯU SCENE
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"[GAM301 BUILDER] Scene với cấu trúc 4 Nhóm Hierarchy chuẩn đã lưu thành công tại: {SCENE_PATH}");
    }

    private static GameObject CreateSectionRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }

    private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.tag = "Wall";
        wall.layer = LayerMask.NameToLayer("Wall");
        wall.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Wall", new Color(0.12f, 0.14f, 0.16f));
    }

    private static GameObject CreateWaypoint(string name, Vector3 pos, Transform parent)
    {
        GameObject wp = new GameObject(name);
        wp.transform.SetParent(parent);
        wp.transform.position = pos;
        return wp;
    }

    private static GameObject CreateSpawnPoint(string name, Vector3 pos, Transform parent)
    {
        GameObject sp = new GameObject(name);
        sp.transform.SetParent(parent);
        sp.transform.position = pos;
        return sp;
    }

    private static void CreateVFXFireAnchor(string name, Vector3 pos, Transform parent)
    {
        GameObject vfx = new GameObject(name);
        vfx.transform.SetParent(parent, false);
        vfx.transform.position = pos;

        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 2.5f;
        main.startSize = 0.6f;
        main.startColor = new Color(1f, 0.4f, 0.05f, 0.9f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.25f;

        var emission = ps.emission;
        emission.rateOverTime = 25f;
    }

    private static void CreateVFXSubEmitterAnchor(string name, Vector3 pos, Transform parent)
    {
        GameObject vfx = new GameObject(name);
        vfx.transform.SetParent(parent, false);
        vfx.transform.position = pos;

        ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = 1.8f;
        main.startSpeed = 3.5f;
        main.startSize = 0.4f;
        main.startColor = new Color(1f, 0.9f, 0.2f, 0.9f);

        // Hạt bay chếch về bên phải theo mô tả GDD
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.rotation = new Vector3(0f, 45f, 0f);

        // Tạo Particle System hạt con (Sub-Emitter)
        GameObject subObj = new GameObject("SubEmitter_Burst");
        subObj.transform.SetParent(vfx.transform, false);
        ParticleSystem subPs = subObj.AddComponent<ParticleSystem>();
        var subMain = subPs.main;
        subMain.duration = 0.5f;
        subMain.loop = false;
        subMain.startLifetime = 0.4f;
        subMain.startSpeed = 6f;
        subMain.startSize = 0.15f;
        subMain.startColor = new Color(1f, 0.6f, 0.1f, 0.8f);

        var subEmission = subPs.emission;
        subEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var subEmitters = ps.subEmitters;
        subEmitters.enabled = true;
        subEmitters.AddSubEmitter(subPs, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
    }

    private static void BuildUICanvas(PlayerController playerCtrl, Transform parent)
    {
        GameObject canvasObj = new GameObject("UI_Canvas");
        canvasObj.transform.SetParent(parent, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.transform.SetParent(parent, false);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // HealthBar_Slider
        GameObject sliderObj = new GameObject("HealthBar_Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform rtSlider = sliderObj.GetComponent<RectTransform>();
        rtSlider.anchorMin = new Vector2(0f, 1f);
        rtSlider.anchorMax = new Vector2(0f, 1f);
        rtSlider.pivot = new Vector2(0f, 1f);
        rtSlider.anchoredPosition = new Vector2(30f, -30f);
        rtSlider.sizeDelta = new Vector2(300f, 30f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform rtFillArea = fillArea.AddComponent<RectTransform>();
        rtFillArea.anchorMin = Vector2.zero;
        rtFillArea.anchorMax = Vector2.one;
        rtFillArea.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.3f);
        RectTransform rtFill = fillObj.GetComponent<RectTransform>();
        rtFill.anchorMin = Vector2.zero;
        rtFill.anchorMax = Vector2.one;
        rtFill.sizeDelta = Vector2.zero;

        slider.targetGraphic = bgImg;
        slider.fillRect = rtFill;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        // Image_DamageFlash
        GameObject flashObj = new GameObject("Image_DamageFlash");
        flashObj.transform.SetParent(canvasObj.transform, false);
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1f, 0f, 0f, 0f); // Mặc định trong suốt
        flashImg.raycastTarget = false;
        RectTransform rtFlash = flashObj.GetComponent<RectTransform>();
        rtFlash.anchorMin = Vector2.zero;
        rtFlash.anchorMax = Vector2.one;
        rtFlash.sizeDelta = Vector2.zero;

        // Panel_ScreenBlur
        GameObject blurObj = new GameObject("Panel_ScreenBlur");
        blurObj.transform.SetParent(canvasObj.transform, false);
        Image blurImg = blurObj.AddComponent<Image>();
        blurImg.color = new Color(0f, 0f, 0f, 0f);
        blurImg.raycastTarget = false;
        RectTransform rtBlur = blurObj.GetComponent<RectTransform>();
        rtBlur.anchorMin = Vector2.zero;
        rtBlur.anchorMax = Vector2.one;
        rtBlur.sizeDelta = Vector2.zero;

        // Kết nối reference vào PlayerController
        if (playerCtrl != null)
        {
            SerializedObject soPlayer = new SerializedObject(playerCtrl);
            soPlayer.FindProperty("healthBarSlider").objectReferenceValue = slider;
            soPlayer.FindProperty("damageFlashImage").objectReferenceValue = flashImg;
            soPlayer.FindProperty("screenBlurImage").objectReferenceValue = blurImg;
            soPlayer.ApplyModifiedProperties();
        }
    }

    private static void SetupObjectPoolManagerConfigs(ObjectPoolManager poolMgr)
    {
        SerializedObject so = new SerializedObject(poolMgr);
        SerializedProperty listProp = so.FindProperty("poolConfigurations");
        listProp.ClearArray();

        AddPoolConfig(listProp, "PlayerBullet", $"{PREFAB_DIR}/PFB_PlayerBullet.prefab", 15);
        AddPoolConfig(listProp, "EnemyBullet", $"{PREFAB_DIR}/PFB_EnemyBullet.prefab", 15);
        AddPoolConfig(listProp, "SphereProjectile", $"{PREFAB_DIR}/PFB_SphereProjectile.prefab", 10);
        AddPoolConfig(listProp, "EnemyPatrol", $"{PREFAB_DIR}/PFB_Enemy_Patrol.prefab", 5);
        AddPoolConfig(listProp, "VFX_Explosion", $"{PREFAB_DIR}/PFB_VFX_Explosion.prefab", 10);
        AddPoolConfig(listProp, "VFX_Spark", $"{PREFAB_DIR}/PFB_VFX_Spark.prefab", 15);

        so.ApplyModifiedProperties();
    }

    private static void AddPoolConfig(SerializedProperty listProp, string key, string prefabPath, int initialSize)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        int index = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(index);
        SerializedProperty elem = listProp.GetArrayElementAtIndex(index);
        elem.FindPropertyRelative("poolKey").stringValue = key;
        elem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        elem.FindPropertyRelative("initialSize").intValue = initialSize;
        elem.FindPropertyRelative("expandable").boolValue = true;
    }
}
