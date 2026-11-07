#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

namespace ProjectBase.Editor
{
    [InitializeOnLoad]
    public static class ExamFourTasksAutoBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/MainArena_GAM301_Base.unity";

        [MenuItem("Tools/GAM301/Build 4 Exam Tasks Into Scene")]
        public static void BuildAndSetupExamTasks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("<color=cyan>=== BẮT ĐẦU TỰ ĐỘNG THIẾT LẬP 4 CÂU ĐỀ THI GAM301 VÀO SCENE ===</color>");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                if (System.IO.File.Exists(SCENE_PATH))
                {
                    activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[ExamBuilder] Không tìm thấy Scene tại: {SCENE_PATH}");
                    return;
                }
            }

            // Đảm bảo có Tag "Ground" trong hệ thống theo quy tắc WEAT
            EnsureGroundTagExists();

            // Nhóm quản lý chính cho 4 câu thi
            GameObject examGroup = GameObject.Find("-- EXAM GAM301 TASKS --");
            if (examGroup == null)
            {
                examGroup = new GameObject("-- EXAM GAM301 TASKS --");
                examGroup.transform.position = Vector3.zero;
            }

            // 1. SETUP CÂU 1: COROUTINE DI CHUYỂN A -> B -> QUAY 90° -> FIRE
            SetupQuestion1(examGroup.transform);

            // 2. SETUP CÂU 2: TRIGGER X -> CAMERA BAY TỚI CUBE (ĐẶT XA) -> 2S BAY VỀ
            SetupQuestion2(examGroup.transform);

            // 3. SETUP CÂU 3: BẤM PHÍM H BẬT/TẮT TRẮNG ĐEN + FILM GRAIN + VIGNETTE
            SetupQuestion3(examGroup.transform);

            // 4. SETUP CÂU 4: OBJECT POOLING BẮN QUẢ CẦU TỪ VỊ TRÍ X -> RƠI XUỐNG ĐẤT BIẾN MẤT
            SetupQuestion4(examGroup.transform);

            // Lưu Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green><b>=== TỰ ĐỘNG THIẾT LẬP TOÀN BỘ 4 CÂU ĐỀ THI GAM301 THÀNH CÔNG RỰC RỠ! ===</b></color>");
        }

        /// <summary>
        /// Bơm Tag Ground vào TagManager nếu chưa có (Quy tắc độc tài WEAT).
        /// </summary>
        private static void EnsureGroundTagExists()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            bool exists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("Ground"))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Ground";
                tagManager.ApplyModifiedProperties();
                Debug.Log("[ExamBuilder] Đã bơm Tag 'Ground' vào TagManager.asset.");
            }
        }

        /// <summary>
        /// 1. CÂU 1: Gán cho con Enemy_Patrol_FSM thực hiện Coroutine: A -> B -> Chờ 1s -> Quay 90° -> Chờ 1s -> "Fire"
        /// </summary>
        private static void SetupQuestion1(Transform parent)
        {
            // Dọn dẹp group Mover tạm trước đó nếu có để tránh dư thừa đối tượng
            GameObject oldMover = GameObject.Find("Exam_Q1_CoroutineMover_Group");
            if (oldMover != null)
            {
                Object.DestroyImmediate(oldMover);
            }

            // Tìm con Enemy_Patrol_FSM trong Scene
            GameObject enemyGo = GameObject.Find("Enemy_Patrol_FSM");
            if (enemyGo != null)
            {
                EnemyPatrolFSM enemyFSM = enemyGo.GetComponent<EnemyPatrolFSM>();
                if (enemyFSM != null)
                {
                    Transform wpA = GameObject.Find("Waypoint_A")?.transform;
                    Transform wpB = GameObject.Find("Waypoint_B")?.transform;

                    SerializedObject soEnemy = new SerializedObject(enemyFSM);
                    soEnemy.FindProperty("useExamCoroutineMode").boolValue = true;
                    soEnemy.FindProperty("examPointA").objectReferenceValue = wpA;
                    soEnemy.FindProperty("examPointB").objectReferenceValue = wpB;
                    soEnemy.FindProperty("examRotateAngle").floatValue = 90.0f;
                    soEnemy.FindProperty("loopExamSequence").boolValue = true;
                    soEnemy.ApplyModifiedProperties();

                    Debug.Log("<color=green>[ExamBuilder] ĐÃ GÁN THÀNH CÔNG CÂU 1 CHO ENEMY_PATROL_FSM (Waypoint_A -> Waypoint_B -> 90° -> Fire)!</color>");
                }
            }
            else
            {
                Debug.LogWarning("[ExamBuilder] Không tìm thấy Enemy_Patrol_FSM trong Scene!");
            }
        }

        /// <summary>
        /// 2. CÂU 2: Player tới vị trí X -> Camera bay tới Cube ở xa -> 2s bay về lại Player
        /// </summary>
        private static void SetupQuestion2(Transform parent)
        {
            GameObject q2Root = GameObject.Find("Exam_Q2_CameraFly_Group");
            if (q2Root == null)
            {
                q2Root = new GameObject("Exam_Q2_CameraFly_Group");
                q2Root.transform.SetParent(parent);
                q2Root.transform.position = Vector3.zero;
            }

            // Đối tượng Cube đặt ở xa nhân vật
            Transform targetCube = q2Root.transform.Find("Exam_Far_Target_Cube");
            if (targetCube == null)
            {
                GameObject cubeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeGo.name = "Exam_Far_Target_Cube";
                cubeGo.transform.SetParent(q2Root.transform);
                cubeGo.transform.position = new Vector3(0f, 6.0f, 22.0f); // Đặt trên cao và xa phía sau đấu trường
                cubeGo.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

                // Gán màu Cyan Neon phát quang rực rỡ
                Renderer rend = cubeGo.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0f, 0.95f, 1.0f);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(0f, 0.95f, 1.0f) * 2.0f);
                    }
                    rend.sharedMaterial = mat;
                }
                targetCube = cubeGo.transform;
            }

            // Cinemachine Camera ngắm tới Cube
            Transform vcamGo = q2Root.transform.Find("CM_Exam_CubeFlyCam");
            CinemachineVirtualCameraBase flyVcam = null;
            if (vcamGo == null)
            {
                GameObject cGo = new GameObject("CM_Exam_CubeFlyCam");
                cGo.transform.SetParent(q2Root.transform);
                cGo.transform.position = new Vector3(0f, 7.5f, 15.0f);
                cGo.transform.LookAt(targetCube.position);

                var cmCam = cGo.AddComponent<CinemachineCamera>();
                cmCam.Priority = 0; // Mặc định thấp
                cmCam.Target.LookAtTarget = targetCube;
                flyVcam = cmCam;
            }
            else
            {
                flyVcam = vcamGo.GetComponent<CinemachineVirtualCameraBase>();
            }

            // Vùng Trigger X: Đặt tại vị trí (0, 1, 4) - ngay trước mặt Player khi bắt đầu chạy
            Transform triggerX = q2Root.transform.Find("Exam_Trigger_ZoneX");
            if (triggerX == null)
            {
                GameObject tGo = new GameObject("Exam_Trigger_ZoneX");
                tGo.transform.SetParent(q2Root.transform);
                tGo.transform.position = new Vector3(0f, 1.0f, 4.0f);

                BoxCollider box = tGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(6.0f, 2.5f, 4.0f);

                triggerX = tGo.transform;
            }

            ExamCameraFlyTrigger flyTrigger = triggerX.GetComponent<ExamCameraFlyTrigger>();
            if (flyTrigger == null) flyTrigger = triggerX.gameObject.AddComponent<ExamCameraFlyTrigger>();

            SerializedObject soTrigger = new SerializedObject(flyTrigger);
            soTrigger.FindProperty("cubeTarget").objectReferenceValue = targetCube;
            soTrigger.FindProperty("cubeFlyVcam").objectReferenceValue = flyVcam;
            soTrigger.FindProperty("holdDurationAtCube").floatValue = 2.0f; // Đúng 2 giây theo đề thi
            soTrigger.FindProperty("flyPriority").intValue = 99;
            soTrigger.FindProperty("returnPriority").intValue = 0;
            soTrigger.ApplyModifiedProperties();

            Debug.Log("[ExamBuilder] Đã cấu hình Câu 2: ExamCameraFlyTrigger (Trigger X -> Cube xa -> 2s về Player)");
        }

        /// <summary>
        /// 3. CÂU 3: Bấm phím H bật/tắt: Trắng đen + Film Grain + Vignette
        /// </summary>
        private static void SetupQuestion3(Transform parent)
        {
            GameObject q3Go = GameObject.Find("Exam_Q3_PostProcessing_ToggleH");
            if (q3Go == null)
            {
                q3Go = new GameObject("Exam_Q3_PostProcessing_ToggleH");
                q3Go.transform.SetParent(parent);
                q3Go.transform.position = Vector3.zero;
            }

            ExamPostProcessingToggle toggleComp = q3Go.GetComponent<ExamPostProcessingToggle>();
            if (toggleComp == null) toggleComp = q3Go.AddComponent<ExamPostProcessingToggle>();

            Debug.Log("[ExamBuilder] Đã cấu hình Câu 3: ExamPostProcessingToggle (Phím H -> Bật/Tắt Trắng đen + Grain + Vignette)");
        }

        /// <summary>
        /// 4. CÂU 4: Pooling Object tại vị trí X bắn quả cầu lên ngẫu nhiên, rơi xuống đất biến mất
        /// </summary>
        private static void SetupQuestion4(Transform parent)
        {
            GameObject q4Go = GameObject.Find("Exam_Q4_SpherePoolingLauncher_X");
            if (q4Go == null)
            {
                q4Go = new GameObject("Exam_Q4_SpherePoolingLauncher_X");
                q4Go.transform.SetParent(parent);
                q4Go.transform.position = new Vector3(12.0f, 0.5f, -6.0f); // Vị trí X tự chọn bên cánh phải sàn đấu

                // Bệ phóng trực quan (Pad hình trụ)
                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "Launcher_Pad";
                pad.transform.SetParent(q4Go.transform);
                pad.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                pad.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
                var col = pad.GetComponent<Collider>();
                if (col) Object.DestroyImmediate(col);
            }

            ExamSpherePoolingSpawner spawnerComp = q4Go.GetComponent<ExamSpherePoolingSpawner>();
            if (spawnerComp == null) spawnerComp = q4Go.AddComponent<ExamSpherePoolingSpawner>();

            SerializedObject soSpawner = new SerializedObject(spawnerComp);
            soSpawner.FindProperty("autoStart").boolValue = true;
            soSpawner.FindProperty("launchInterval").floatValue = 0.5f; // Bắn liên tục mỗi 0.5s
            soSpawner.FindProperty("launchForceMin").floatValue = 7.0f;
            soSpawner.FindProperty("launchForceMax").floatValue = 11.0f;
            soSpawner.FindProperty("initialPoolSize").intValue = 25;
            soSpawner.ApplyModifiedProperties();

            // Đảm bảo Ground trong Environment có Tag Ground
            GameObject ground = GameObject.Find("Ground");
            if (ground != null && !ground.CompareTag("Ground"))
            {
                ground.tag = "Ground";
            }

            Debug.Log("[ExamBuilder] Đã cấu hình Câu 4: ExamSpherePoolingSpawner (Object Pooling bắn cầu liên tục, rơi xuống đất biến mất)");
        }
    }
}
#endif
