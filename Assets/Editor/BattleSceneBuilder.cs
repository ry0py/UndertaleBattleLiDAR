using MediaFrontJapan.SCIP;
using TMPro;
using UndertaleLiDAR.Battle;
using UndertaleLiDAR.Config;
using UndertaleLiDAR.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UndertaleLiDAR.EditorTools
{
    /// <summary>
    /// BattleScene 一式 (Canvas/盤面/SOUL/HP UI/弾Prefab/設定アセット/BattleManager 結線) を
    /// メニュー 1 クリックで生成する。MCP に依存せず、Unity の正規 API のみで構築する (再現可能)。
    /// メニュー: Tools/UndertaleLiDAR/Build Battle Scene
    /// </summary>
    public static class BattleSceneBuilder
    {
        private const string SettingsPath = "Assets/Settings/LidarSettings.asset";
        private const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";
        private const string ScenePath = "Assets/Scenes/BattleScene.unity";
        private const string ScipPrefabPath =
            "Packages/com.mediafrontjapan.urg-unity/Runtime/MediaFrontJapan/SCIP/SCIPInputModules.prefab";

        // Undertale 風の色。
        private static readonly Color Black = new Color(0f, 0f, 0f, 1f);
        private static readonly Color White = Color.white;
        private static readonly Color SoulRed = new Color(1f, 0f, 0f, 1f);
        private static readonly Color HpYellow = new Color(1f, 0.85f, 0f, 1f);
        private static readonly Color HpBack = new Color(0.4f, 0f, 0f, 1f);

        [MenuItem("Tools/UndertaleLiDAR/Build Battle Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");

            LidarSettings settings = LoadOrCreateSettings();
            Bullet bulletPrefab = CreateBulletPrefab();

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneSetupMode.ReplaceScene);

            // --- カメラ (URP, UI Overlay には必須ではないが調整用に置く) ---
            var cameraGO = new GameObject("Main Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            cameraGO.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            cameraGO.GetComponent<Camera>().backgroundColor = Black;
            cameraGO.transform.position = new Vector3(0f, 0f, -10f);

            // --- EventSystem (SCIP 入力モジュールが利用) ---
            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // --- Canvas (Screen Space Overlay) ---
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // --- 背景 (全面黒) ---
            GameObject bg = CreateUIObject("Background", canvasGO.transform);
            Stretch(bg);
            AddImage(bg, Black);

            // --- 盤面フレーム (白枠) → 内側 (黒) ---
            GameObject frame = CreateUIObject("BoardFrame", canvasGO.transform);
            Center(frame, new Vector2(0f, 60f), new Vector2(612f, 412f));
            AddImage(frame, White);

            GameObject boardGO = CreateUIObject("BulletBoard", frame.transform);
            Center(boardGO, Vector2.zero, new Vector2(600f, 400f));
            AddImage(boardGO, Black);
            var board = boardGO.AddComponent<BulletBoard>();

            // --- SOUL (赤ハート) ＋ Health (= プレイヤー) ---
            GameObject soulGO = CreateUIObject("Soul", boardGO.transform);
            Center(soulGO, Vector2.zero, new Vector2(18f, 18f));
            AddImage(soulGO, SoulRed);
            var soul = soulGO.AddComponent<SoulController>();
            var health = soulGO.AddComponent<Health>();
            Wire(soul, "_board", board);

            // --- HP UI (盤面下) ---
            GameObject hpPanel = CreateUIObject("HpPanel", canvasGO.transform);
            Center(hpPanel, new Vector2(0f, -210f), new Vector2(612f, 40f));

            GameObject hpBack = CreateUIObject("HpBarBack", hpPanel.transform);
            Center(hpBack, new Vector2(60f, 0f), new Vector2(200f, 24f));
            AddImage(hpBack, HpBack);

            GameObject hpFillGO = CreateUIObject("HpFill", hpBack.transform);
            Stretch(hpFillGO);
            Image hpFill = AddImage(hpFillGO, HpYellow);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = 1f;

            GameObject hpLabelGO = CreateUIObject("HpLabel", hpPanel.transform);
            Center(hpLabelGO, new Vector2(-160f, 0f), new Vector2(200f, 36f));
            TMP_Text hpLabel = hpLabelGO.AddComponent<TextMeshProUGUI>();
            hpLabel.text = "HP 20/20";
            hpLabel.fontSize = 28f;
            hpLabel.alignment = TextAlignmentOptions.Left;
            hpLabel.color = White;

            var healthView = hpPanel.AddComponent<HealthView>();
            Wire(healthView, "_health", health);
            Wire(healthView, "_label", hpLabel);
            Wire(healthView, "_fill", hpFill);

            // --- ゲームシステム (BulletSpawner / BattleManager) ---
            var systemsGO = new GameObject("BattleSystems");

            var spawner = systemsGO.AddComponent<BulletSpawner>();
            Wire(spawner, "_board", board);
            Wire(spawner, "_soul", soul);
            Wire(spawner, "_health", health);
            Wire(spawner, "_bulletPrefab", bulletPrefab);

            var manager = systemsGO.AddComponent<BattleManager>();
            Wire(manager, "_settings", settings);
            Wire(manager, "_soul", soul);
            Wire(manager, "_health", health);
            Wire(manager, "_spawner", spawner);

            // --- urg-unity (SCIPScanPlane) を配置して結線 ---
            SCIPScanPlane scanPlane = TryInstantiateScipModules();
            if (scanPlane != null)
            {
                Wire(manager, "_scanPlane", scanPlane);
            }
            else
            {
                Debug.LogWarning("[BattleSceneBuilder] SCIPInputModules.prefab を配置できませんでした。" +
                                 "BattleManager の InputMode を Mock にすればマウスで動作確認できます。");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BattleSceneBuilder] 完成: {ScenePath} / {BulletPrefabPath} / {SettingsPath}");
            EditorUtility.DisplayDialog("UndertaleLiDAR",
                "BattleScene を生成しました。\n\n" +
                "・実機(UST-10LX): Play 中に C キーで IP/位置/角度を校正\n" +
                "・実機が無い場合: BattleManager の InputMode を Mock にするとマウスで操作\n" +
                "・LidarSettings.asset で物理範囲(PhysicalMin/Max)を実測値に調整",
                "OK");
        }

        private static SCIPScanPlane TryInstantiateScipModules()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScipPrefabPath);
            if (prefab == null)
            {
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance != null ? instance.GetComponentInChildren<SCIPScanPlane>(true) : null;
        }

        private static LidarSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LidarSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LidarSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        private static Bullet CreateBulletPrefab()
        {
            var temp = CreateUIObject("Bullet", null);
            Center(temp, Vector2.zero, new Vector2(12f, 12f));
            AddImage(temp, White);
            temp.AddComponent<Bullet>();

            Bullet prefabComp = null;
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, BulletPrefabPath);
            if (prefab != null)
            {
                prefabComp = prefab.GetComponent<Bullet>();
            }
            Object.DestroyImmediate(temp);
            return prefabComp;
        }

        // --- helpers ---

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static void Center(GameObject go, Vector2 anchoredPos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }

        private static void Stretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>private [SerializeField] を SerializedObject 経由で安全に設定する。</summary>
        private static void Wire(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[BattleSceneBuilder] フィールド '{fieldName}' が {target.GetType().Name} に見つかりません。");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
