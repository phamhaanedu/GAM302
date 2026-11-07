#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBase.Editor
{
    /// <summary>
    /// Công cụ Editor tự động thiết lập:
    /// 1. GameObject AudioManager kết nối 6 AudioClips chuẩn vào Scene.
    /// 2. TextMeshPro Score Text hiển thị điểm số thời gian thực trên Canvas.
    /// 3. Popup Game Over chuẩn thẩm mỹ 60-30-10 Glassmorphism với Restart và Quit.
    /// </summary>
    public static class AudioAndUIBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/MainArena_GAM301_Base.unity";

        [MenuItem("GAM301/Setup Audio and UI System", false, 11)]
        public static void BuildAudioAndUI()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            SetupAudioManager();
            SetupScoreUI();
            SetupGameOverPopup();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[AudioAndUIBuilder] ĐÃ CÀI ĐẶT HOÀN TẤT AUDIO MANAGER VÀ UI CANVAS THÀNH CÔNG 100%!</color>");
        }

        // ==========================================
        // 1. CÀI ĐẶT AUDIO MANAGER VÀO SCENE
        // ==========================================
        private static void SetupAudioManager()
        {
            GameObject managersRoot = GameObject.Find("-- MANAGERS & GLOBAL SYSTEMS --");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("-- MANAGERS & GLOBAL SYSTEMS --");
            }

            GameObject audioMgrObj = GameObject.Find("AudioManager");
            if (audioMgrObj == null)
            {
                audioMgrObj = new GameObject("AudioManager");
                audioMgrObj.transform.SetParent(managersRoot.transform, false);
            }

            AudioManager audioMgr = audioMgrObj.GetComponent<AudioManager>();
            if (audioMgr == null) audioMgr = audioMgrObj.AddComponent<AudioManager>();

            AudioSource musicSource = audioMgrObj.transform.Find("MusicSource")?.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                GameObject msObj = new GameObject("MusicSource");
                msObj.transform.SetParent(audioMgrObj.transform, false);
                musicSource = msObj.AddComponent<AudioSource>();
            }
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;

            AudioSource sfxSource = audioMgrObj.transform.Find("SFXSource")?.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(audioMgrObj.transform, false);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            // Load các audio clips
            AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_LaserShoot.wav");
            AudioClip explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Explosion.wav");
            AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Hit.wav");
            AudioClip scoreClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Score.wav");
            AudioClip gameOverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_GameOver.wav");
            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_CyberArena.wav");

            SerializedObject so = new SerializedObject(audioMgr);
            so.FindProperty("musicSource").objectReferenceValue = musicSource;
            so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            so.FindProperty("shootClip").objectReferenceValue = shootClip;
            so.FindProperty("explosionClip").objectReferenceValue = explosionClip;
            so.FindProperty("hitClip").objectReferenceValue = hitClip;
            so.FindProperty("scoreClip").objectReferenceValue = scoreClip;
            so.FindProperty("gameOverClip").objectReferenceValue = gameOverClip;
            so.FindProperty("bgmClip").objectReferenceValue = bgmClip;
            so.FindProperty("musicVolume").floatValue = 0.5f;
            so.FindProperty("sfxVolume").floatValue = 0.85f;
            so.FindProperty("autoPlayBgmOnStart").boolValue = true;
            so.ApplyModifiedProperties();

            Debug.Log("[AudioAndUIBuilder] Đã cấu hình xong AudioManager với 6 AudioClips chuẩn!");
        }

        // ==========================================
        // 2. CÀI ĐẶT TEXT SCORE TRÊN CANVAS
        // ==========================================
        private static void SetupScoreUI()
        {
            GameObject canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[AudioAndUIBuilder] Không tìm thấy UI_Canvas!");
                return;
            }

            Transform existingScore = canvasObj.transform.Find("Text_Score");
            GameObject scoreObj = existingScore != null ? existingScore.gameObject : new GameObject("Text_Score");
            scoreObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = scoreObj.GetComponent<RectTransform>();
            if (rt == null) rt = scoreObj.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f);
            rt.sizeDelta = new Vector2(320f, 50f);

            TextMeshProUGUI tmp = scoreObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = scoreObj.AddComponent<TextMeshProUGUI>();

            tmp.fontSize = 32f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // Tắt raycast theo rule AGENTS.md
            tmp.text = "SCORE: <color=#38BDF8>0000</color>";

            ScoreDisplayUI displayUI = scoreObj.GetComponent<ScoreDisplayUI>();
            if (displayUI == null) displayUI = scoreObj.AddComponent<ScoreDisplayUI>();

            Debug.Log("[AudioAndUIBuilder] Đã cấu hình xong Text_Score (TextMeshPro) trên UI_Canvas!");
        }

        // ==========================================
        // 3. CÀI ĐẶT POPUP GAME OVER TRÊN CANVAS
        // ==========================================
        // 3. CÀI ĐẶT POPUP GAME OVER TRÊN CANVAS (DÙNG TMP_DefaultControls)
        // ==========================================
        private static void SetupGameOverPopup()
        {
            GameObject canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj == null) return;

            Transform existingPopup = canvasObj.transform.Find("Panel_GameOver");
            if (existingPopup != null)
            {
                Object.DestroyImmediate(existingPopup.gameObject);
            }

            TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources();

            // 1. Panel Overlay (Toàn màn hình tối 60%)
            GameObject popupObj = new GameObject("Panel_GameOver", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            popupObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rtPopup = popupObj.GetComponent<RectTransform>();
            rtPopup.anchorMin = Vector2.zero;
            rtPopup.anchorMax = Vector2.one;
            rtPopup.offsetMin = Vector2.zero;
            rtPopup.offsetMax = Vector2.zero;

            Image bgOverlay = popupObj.GetComponent<Image>();
            bgOverlay.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);
            bgOverlay.raycastTarget = true;

            CanvasGroup cg = popupObj.GetComponent<CanvasGroup>();

            // 2. Card Container (Glassmorphism 30%)
            GameObject cardObj = new GameObject("Card_Container", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(popupObj.transform, false);

            RectTransform rtCard = cardObj.GetComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.anchoredPosition = Vector2.zero;
            rtCard.sizeDelta = new Vector2(480f, 350f);

            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.16f, 0.23f, 0.96f);
            cardImg.raycastTarget = false;

            // 3. Tiêu đề GAME OVER qua TMP_DefaultControls
            GameObject titleObj = TMP_DefaultControls.CreateText(res);
            titleObj.name = "Text_Title";
            titleObj.transform.SetParent(cardObj.transform, false);

            RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.5f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
            rtTitle.pivot = new Vector2(0.5f, 0.5f);
            rtTitle.anchoredPosition = new Vector2(0f, 100f);
            rtTitle.sizeDelta = new Vector2(420f, 60f);

            TextMeshProUGUI tmpTitle = titleObj.GetComponent<TextMeshProUGUI>();
            tmpTitle.text = "GAME OVER";
            tmpTitle.fontSize = 46f;
            tmpTitle.fontStyle = FontStyles.Bold;
            tmpTitle.color = new Color(0.94f, 0.27f, 0.27f, 1f); // Neon Red
            tmpTitle.alignment = TextAlignmentOptions.Center;
            tmpTitle.raycastTarget = false;

            // 4. Text FINAL SCORE qua TMP_DefaultControls
            GameObject finalScoreObj = TMP_DefaultControls.CreateText(res);
            finalScoreObj.name = "Text_FinalScore";
            finalScoreObj.transform.SetParent(cardObj.transform, false);

            RectTransform rtFinal = finalScoreObj.GetComponent<RectTransform>();
            rtFinal.anchorMin = new Vector2(0.5f, 0.5f);
            rtFinal.anchorMax = new Vector2(0.5f, 0.5f);
            rtFinal.pivot = new Vector2(0.5f, 0.5f);
            rtFinal.anchoredPosition = new Vector2(0f, 35f);
            rtFinal.sizeDelta = new Vector2(420f, 40f);

            TextMeshProUGUI tmpFinal = finalScoreObj.GetComponent<TextMeshProUGUI>();
            tmpFinal.text = "FINAL SCORE: <color=#38BDF8>0000</color>";
            tmpFinal.fontSize = 24f;
            tmpFinal.fontStyle = FontStyles.Bold;
            tmpFinal.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            tmpFinal.alignment = TextAlignmentOptions.Center;
            tmpFinal.raycastTarget = false;

            // 5. Nút RESTART qua TMP_DefaultControls (CTA Neon Orange 10%)
            GameObject btnRestartObj = TMP_DefaultControls.CreateButton(res);
            btnRestartObj.name = "Button_Restart";
            btnRestartObj.transform.SetParent(cardObj.transform, false);

            RectTransform rtRestart = btnRestartObj.GetComponent<RectTransform>();
            rtRestart.anchorMin = new Vector2(0.5f, 0.5f);
            rtRestart.anchorMax = new Vector2(0.5f, 0.5f);
            rtRestart.pivot = new Vector2(0.5f, 0.5f);
            rtRestart.anchoredPosition = new Vector2(0f, -40f);
            rtRestart.sizeDelta = new Vector2(250f, 50f);

            Image imgRestart = btnRestartObj.GetComponent<Image>();
            imgRestart.color = new Color(0.92f, 0.35f, 0.05f, 1f);

            Button btnRestart = btnRestartObj.GetComponent<Button>();
            TextMeshProUGUI tmpRestart = btnRestartObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpRestart != null)
            {
                tmpRestart.text = "RESTART";
                tmpRestart.fontSize = 20f;
                tmpRestart.fontStyle = FontStyles.Bold;
                tmpRestart.color = Color.white;
                tmpRestart.alignment = TextAlignmentOptions.Center;
                tmpRestart.raycastTarget = false;
            }

            // 6. Nút QUIT qua TMP_DefaultControls
            GameObject btnQuitObj = TMP_DefaultControls.CreateButton(res);
            btnQuitObj.name = "Button_Quit";
            btnQuitObj.transform.SetParent(cardObj.transform, false);

            RectTransform rtQuit = btnQuitObj.GetComponent<RectTransform>();
            rtQuit.anchorMin = new Vector2(0.5f, 0.5f);
            rtQuit.anchorMax = new Vector2(0.5f, 0.5f);
            rtQuit.pivot = new Vector2(0.5f, 0.5f);
            rtQuit.anchoredPosition = new Vector2(0f, -105f);
            rtQuit.sizeDelta = new Vector2(250f, 44f);

            Image imgQuit = btnQuitObj.GetComponent<Image>();
            imgQuit.color = new Color(0.2f, 0.25f, 0.33f, 1f);

            Button btnQuit = btnQuitObj.GetComponent<Button>();
            TextMeshProUGUI tmpQuit = btnQuitObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpQuit != null)
            {
                tmpQuit.text = "QUIT";
                tmpQuit.fontSize = 18f;
                tmpQuit.fontStyle = FontStyles.Bold;
                tmpQuit.color = new Color(0.8f, 0.85f, 0.9f, 1f);
                tmpQuit.alignment = TextAlignmentOptions.Center;
                tmpQuit.raycastTarget = false;
            }

            // 7. Gắn GameOverPopupUI và liên kết references
            GameOverPopupUI popupScript = popupObj.AddComponent<GameOverPopupUI>();
            SerializedObject soPopup = new SerializedObject(popupScript);
            soPopup.FindProperty("popupRoot").objectReferenceValue = popupObj;
            soPopup.FindProperty("canvasGroup").objectReferenceValue = cg;
            soPopup.FindProperty("finalScoreText").objectReferenceValue = tmpFinal;
            soPopup.FindProperty("restartButton").objectReferenceValue = btnRestart;
            soPopup.FindProperty("quitButton").objectReferenceValue = btnQuit;
            soPopup.ApplyModifiedProperties();

            // Mặc định ẩn popup ban đầu
            popupObj.SetActive(false);

            Debug.Log("[AudioAndUIBuilder] Đã cấu hình xong Panel_GameOver (Popup Glassmorphism TMP) trên UI_Canvas!");
        }
    }
}
#endif
