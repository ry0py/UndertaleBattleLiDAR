using MediaFrontJapan.SCIP;
using TMPro;
using UndertaleLiDAR.Audio;
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
    /// BattleScene 一式 (Undertale 風 UI: 敵/盤面/SOUL/HP/セリフ枠/メニュー/弾Prefab/
    /// 設定アセット/MusicManager/BattleManager/BattleDirector 結線) を
    /// メニュー 1 クリックで生成する。MCP に依存せず Unity の正規 API のみで構築する (再現可能)。
    /// メニュー: Tools/UndertaleLiDAR/Build Battle Scene
    /// </summary>
    public static class BattleSceneBuilder
    {
        private const string SettingsPath = "Assets/Settings/LidarSettings.asset";
        private const string EncounterPath = "Assets/Settings/Encounter.asset";
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
        private static readonly Color Orange = new Color(1f, 0.55f, 0f, 1f);

        [MenuItem("Tools/UndertaleLiDAR/Build Battle Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");

            LidarSettings settings = LoadOrCreateSettings();
            EncounterData encounter = LoadOrCreateEncounter();
            Bullet bulletPrefab = CreateBulletPrefab();

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- カメラ (URP・AudioListener も兼ねる) ---
            var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Black;
            cameraGO.transform.position = new Vector3(0f, 0f, -10f);

            // --- EventSystem (ボタン/SCIP 入力が利用) ---
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // --- Canvas (Screen Space Overlay) ---
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform canvasT = canvasGO.transform;

            // --- 背景 (全面黒) ---
            GameObject bg = CreateUIObject("Background", canvasT);
            Stretch(bg);
            AddImage(bg, Black);

            // --- 敵エリア (上部・プレースホルダー) ---
            GameObject enemy = CreateUIObject("Enemy", canvasT);
            Center(enemy, new Vector2(0f, 325f), new Vector2(170f, 170f));
            AddImage(enemy, White);
            TMP_Text enemyName = CreateText("EnemyName", canvasT, encounter.EnemyName,
                30f, TextAlignmentOptions.Center, White);
            Center(enemyName.gameObject, new Vector2(0f, 215f), new Vector2(500f, 40f));

            // --- 盤面フレーム (白枠) → 内側 (黒)。セリフ枠と弾幕枠を兼ねる ---
            GameObject frame = CreateUIObject("BoardFrame", canvasT);
            Center(frame, new Vector2(0f, 25f), new Vector2(612f, 300f));
            AddImage(frame, White);

            GameObject boardGO = CreateUIObject("BulletBoard", frame.transform);
            Center(boardGO, Vector2.zero, new Vector2(600f, 288f));
            AddImage(boardGO, Black);
            var board = boardGO.AddComponent<BulletBoard>();

            // --- セリフ表示 (盤面内・タイプライター) ---
            TMP_Text dialogueLabel = CreateText("DialogueText", boardGO.transform, string.Empty,
                30f, TextAlignmentOptions.TopLeft, White);
            StretchPadded(dialogueLabel.gameObject, 24f);
            var dialogue = dialogueLabel.gameObject.AddComponent<DialogueBox>();

            // --- SOUL (赤ハート) ＋ Health (= プレイヤー) ---
            GameObject soulGO = CreateUIObject("Soul", boardGO.transform);
            Center(soulGO, Vector2.zero, new Vector2(18f, 18f));
            AddImage(soulGO, SoulRed);
            var soul = soulGO.AddComponent<SoulController>();
            var health = soulGO.AddComponent<Health>();
            Wire(soul, "_board", board);

            // --- ステータス行 (盤面下: 名前/LV + HP ゲージ + 数値) ---
            GameObject statusGO = CreateUIObject("StatusPanel", canvasT);
            Center(statusGO, new Vector2(0f, -180f), new Vector2(780f, 40f));

            TMP_Text nameLv = CreateText("NameLv", statusGO.transform, "Chara   LV  1",
                26f, TextAlignmentOptions.Left, White);
            Center(nameLv.gameObject, new Vector2(-230f, 0f), new Vector2(320f, 36f));

            TMP_Text hpTag = CreateText("HpTag", statusGO.transform, "HP",
                26f, TextAlignmentOptions.Center, HpYellow);
            Center(hpTag.gameObject, new Vector2(40f, 0f), new Vector2(50f, 36f));

            GameObject hpBack = CreateUIObject("HpBarBack", statusGO.transform);
            Center(hpBack, new Vector2(150f, 0f), new Vector2(150f, 22f));
            AddImage(hpBack, HpBack);

            GameObject hpFillGO = CreateUIObject("HpFill", hpBack.transform);
            Stretch(hpFillGO);
            Image hpFill = AddImage(hpFillGO, HpYellow);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = 1f;

            TMP_Text hpNum = CreateText("HpNumber", statusGO.transform, "20 / 20",
                26f, TextAlignmentOptions.Left, White);
            Center(hpNum.gameObject, new Vector2(290f, 0f), new Vector2(140f, 36f));

            var healthView = statusGO.AddComponent<HealthView>();
            Wire(healthView, "_health", health);
            Wire(healthView, "_label", hpNum);
            Wire(healthView, "_fill", hpFill);

            // --- メニュー (FIGHT / ACT / ITEM / MERCY) ---
            GameObject menuGO = CreateUIObject("ButtonRow", canvasT);
            Center(menuGO, new Vector2(0f, -255f), new Vector2(900f, 60f));
            Button bFight = CreateMenuButton(menuGO.transform, "Fight", "FIGHT", new Vector2(-300f, 0f));
            Button bAct = CreateMenuButton(menuGO.transform, "Act", "ACT", new Vector2(-100f, 0f));
            Button bItem = CreateMenuButton(menuGO.transform, "Item", "ITEM", new Vector2(100f, 0f));
            Button bMercy = CreateMenuButton(menuGO.transform, "Mercy", "MERCY", new Vector2(300f, 0f));

            var battleMenu = menuGO.AddComponent<BattleMenu>();
            Wire(battleMenu, "_fight", bFight);
            Wire(battleMenu, "_act", bAct);
            Wire(battleMenu, "_item", bItem);
            Wire(battleMenu, "_mercy", bMercy);

            // アセット参照は「インポート済みのものを読み直して」から結線する。
            // 作成直後のインメモリ参照は GUID 未確定なことがあり、その状態で結線すると
            // シーン保存時に参照が null 化する (シーン内オブジェクト参照は影響を受けない)。
            AssetDatabase.SaveAssets();
            settings = AssetDatabase.LoadAssetAtPath<LidarSettings>(SettingsPath);
            encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(EncounterPath);
            bulletPrefab = AssetDatabase.LoadAssetAtPath<Bullet>(BulletPrefabPath);

            // --- ゲームシステム (Spawner / Music / BattleManager / BattleDirector) ---
            var systemsGO = new GameObject("BattleSystems");

            var music = systemsGO.AddComponent<MusicManager>();
            Wire(dialogue, "_label", dialogueLabel);
            Wire(dialogue, "_music", music);

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

            var director = systemsGO.AddComponent<BattleDirector>();
            Wire(director, "_encounter", encounter);
            Wire(director, "_dialogue", dialogue);
            Wire(director, "_menu", battleMenu);
            Wire(director, "_spawner", spawner);
            Wire(director, "_soul", soul);
            Wire(director, "_health", health);
            Wire(director, "_music", music);

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

            // 注: ここで EditorUtility.DisplayDialog を出すと MCP 自動実行時に
            // メインスレッドのモーダルでブロックするため、ログ通知に留める。
            Debug.Log($"[BattleSceneBuilder] 完成: {ScenePath} / {BulletPrefabPath} / {SettingsPath} / {EncounterPath}\n" +
                "・導入セリフ → Z/Enter/Space で送り → メニュー\n" +
                "・FIGHT/ACT/ITEM/MERCY をクリックすると敵ターン (弾幕回避) へ\n" +
                "・ACT を 2 回で MERCY 可能。MERCY で勝利\n" +
                "・実機が無い場合: BattleManager の InputMode を Mock にするとマウスで操作\n" +
                "・BGM/SFX は MusicManager に AudioClip を割り当てると鳴ります");
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

        private static EncounterData LoadOrCreateEncounter()
        {
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(EncounterPath);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterData>();
                AssetDatabase.CreateAsset(encounter, EncounterPath);
                AssetDatabase.SaveAssets();
            }
            return encounter;
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

        /// <summary>指定フォルダが無ければ親から順に作成する (AssetDatabase は階層を 1 段ずつしか作れない)。</summary>
        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text,
            float size, TextAlignmentOptions align, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>FIGHT 等の 1 ボタンを生成する (橙枠 + 黒地 + 橙文字)。</summary>
        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject outer = CreateUIObject(name, parent);
            Center(outer, pos, new Vector2(190f, 54f));
            Image border = AddImage(outer, Orange);

            GameObject fill = CreateUIObject("Fill", outer.transform);
            StretchPadded(fill, 5f);
            AddImage(fill, Black);

            TMP_Text txt = CreateText("Label", outer.transform, label, 26f, TextAlignmentOptions.Center, Orange);
            Stretch(txt.gameObject);

            var btn = outer.AddComponent<Button>();
            btn.targetGraphic = border;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.72f, 0.25f);
            colors.pressedColor = new Color(1f, 0.82f, 0.45f);
            colors.disabledColor = new Color(0.45f, 0.28f, 0f);
            btn.colors = colors;
            return btn;
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

        /// <summary>親いっぱいに広げつつ四辺に padding[px] の余白を残す。</summary>
        private static void StretchPadded(GameObject go, float padding)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
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
