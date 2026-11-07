#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace ProjectBase.Editor
{
    public static class CharacterReplacer
    {
        [MenuItem("Tools/Replace Characters and Recolor")]
        public static void ExecuteReplacement()
        {
            Debug.Log("[CharacterReplacer] Starting Character replacement and recoloring...");

            // ==========================================
            // 1. THAY PBRCharacter THÀNH Player_Hero
            // ==========================================
            GameObject playerHero = GameObject.Find("Player_Hero");
            if (playerHero == null)
            {
                Debug.LogError("[CharacterReplacer] Không tìm thấy GameObject 'Player_Hero' trong Scene!");
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(playerHero, "Replace Player_Hero Visual");

                // Tìm PBRCharacter trong Scene (hoặc load từ Prefab)
                GameObject pbrChar = GameObject.Find("PBRCharacter");
                if (pbrChar == null)
                {
                    var pbrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Import/SciFiWarriorPBRHPPolyart/Prefabs/PBRCharacter.prefab");
                    if (pbrPrefab != null)
                    {
                        pbrChar = (GameObject)PrefabUtility.InstantiatePrefab(pbrPrefab);
                    }
                }

                if (pbrChar != null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(pbrChar, "Parent PBRCharacter to Player_Hero");

                    // Tắt hoặc xóa MeshRenderer & MeshFilter primitive Capsule cũ trên Player_Hero
                    var mr = playerHero.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                    var mf = playerHero.GetComponent<MeshFilter>();
                    if (mf != null) mf.sharedMesh = null;

                    // Gán PBRCharacter làm model con của Player_Hero
                    pbrChar.name = "PBRCharacter";
                    pbrChar.transform.SetParent(playerHero.transform, false);
                    pbrChar.transform.localPosition = new Vector3(0f, -1f, 0f); // Đặt chân chạm đất
                    pbrChar.transform.localRotation = Quaternion.identity;
                    pbrChar.transform.localScale = Vector3.one;

                    // Đảm bảo Animator hoạt động tốt
                    var anim = pbrChar.GetComponent<Animator>();
                    if (anim != null && anim.runtimeAnimatorController == null)
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                            "Assets/Import/SciFiWarriorPBRHPPolyart/Animators/SciFiWarrior.controller");
                        if (controller != null) anim.runtimeAnimatorController = controller;
                    }

                    // Tinh chỉnh MuzzlePoint và DeathCam_Target
                    Transform muzzle = playerHero.transform.Find("MuzzlePoint");
                    if (muzzle != null)
                    {
                        muzzle.localPosition = new Vector3(0.2f, 0.4f, 0.8f);
                    }

                    Transform deathCam = playerHero.transform.Find("DeathCam_Target");
                    if (deathCam != null)
                    {
                        deathCam.localPosition = new Vector3(0f, 0.5f, 0f);
                    }

                    EditorUtility.SetDirty(playerHero);
                    Debug.Log("[CharacterReplacer] Đã thay thế thành công PBRCharacter thành model của Player_Hero!");
                }
                else
                {
                    Debug.LogError("[CharacterReplacer] Không tìm thấy PBRCharacter trong Scene hoặc Prefab!");
                }
            }

            // ==========================================
            // 2. THAY P_ISO_Mech THÀNH Enemy_Stationary_BT
            // ==========================================
            GameObject enemyStationary = GameObject.Find("Enemy_Stationary_BT");
            if (enemyStationary == null)
            {
                Debug.LogError("[CharacterReplacer] Không tìm thấy GameObject 'Enemy_Stationary_BT' trong Scene!");
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(enemyStationary, "Replace Enemy_Stationary_BT Visual");

                // Tìm P_ISO_Mech trong Scene (hoặc load từ Prefab)
                GameObject mech = GameObject.Find("P_ISO_Mech");
                if (mech == null)
                {
                    var mechPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Prefabs/Mech/P_ISO_Mech.prefab");
                    if (mechPrefab != null)
                    {
                        mech = (GameObject)PrefabUtility.InstantiatePrefab(mechPrefab);
                    }
                }

                if (mech != null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(mech, "Parent P_ISO_Mech to Enemy_Stationary_BT");

                    // Tắt hoặc xóa MeshRenderer & MeshFilter primitive Capsule cũ trên Enemy_Stationary_BT
                    var mr = enemyStationary.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                    var mf = enemyStationary.GetComponent<MeshFilter>();
                    if (mf != null) mf.sharedMesh = null;

                    // Giữ scale ~0.346 như sếp đã tinh chỉnh
                    Vector3 mechScale = new Vector3(0.345958143f, 0.345958143f, 0.345958143f);

                    mech.name = "P_ISO_Mech";
                    mech.transform.SetParent(enemyStationary.transform, false);
                    mech.transform.localPosition = new Vector3(0f, -1f, 0f); // Đặt chân chạm đất
                    mech.transform.localRotation = Quaternion.identity;
                    mech.transform.localScale = mechScale;

                    // Thêm Animator nếu chưa có để Mech chơi animation Idle
                    var anim = mech.GetComponent<Animator>();
                    if (anim == null) anim = mech.AddComponent<Animator>();
                    if (anim.runtimeAnimatorController == null)
                    {
                        var mechController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                            "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Animations/Mech/AC_Mech_Demo.controller");
                        if (mechController != null) anim.runtimeAnimatorController = mechController;
                    }

                    // Tinh chỉnh CapsuleCollider cho vừa vặn với kích thước Mech
                    var col = enemyStationary.GetComponent<CapsuleCollider>();
                    if (col != null)
                    {
                        col.radius = 0.8f;
                        col.height = 2.0f;
                        col.center = new Vector3(0f, 0f, 0f);
                    }

                    // Tinh chỉnh FirePoint ở nòng súng Mech
                    Transform firePoint = enemyStationary.transform.Find("FirePoint");
                    if (firePoint != null)
                    {
                        firePoint.localPosition = new Vector3(0f, 0.6f, 1.2f);
                    }

                    EditorUtility.SetDirty(enemyStationary);
                    Debug.Log("[CharacterReplacer] Đã thay thế thành công P_ISO_Mech thành model của Enemy_Stationary_BT!");
                }
                else
                {
                    Debug.LogError("[CharacterReplacer] Không tìm thấy P_ISO_Mech trong Scene hoặc Prefab!");
                }
            }

            // ==========================================
            // 3. ĐỔI MÀU THÀNH XANH CHO Enemy_Patrol_FSM
            // ==========================================
            Material matPatrol = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EnemyPatrol.mat");
            if (matPatrol != null)
            {
                Undo.RecordObject(matPatrol, "Change Enemy_Patrol Color to Green");
                Color neonGreen = new Color(0.12f, 0.85f, 0.35f, 1.0f); // Xanh lá neon #22C55E nổi bật
                matPatrol.SetColor("_BaseColor", neonGreen);
                matPatrol.SetColor("_Color", neonGreen);
                EditorUtility.SetDirty(matPatrol);
                Debug.Log("[CharacterReplacer] Đã đổi màu Mat_EnemyPatrol sang xanh lá neon (#22C55E)!");
            }
            else
            {
                Debug.LogError("[CharacterReplacer] Không tìm thấy Material 'Assets/Materials/Mat_EnemyPatrol.mat'!");
            }

            // Kiểm tra renderers trên Enemy_Patrol_FSM
            GameObject enemyPatrol = GameObject.Find("Enemy_Patrol_FSM");
            if (enemyPatrol != null)
            {
                var mr = enemyPatrol.GetComponent<MeshRenderer>();
                if (mr != null && matPatrol != null)
                {
                    Undo.RecordObject(mr, "Set Patrol Material");
                    mr.sharedMaterial = matPatrol;
                    EditorUtility.SetDirty(mr);
                }
            }

            // Lưu Scene và Assets
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[CharacterReplacer] Hoàn tất toàn bộ công đoạn và đã lưu Scene an toàn!");
        }
    }
}
#endif
