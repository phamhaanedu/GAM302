#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBase.Editor
{
    [InitializeOnLoad]
    public static class ExamPreparationSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/MainArena_GAM301_Base.unity";
        private const string PLAYER_BULLET_PATH = "Assets/Prefabs/PFB_PlayerBullet.prefab";
        private const string ENEMY_BULLET_PATH = "Assets/Prefabs/PFB_EnemyBullet.prefab";

        static ExamPreparationSetup()
        {
            EditorApplication.delayCall += ExecuteSetup;
        }

        [MenuItem("Tools/Setup Exam 4 Requirements")]
        public static void ExecuteSetup()
        {
            SetupBulletPrefabs();
            SetupSceneComponents();
        }

        private static void SetupBulletPrefabs()
        {
            // 1. CÂU 3: Cấu hình TrailRenderer cho PFB_PlayerBullet
            GameObject playerBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_BULLET_PATH);
            if (playerBulletPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(playerBulletPrefab);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);

                TrailRenderer trail = instance.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = instance.AddComponent<TrailRenderer>();
                }

                trail.time = 0.35f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.02f;

                Renderer rend = instance.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    trail.material = rend.sharedMaterial;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);
                Debug.Log("[ExamSetup] Đã bổ sung TrailRenderer cho PFB_PlayerBullet.prefab (Câu 3)");
            }

            // 2. CÂU 3: Đảm bảo TrailRenderer cho PFB_EnemyBullet
            GameObject enemyBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_BULLET_PATH);
            if (enemyBulletPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(enemyBulletPrefab);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);

                TrailRenderer trail = instance.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = instance.AddComponent<TrailRenderer>();
                }

                trail.time = 0.35f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.02f;

                Renderer rend = instance.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    trail.material = rend.sharedMaterial;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);
                Debug.Log("[ExamSetup] Đã cấu hình TrailRenderer cho PFB_EnemyBullet.prefab (Câu 3)");
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetupSceneComponents()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                if (System.IO.File.Exists(SCENE_PATH))
                {
                    activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                }
                else
                {
                    return;
                }
            }

            bool sceneDirty = false;

            // 3. CÂU 4: Kết nối Panel_ScreenBlur vào PlayerController
            PlayerController playerCtrl = Object.FindAnyObjectByType<PlayerController>();
            GameObject blurPanel = GameObject.Find("Panel_ScreenBlur");
            if (playerCtrl != null && blurPanel != null)
            {
                Image blurImg = blurPanel.GetComponent<Image>();
                if (blurImg != null)
                {
                    blurImg.color = new Color(0.08f, 0.08f, 0.12f, 0f);
                    blurImg.raycastTarget = false;

                    SerializedObject soPlayer = new SerializedObject(playerCtrl);
                    SerializedProperty spBlur = soPlayer.FindProperty("screenBlurImage");
                    if (spBlur != null && spBlur.objectReferenceValue != blurImg)
                    {
                        spBlur.objectReferenceValue = blurImg;
                        soPlayer.ApplyModifiedProperties();
                        sceneDirty = true;
                        Debug.Log("[ExamSetup] Đã liên kết Panel_ScreenBlur vào PlayerController (Câu 4)");
                    }
                }
            }

            // 4. CÂU 2: Gắn và cấu hình EnemyTurretBT cho quái trụ trong Scene
            GameObject enemyStationary = GameObject.Find("Enemy_Stationary_BT");
            if (enemyStationary != null)
            {
                EnemyStationaryBT oldBT = enemyStationary.GetComponent<EnemyStationaryBT>();
                if (oldBT != null)
                {
                    oldBT.enabled = false;
                }

                EnemyTurretBT newTurretBT = enemyStationary.GetComponent<EnemyTurretBT>();
                if (newTurretBT == null)
                {
                    newTurretBT = enemyStationary.AddComponent<EnemyTurretBT>();
                }
                newTurretBT.enabled = true;

                // Gán nòng súng FirePoint
                Transform firePoint = enemyStationary.transform.Find("FirePoint");
                if (firePoint == null)
                {
                    GameObject fp = new GameObject("FirePoint");
                    fp.transform.SetParent(enemyStationary.transform, false);
                    fp.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);
                    firePoint = fp.transform;
                }

                SerializedObject soTurret = new SerializedObject(newTurretBT);
                soTurret.FindProperty("attackRange").floatValue = 10.0f; // Đúng 10 đơn vị theo đề bài
                soTurret.FindProperty("idleRotateSpeed").floatValue = 45.0f; // Xoay tròn tại chỗ
                soTurret.FindProperty("firePoint").objectReferenceValue = firePoint;
                soTurret.FindProperty("bulletPoolKey").stringValue = "EnemyBullet";
                soTurret.ApplyModifiedProperties();

                sceneDirty = true;
                Debug.Log("[ExamSetup] Đã cấu hình Behavior Tree Quái EnemyTurretBT (Câu 2)");
            }

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green>[ExamSetup] ĐÃ HOÀN TẤT SETUP TỰ ĐỘNG CẢ 4 CÂU ĐỀ THI VÀO SCENE!</color>");
            }
        }
    }
}
#endif
