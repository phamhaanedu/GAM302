#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBase.Editor
{
    /// <summary>
    /// Công cụ Editor tự động thiết kế lại 100% UI/UX theo chuẩn unity_ui_ux_master_sop.md:
    /// 1. Thanh máu HUD chuẩn AAA Glassmorphism: Khung bo viền, Icon HP badge, Ghost Slider tụt chậm, Text HP 1000/1000.
    /// 2. Thẻ Score HUD góc trên phải: Card Glassmorphism viền Cyan, icon vàng, TextMeshPro Tabular digits.
    /// 3. Nút bấm tương tác: Gắn UIButtonFeedback (Hover scale 1.05x, Press 0.92x, Release bounce EaseOutBack, SFX).
    /// 4. Tắt Raycast Target toàn bộ các phần tử nền và chữ tĩnh để tối ưu FPS tối đa.
    /// </summary>
    public static class UIApexBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/MainArena_GAM301_Base.unity";

        [MenuItem("GAM301/UI-UX/Master Polish HUD and Popup", false, 12)]
        public static void BuildMasterUI()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            GameObject canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[UIApexBuilder] Không tìm thấy UI_Canvas!");
                return;
            }

            BuildPlayerHealthHUD(canvasObj);
            BuildScoreHUD(canvasObj);
            PolishGameOverPopup(canvasObj);

            Canvas.ForceUpdateCanvases();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[UIApexBuilder] ĐÃ HOÀN TẤT THIẾT KẾ LẠI TOÀN BỘ GIAO DIỆN UI/UX CHUẨN MASTER SOP!</color>");
        }

        // ==========================================
        // 1. THANH MÁU HUD CHUẨN AAA GLASSMORPHISM
        // ==========================================
        private static void BuildPlayerHealthHUD(GameObject canvasObj)
        {
            // Xóa slider cũ nếu có
            Transform oldSlider = canvasObj.transform.Find("HealthBar_Slider");
            if (oldSlider != null) Object.DestroyImmediate(oldSlider.gameObject);

            Transform oldHud = canvasObj.transform.Find("HUD_PlayerHealth");
            if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

            // Container chính: Anchor Top-Left (0, 1), Size 360x44, Pos (35, -35)
            GameObject hudObj = new GameObject("HUD_PlayerHealth", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hudObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rtHud = hudObj.GetComponent<RectTransform>();
            rtHud.anchorMin = new Vector2(0f, 1f);
            rtHud.anchorMax = new Vector2(0f, 1f);
            rtHud.pivot = new Vector2(0f, 1f);
            rtHud.anchoredPosition = new Vector2(35f, -35f);
            rtHud.sizeDelta = new Vector2(360f, 44f);

            // Nền tối 60% Dark Slate (#0F172A)
            Image bgHud = hudObj.GetComponent<Image>();
            bgHud.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);
            bgHud.raycastTarget = false;

            // Icon Badge HP góc trái (36x36)
            GameObject badgeObj = new GameObject("Badge_HP", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObj.transform.SetParent(hudObj.transform, false);
            RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
            rtBadge.anchorMin = new Vector2(0f, 0.5f);
            rtBadge.anchorMax = new Vector2(0f, 0.5f);
            rtBadge.pivot = new Vector2(0.5f, 0.5f);
            rtBadge.anchoredPosition = new Vector2(22f, 0f);
            rtBadge.sizeDelta = new Vector2(32f, 32f);

            Image imgBadge = badgeObj.GetComponent<Image>();
            imgBadge.color = new Color(0.96f, 0.25f, 0.37f, 1f); // Neon Pink/Red #F43F5E
            imgBadge.raycastTarget = false;

            GameObject badgeTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            RectTransform rtBadgeText = badgeTextObj.GetComponent<RectTransform>();
            rtBadgeText.anchorMin = Vector2.zero;
            rtBadgeText.anchorMax = Vector2.one;
            rtBadgeText.offsetMin = Vector2.zero;
            rtBadgeText.offsetMax = Vector2.zero;
            TextMeshProUGUI tmpBadge = badgeTextObj.GetComponent<TextMeshProUGUI>();
            tmpBadge.text = "+";
            tmpBadge.fontSize = 26f;
            tmpBadge.fontStyle = FontStyles.Bold;
            tmpBadge.color = Color.white;
            tmpBadge.alignment = TextAlignmentOptions.Center;
            tmpBadge.raycastTarget = false;

            // Bar Area (khu vực chứa 2 sliders)
            GameObject barArea = new GameObject("Bar_Area", typeof(RectTransform));
            barArea.transform.SetParent(hudObj.transform, false);
            RectTransform rtBar = barArea.GetComponent<RectTransform>();
            rtBar.anchorMin = new Vector2(0f, 0f);
            rtBar.anchorMax = new Vector2(1f, 1f);
            rtBar.offsetMin = new Vector2(44f, 6f);
            rtBar.offsetMax = new Vector2(-10f, -6f);

            // A. Ghost Slider (Thanh đỏ/cam trôi chậm)
            GameObject ghostObj = CreateSliderObject("GhostSlider", barArea.transform, new Color(0.86f, 0.15f, 0.15f, 0.85f));
            Slider ghostSlider = ghostObj.GetComponent<Slider>();

            // B. Main Slider (Thanh xanh ngọc chính)
            GameObject mainObj = CreateSliderObject("MainSlider", barArea.transform, new Color(0.06f, 0.73f, 0.51f, 1f));
            Slider mainSlider = mainObj.GetComponent<Slider>();
            Image mainFill = mainObj.transform.Find("Fill Area/Fill")?.GetComponent<Image>();

            // C. Text HP Number (TextMeshProUGUI hiển thị con số máu)
            GameObject hpTextObj = new GameObject("Text_HPNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            hpTextObj.transform.SetParent(barArea.transform, false);
            RectTransform rtHpText = hpTextObj.GetComponent<RectTransform>();
            rtHpText.anchorMin = Vector2.zero;
            rtHpText.anchorMax = Vector2.one;
            rtHpText.offsetMin = Vector2.zero;
            rtHpText.offsetMax = Vector2.zero;

            TextMeshProUGUI tmpHp = hpTextObj.GetComponent<TextMeshProUGUI>();
            tmpHp.text = "HP: <color=#FFFFFF>1000</color> / 1000";
            tmpHp.fontSize = 18f;
            tmpHp.fontStyle = FontStyles.Bold;
            tmpHp.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            tmpHp.alignment = TextAlignmentOptions.Center;
            tmpHp.raycastTarget = false;

            // Gắn PlayerHealthBarHUD script
            PlayerHealthBarHUD hudScript = hudObj.AddComponent<PlayerHealthBarHUD>();
            SerializedObject soHud = new SerializedObject(hudScript);
            soHud.FindProperty("mainSlider").objectReferenceValue = mainSlider;
            soHud.FindProperty("ghostSlider").objectReferenceValue = ghostSlider;
            soHud.FindProperty("mainFillImage").objectReferenceValue = mainFill;
            soHud.FindProperty("hpNumberText").objectReferenceValue = tmpHp;
            soHud.ApplyModifiedProperties();

            // Cập nhật tham chiếu vào PlayerController
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                SerializedObject soPlayer = new SerializedObject(player);
                soPlayer.FindProperty("healthBarSlider").objectReferenceValue = mainSlider;
                soPlayer.ApplyModifiedProperties();
                hudScript.SetHealth(1000f, 1000f);
            }

            Debug.Log("[UIApexBuilder] Đã tạo xong HUD_PlayerHealth chuẩn AAA Glassmorphism!");
        }

        private static GameObject CreateSliderObject(string name, Transform parent, Color fillColor)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);

            RectTransform rt = sliderObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;

            // Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform rtBg = bgObj.GetComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.14f, 0.2f, 0.5f);
            bgImg.raycastTarget = false;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform rtFillArea = fillArea.GetComponent<RectTransform>();
            rtFillArea.anchorMin = Vector2.zero;
            rtFillArea.anchorMax = Vector2.one;
            rtFillArea.offsetMin = Vector2.zero;
            rtFillArea.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform rtFill = fillObj.GetComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.offsetMin = Vector2.zero;
            rtFill.offsetMax = Vector2.zero;
            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            slider.targetGraphic = bgImg;
            slider.fillRect = rtFill;
            slider.minValue = 0f;
            slider.maxValue = 1000f;
            slider.value = 1000f;

            return sliderObj;
        }

        // ==========================================
        // 2. THẺ SCORE HUD CHUẨN GLASSMORPHISM
        // ==========================================
        private static void BuildScoreHUD(GameObject canvasObj)
        {
            Transform existingScore = canvasObj.transform.Find("Text_Score");
            if (existingScore != null) Object.DestroyImmediate(existingScore.gameObject);

            Transform oldScoreCard = canvasObj.transform.Find("HUD_ScoreCard");
            if (oldScoreCard != null) Object.DestroyImmediate(oldScoreCard.gameObject);

            // Container Score Card: Anchor Top-Right (1, 1), Size 240x44, Pos (-35, -35)
            GameObject scoreCard = new GameObject("HUD_ScoreCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            scoreCard.transform.SetParent(canvasObj.transform, false);

            RectTransform rtCard = scoreCard.GetComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(1f, 1f);
            rtCard.anchorMax = new Vector2(1f, 1f);
            rtCard.pivot = new Vector2(1f, 1f);
            rtCard.anchoredPosition = new Vector2(-35f, -35f);
            rtCard.sizeDelta = new Vector2(240f, 44f);

            Image cardImg = scoreCard.GetComponent<Image>();
            cardImg.color = new Color(0.06f, 0.09f, 0.16f, 0.92f); // Dark Slate #0F172A
            cardImg.raycastTarget = false;

            // Badge Icon Ngôi Sao Vàng góc trái Card
            GameObject starBadge = new GameObject("Badge_Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            starBadge.transform.SetParent(scoreCard.transform, false);
            RectTransform rtStar = starBadge.GetComponent<RectTransform>();
            rtStar.anchorMin = new Vector2(0f, 0.5f);
            rtStar.anchorMax = new Vector2(0f, 0.5f);
            rtStar.pivot = new Vector2(0.5f, 0.5f);
            rtStar.anchoredPosition = new Vector2(22f, 0f);
            rtStar.sizeDelta = new Vector2(30f, 30f);

            Image imgStar = starBadge.GetComponent<Image>();
            imgStar.color = new Color(0.96f, 0.62f, 0.04f, 1f); // Amber Gold #F59E0B
            imgStar.raycastTarget = false;

            GameObject starTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            starTextObj.transform.SetParent(starBadge.transform, false);
            RectTransform rtStarText = starTextObj.GetComponent<RectTransform>();
            rtStarText.anchorMin = Vector2.zero;
            rtStarText.anchorMax = Vector2.one;
            rtStarText.offsetMin = Vector2.zero;
            rtStarText.offsetMax = Vector2.zero;
            TextMeshProUGUI tmpStar = starTextObj.GetComponent<TextMeshProUGUI>();
            tmpStar.text = "★";
            tmpStar.fontSize = 20f;
            tmpStar.color = Color.white;
            tmpStar.alignment = TextAlignmentOptions.Center;
            tmpStar.raycastTarget = false;

            // Text Score
            GameObject textScoreObj = new GameObject("Text_Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textScoreObj.transform.SetParent(scoreCard.transform, false);

            RectTransform rtScoreText = textScoreObj.GetComponent<RectTransform>();
            rtScoreText.anchorMin = new Vector2(0f, 0f);
            rtScoreText.anchorMax = new Vector2(1f, 1f);
            rtScoreText.offsetMin = new Vector2(44f, 0f);
            rtScoreText.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI tmpScore = textScoreObj.GetComponent<TextMeshProUGUI>();
            tmpScore.text = "SCORE: <color=#38BDF8>0000</color>";
            tmpScore.fontSize = 20f;
            tmpScore.fontStyle = FontStyles.Bold;
            tmpScore.color = new Color(0.9f, 0.93f, 0.96f, 1f);
            tmpScore.alignment = TextAlignmentOptions.Right;
            tmpScore.raycastTarget = false;

            // Gắn ScoreDisplayUI vào card để thực hiện Punch Scale
            ScoreDisplayUI scoreUI = textScoreObj.AddComponent<ScoreDisplayUI>();

            Debug.Log("[UIApexBuilder] Đã tạo xong HUD_ScoreCard chuẩn Glassmorphism!");
        }

        // ==========================================
        // 3. TỐI ƯU & GẮN NÚT BẤM JUICE CHO GAME OVER POPUP
        // ==========================================
        private static void PolishGameOverPopup(GameObject canvasObj)
        {
            Transform popup = canvasObj.transform.Find("Panel_GameOver");
            if (popup == null) return;

            Transform btnRestart = popup.Find("Card_Container/Button_Restart");
            if (btnRestart != null)
            {
                UIButtonFeedback fb = btnRestart.GetComponent<UIButtonFeedback>();
                if (fb == null) btnRestart.gameObject.AddComponent<UIButtonFeedback>();
            }

            Transform btnQuit = popup.Find("Card_Container/Button_Quit");
            if (btnQuit != null)
            {
                UIButtonFeedback fb = btnQuit.GetComponent<UIButtonFeedback>();
                if (fb == null) btnQuit.gameObject.AddComponent<UIButtonFeedback>();
            }

            Debug.Log("[UIApexBuilder] Đã gắn UIButtonFeedback cho Restart và Quit buttons!");
        }
    }
}
#endif
