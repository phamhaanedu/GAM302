using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using GAM301.UI;

namespace GAM301.EditorTools
{
    public static class HealthBarBuilder
    {
        [MenuItem("Tools/Build WorldSpace HealthBars For Enemies")]
        public static void BuildHealthBars()
        {
            // Build cho Enemy_Patrol_FSM
            var patrolEnemy = GameObject.Find("Enemy_Patrol_FSM");
            if (patrolEnemy != null)
            {
                var fsm = patrolEnemy.GetComponent<EnemyPatrolFSM>();
                float maxHp = fsm != null ? fsm.CurrentHealth : 100f;
                if (maxHp <= 0) maxHp = 100f;
                var hb = AttachHealthBar(patrolEnemy, new Vector3(0f, 3.2f, 0f), maxHp);
                if (fsm != null && hb != null)
                {
                    var so = new SerializedObject(fsm);
                    so.FindProperty("healthBar").objectReferenceValue = hb;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(fsm);
                }
            }

            // Build cho Enemy_Stationary_BT
            var turretEnemy = GameObject.Find("Enemy_Stationary_BT");
            if (turretEnemy != null)
            {
                var bt = turretEnemy.GetComponent<EnemyTurretBT>();
                float maxHp = bt != null ? bt.CurrentHealth : 60f;
                if (maxHp <= 0) maxHp = 60f;
                var hb = AttachHealthBar(turretEnemy, new Vector3(0f, 2.2f, 0f), maxHp);
                if (bt != null && hb != null)
                {
                    var so = new SerializedObject(bt);
                    so.FindProperty("healthBar").objectReferenceValue = hb;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(bt);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[HealthBarBuilder] Successfully built World-Space HealthBars for all enemies!");
        }

        private static WorldSpaceHealthBar AttachHealthBar(GameObject target, Vector3 offset, float maxHealth)
        {
            // Xóa health bar cũ nếu có
            var oldHb = target.transform.Find("HealthBar_WorldSpace");
            if (oldHb != null)
            {
                Undo.DestroyObjectImmediate(oldHb.gameObject);
            }

            // 1. Tạo Canvas World Space Root
            var hbRoot = new GameObject("HealthBar_WorldSpace");
            Undo.RegisterCreatedObjectUndo(hbRoot, "Create HealthBar Root");
            hbRoot.transform.SetParent(target.transform, false);
            hbRoot.transform.localPosition = offset;
            hbRoot.transform.localRotation = Quaternion.identity;
            hbRoot.transform.localScale = Vector3.one * 0.01f;

            var canvas = hbRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rtCanvas = hbRoot.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(160f, 22f);

            var canvasGroup = hbRoot.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var hbComp = hbRoot.AddComponent<WorldSpaceHealthBar>();

            // 2. Viền ngoài (Outer Border)
            var borderObj = new GameObject("Border");
            borderObj.transform.SetParent(hbRoot.transform, false);
            var rtBorder = borderObj.AddComponent<RectTransform>();
            rtBorder.anchorMin = Vector2.zero;
            rtBorder.anchorMax = Vector2.one;
            rtBorder.offsetMin = new Vector2(-2f, -2f);
            rtBorder.offsetMax = new Vector2(2f, 2f);
            var imgBorder = borderObj.AddComponent<Image>();
            imgBorder.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            imgBorder.raycastTarget = false;

            // 3. Nền tối (Background #0F172A)
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hbRoot.transform, false);
            var rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            var imgBg = bgObj.AddComponent<Image>();
            imgBg.color = new Color(0.06f, 0.09f, 0.16f, 0.95f); // #0F172A
            imgBg.raycastTarget = false;

            // 4. Ghost Slider (Thanh máu trễ vàng cam #F59E0B)
            var ghostSlider = CreateSubSlider(hbRoot.transform, "GhostSlider", new Color(0.96f, 0.62f, 0.04f, 0.9f));

            // 5. Main Slider (Thanh máu chính đỏ tươi #EF4444)
            var mainSlider = CreateSubSlider(hbRoot.transform, "MainSlider", new Color(0.94f, 0.27f, 0.27f, 1f));

            // Link vào WorldSpaceHealthBar component
            var so = new SerializedObject(hbComp);
            so.FindProperty("mainSlider").objectReferenceValue = mainSlider;
            so.FindProperty("ghostSlider").objectReferenceValue = ghostSlider;
            so.FindProperty("heightOffset").vector3Value = offset;
            so.FindProperty("ghostSpeed").floatValue = 1.5f;
            so.ApplyModifiedProperties();

            hbComp.Initialize(maxHealth, offset);

            return hbComp;
        }

        private static Slider CreateSubSlider(Transform parent, string name, Color fillColor)
        {
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            var rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchorMin = Vector2.zero;
            rtSlider.anchorMax = Vector2.one;
            rtSlider.offsetMin = new Vector2(2f, 2f);
            rtSlider.offsetMax = new Vector2(-2f, -2f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var rtFillArea = fillArea.AddComponent<RectTransform>();
            rtFillArea.anchorMin = Vector2.zero;
            rtFillArea.anchorMax = Vector2.one;
            rtFillArea.offsetMin = Vector2.zero;
            rtFillArea.offsetMax = Vector2.zero;

            // Fill Image
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.offsetMin = Vector2.zero;
            rtFill.offsetMax = Vector2.zero;

            var imgFill = fillObj.AddComponent<Image>();
            imgFill.color = fillColor;
            imgFill.raycastTarget = false;

            slider.fillRect = rtFill;
            slider.targetGraphic = imgFill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            return slider;
        }
    }
}
