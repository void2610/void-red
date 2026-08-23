using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// オークションシーンを旧 BattleScene の資産 (背景 / 机 / 感情ホイール / 入札ウィンドウ / 対話カットイン / 天秤) から組み立てるエディタツール
/// 生成物は AuctionScene.unity と、新ルールで足りない小物プレハブだけ
/// </summary>
public static class AuctionUiBuilder
{
    private const string PREFAB_DIR = "Assets/Prefabs/AuctionSceneView";
    private const string LOBBY_PREFAB_DIR = "Assets/Prefabs/HomeSceneView";
    private const string NORMAL_BUTTON = "Assets/Prefabs/Button/NormalButton.prefab";
    private const string BATTLE_SCENE = "Assets/Scenes/BattleScene.unity";
    private const string HOME_SCENE = "Assets/Scenes/HomeScene.unity";
    private const string AUCTION_SCENE = "Assets/Scenes/AuctionScene.unity";
    private const string ALL_FLOOR_DATA = "Assets/ScriptableObjects/Auction/AllFloorData.asset";
    private const string ROOT_SCOPE_PREFAB = "Assets/Prefabs/Root/RootLifetimeScope.prefab";
    private const string DIALOGUE_PHASE_PREFAB = "Assets/Prefabs/NewBattleSceneView/DialoguePhase/DialoguePhaseView.prefab";

    // 旧ルール専用で、新オークションでは使わないプレハブインスタンス
    private static readonly string[] OBSOLETE_OBJECTS =
    {
        "CardBattleView", "DeckSelectionView", "RewardPhaseView", "MemoryGrowthView", "SkillButtonView", "TutorialView", "TutorialGizmoHelper",
    };

    private static readonly Color PANEL_BG = new(0.08f, 0.05f, 0.08f, 0.9f);
    private static readonly Color OVERLAY_BG = new(0.05f, 0.02f, 0.04f, 0.95f);

    private static TMP_FontAsset _font;

    [MenuItem("VoidRed/Auction/Build All")]
    public static void BuildAll()
    {
        BuildPrefabs();
        BuildScene();
        BuildLobby();
    }

    [MenuItem("VoidRed/Auction/Build UI Prefabs")]
    public static void BuildPrefabs()
    {
        EnsureDir(PREFAB_DIR);
        LoadFont();
        BuildParticipantIcon();
        BuildWonLotEntry();
        AssetDatabase.SaveAssets();
        Debug.Log("[AuctionUiBuilder] プレハブ生成完了");
    }

    [MenuItem("VoidRed/Auction/Build Scene")]
    public static void BuildScene()
    {
        LoadFont();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AUCTION_SCENE) != null) AssetDatabase.DeleteAsset(AUCTION_SCENE);
        if (!AssetDatabase.CopyAsset(BATTLE_SCENE, AUCTION_SCENE)) throw new InvalidOperationException("BattleScene の複製に失敗");
        var scene = EditorSceneManager.OpenScene(AUCTION_SCENE, OpenSceneMode.Single);

        // missing prefab は名前に "(Missing Prefab ...)" が付くため前方一致で消す
        foreach (var go in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).Distinct().ToList())
        {
            if (go && OBSOLETE_OBJECTS.Any(n => go.name.StartsWith(n))) UnityEngine.Object.DestroyImmediate(go);
        }
        foreach (var scope in scene.GetRootGameObjects().Where(g => g.name == "BattleLifetimeScope").ToList()) UnityEngine.Object.DestroyImmediate(scope);

        var canvas = scene.GetRootGameObjects().First(g => g.name == "Canvas");
        // 対話 View はシーンに元からあるものを使う (無ければプレハブから置く)
        var dialogueView = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DialoguePhaseView>(true)).FirstOrDefault();
        var dialogue = dialogueView ? dialogueView.gameObject : InstantiatePrefab(DIALOGUE_PHASE_PREFAB, canvas.transform);
        var participantBar = BuildParticipantBar(canvas);
        var baptism = BuildBaptism(canvas);
        var gameOver = BuildGameOver(canvas);

        var sceneViewGo = new GameObject("AuctionSceneView");
        var sceneView = sceneViewGo.AddComponent<AuctionSceneView>();
        Wire(sceneView,
            ("theme", Find<ThemeView>(scene)),
            ("announcement", Find<AnnouncementView>(scene)),
            ("auction", Find<AuctionView>(scene)),
            ("dialogue", dialogue.GetComponent<DialoguePhaseView>()),
            ("competition", Find<CompetitionView>(scene)),
            ("baptism", baptism),
            ("gameOver", gameOver),
            ("rival", Find<EnemyView>(scene)),
            ("participantBar", participantBar.transform),
            ("participantIconPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ParticipantIcon.prefab").GetComponent<ParticipantIconView>()));

        // 旧ルールの固定文言を新ルールに差し替える
        foreach (var text in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMeshProUGUI>(true)).Where(t => t.name == "InstructionText"))
        {
            var tso = new SerializedObject(text);
            tso.FindProperty("m_text").stringValue = "";
            tso.ApplyModifiedPropertiesWithoutUndo();
        }

        var scopeGo = new GameObject("AuctionLifetimeScope");
        scopeGo.AddComponent<AuctionLifetimeScope>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        RegisterSceneInBuild();
        Debug.Log("[AuctionUiBuilder] AuctionScene 生成完了");
    }

    [MenuItem("VoidRed/Auction/Build Lobby")]
    public static void BuildLobby()
    {
        LoadFont();

        var rootPrefab = PrefabUtility.LoadPrefabContents(ROOT_SCOPE_PREFAB);
        var rootSo = new SerializedObject(rootPrefab.GetComponent<RootLifetimeScope>());
        rootSo.FindProperty("allFloorData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AllFloorData>(ALL_FLOOR_DATA);
        rootSo.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(rootPrefab, ROOT_SCOPE_PREFAB);
        PrefabUtility.UnloadPrefabContents(rootPrefab);

        var entryPrefab = BuildCollectionEntry();
        var collectionPrefab = BuildCollectionWindow(entryPrefab);
        var personaPrefab = BuildPersonaWindow();

        var scene = EditorSceneManager.OpenScene(HOME_SCENE, OpenSceneMode.Single);
        var canvas = scene.GetRootGameObjects().First(g => g.name == "Canvas");
        foreach (var stale in Children(canvas.transform).Where(c => c.name is "MemoryCollectionView" or "PersonaView" or "ProgressText").ToList()) UnityEngine.Object.DestroyImmediate(stale.gameObject);

        var progress = Text(canvas, "ProgressText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(progress.rectTransform, new Vector2(0f, 1f), new Vector2(230, -24), new Vector2(440, 28));
        var collection = (GameObject)PrefabUtility.InstantiatePrefab(collectionPrefab, canvas.transform);
        var persona = (GameObject)PrefabUtility.InstantiatePrefab(personaPrefab, canvas.transform);
        var settings = Children(canvas.transform).FirstOrDefault(c => c.name == "SettingsPanel");
        if (settings != null)
        {
            collection.transform.SetSiblingIndex(settings.GetSiblingIndex());
            persona.transform.SetSiblingIndex(settings.GetSiblingIndex());
        }

        var homeView = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<HomeView>(true)).First(v => v != null);
        Wire(homeView, ("progressText", progress), ("collectionView", collection.GetComponent<MemoryCollectionView>()), ("personaView", persona.GetComponent<PersonaView>()));

        foreach (var button in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Button>(true)).Where(b => b.name is "CardLibButton" or "DeckButton"))
        {
            // プレハブインスタンスの上書き値はプロパティ経由では保存されないため SerializedObject で書く
            var bso = new SerializedObject(button);
            bso.FindProperty("m_Interactable").boolValue = true;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            var lso = new SerializedObject(label);
            lso.FindProperty("m_text").stringValue = button.name == "DeckButton" ? "人格" : "記憶コレクション";
            lso.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AuctionUiBuilder] ロビー UI 生成完了");
    }

    private static void RegisterSceneInBuild()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != AUCTION_SCENE).ToList();
        var novelIndex = scenes.FindIndex(s => s.path.EndsWith("NovelKitScene.unity"));
        scenes.Insert(novelIndex < 0 ? scenes.Count : novelIndex, new EditorBuildSettingsScene(AUCTION_SCENE, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject BuildParticipantIcon()
    {
        var root = Rect("ParticipantIcon", null, new Vector2(150, 170));
        var bg = Image(root, "Background", new Color(0.06f, 0.03f, 0.05f, 0.55f));
        Stretch(bg);
        // 枠は所属色の下線として使う (塗りつぶすと立ち絵が読めなくなる)
        var frame = Image(root, "Frame", Color.white);
        Place(frame.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 4), new Vector2(150, 5));
        var icon = Image(root, "Icon", Color.white);
        Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(96, 96));
        var nameText = Text(root, "NameText", "名前", 19, TextAlignmentOptions.Center);
        Place(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -118), new Vector2(150, 26));
        var resourceText = Text(root, "ResourceText", "40", 20, TextAlignmentOptions.Center);
        Place(resourceText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(150, 26));
        var bidText = Text(root, "BidText", "", 30, TextAlignmentOptions.Center);
        bidText.color = new Color(1f, 0.88f, 0.5f);
        Place(bidText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(150, 40));
        var winner = Text(root, "WinnerMark", "WINNER", 20, TextAlignmentOptions.Center);
        winner.color = new Color(1f, 0.45f, 0.35f);
        Place(winner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(150, 28));
        winner.gameObject.SetActive(false);
        var outMark = Text(root, "OutMark", "OUT", 24, TextAlignmentOptions.Center);
        outMark.color = new Color(0.65f, 0.65f, 0.65f);
        Place(outMark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 30));
        outMark.gameObject.SetActive(false);
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var view = root.AddComponent<ParticipantIconView>();
        Wire(view, ("icon", icon), ("frame", frame), ("nameText", nameText), ("resourceText", resourceText), ("bidText", bidText),
            ("winnerMark", winner.gameObject), ("outMark", outMark.gameObject), ("selectButton", button));
        return SavePrefab(root, "ParticipantIcon");
    }

    private static GameObject BuildParticipantBar(GameObject canvas)
    {
        var bar = Rect("ParticipantBar", canvas, new Vector2(820, 175));
        Place(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 96), new Vector2(820, 175));
        var layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return bar;
    }

    private static BaptismView BuildBaptism(GameObject canvas)
    {
        var root = Window(canvas, "BaptismView", "洗礼", out var close, showClose: false);
        var header = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "TitleText");
        var container = Rect("EntryContainer", root, new Vector2(720, 360));
        Place(container.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(720, 360));
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var remaining = Text(root, "RemainingText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(remaining.rectTransform, new Vector2(0.5f, 0f), new Vector2(-180, 118), new Vector2(360, 26));
        var collapsed = Text(root, "CollapsedText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(collapsed.rectTransform, new Vector2(0.5f, 0f), new Vector2(-180, 90), new Vector2(360, 26));
        var selected = Text(root, "SelectedText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(selected.rectTransform, new Vector2(0.5f, 0f), new Vector2(-180, 62), new Vector2(360, 26));
        var finish = ButtonPrefab(root, "FinishButton", "洗礼を受ける", new Vector2(200, 44));
        Place(finish.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(210, 84), new Vector2(200, 44));

        var view = root.AddComponent<BaptismView>();
        Wire(view, ("headerText", header), ("entryContainer", container.transform), ("entryPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/WonLotEntry.prefab")),
            ("remainingText", remaining), ("collapsedText", collapsed), ("selectedText", selected), ("finishButton", finish));
        return view;
    }

    private static GameOverView BuildGameOver(GameObject canvas)
    {
        var root = Window(canvas, "GameOverView", "", out _, showClose: false);
        var message = Text(root, "MessageText", "", 26, TextAlignmentOptions.Center);
        Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(700, 120));
        var retry = ButtonPrefab(root, "RetryButton", "やり直す", new Vector2(200, 44));
        Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-120, -50), new Vector2(200, 44));
        var lobby = ButtonPrefab(root, "LobbyButton", "ロビーへ", new Vector2(200, 44));
        Place(lobby.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(120, -50), new Vector2(200, 44));
        var view = root.AddComponent<GameOverView>();
        Wire(view, ("messageText", message), ("retryButton", retry), ("lobbyButton", lobby));
        return view;
    }

    private static GameObject BuildWonLotEntry()
    {
        var root = Rect("WonLotEntry", null, new Vector2(700, 84));
        var frame = Image(root, "Frame", Color.white);
        Stretch(frame);
        var inner = Image(root, "Inner", new Color(0.12f, 0.08f, 0.1f, 0.95f));
        Stretch(inner, 2);
        var title = Text(root, "TitleText", "ロット", 19, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(300, -18), new Vector2(560, 26));
        var detail = Text(root, "DetailText", "", 14, TextAlignmentOptions.TopLeft);
        Place(detail.rectTransform, new Vector2(0f, 1f), new Vector2(300, -54), new Vector2(560, 44));
        var distortion = Text(root, "DistortionText", "歪み", 17, TextAlignmentOptions.Center);
        Place(distortion.rectTransform, new Vector2(1f, 1f), new Vector2(-70, -20), new Vector2(120, 26));
        var integrate = ButtonPrefab(root, "IntegrateButton", "統合する", new Vector2(120, 34));
        Place(integrate.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-70, 24), new Vector2(120, 34));
        var view = root.AddComponent<WonLotEntryView>();
        Wire(view, ("titleText", title), ("detailText", detail), ("distortionText", distortion), ("integrateButton", integrate), ("frame", frame));
        return SavePrefab(root, "WonLotEntry");
    }

    private static GameObject BuildCollectionEntry()
    {
        var root = Rect("MemoryCollectionEntry", null, new Vector2(640, 56));
        var bg = Image(root, "Background", new Color(0.12f, 0.08f, 0.1f, 0.9f));
        Stretch(bg);
        var bar = Image(root, "ColorBar", Color.white);
        Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(10, 46));
        var title = Text(root, "TitleText", "", 17, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(290, -14), new Vector2(520, 24));
        var flavor = Text(root, "FlavorText", "", 13, TextAlignmentOptions.MidlineLeft);
        Place(flavor.rectTransform, new Vector2(0f, 0f), new Vector2(290, 14), new Vector2(520, 22));
        var mark = Text(root, "MarkText", "", 16, TextAlignmentOptions.Center);
        Place(mark.rectTransform, new Vector2(1f, 0.5f), new Vector2(-40, 0), new Vector2(60, 24));
        var view = root.AddComponent<MemoryCollectionEntryView>();
        Wire(view, ("colorBar", bar), ("titleText", title), ("flavorText", flavor), ("markText", mark));
        return SavePrefab(root, "MemoryCollectionEntry", LOBBY_PREFAB_DIR);
    }

    private static GameObject BuildCollectionWindow(GameObject entryPrefab)
    {
        var root = Window(null, "MemoryCollectionView", "記憶コレクション", out var close);
        var scroll = Rect("Scroll", root, new Vector2(660, 400));
        Place(scroll.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(660, 400));
        var scrollRect = scroll.AddComponent<ScrollRect>();
        scroll.AddComponent<RectMask2D>();
        var content = Rect("Content", scroll, new Vector2(660, 0));
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = crt;
        scrollRect.horizontal = false;
        scrollRect.viewport = scroll.GetComponent<RectTransform>();
        var summary = Text(root, "SummaryText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(summary.rectTransform, new Vector2(0f, 1f), new Vector2(200, -60), new Vector2(300, 26));
        var view = root.AddComponent<MemoryCollectionView>();
        Wire(view, ("closeButton", close), ("entryContainer", content.transform), ("entryPrefab", entryPrefab), ("summaryText", summary));
        return SavePrefab(root, "MemoryCollectionView", LOBBY_PREFAB_DIR);
    }

    private static GameObject BuildPersonaWindow()
    {
        var root = Window(null, "PersonaView", "人格", out var close);
        var integrated = Text(root, "IntegratedText", "", 18, TextAlignmentOptions.TopLeft);
        Place(integrated.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(640, 200));
        var wallet = Text(root, "WalletText", "", 16, TextAlignmentOptions.MidlineLeft);
        Place(wallet.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(640, 40));
        var collapsed = Text(root, "CollapsedText", "", 16, TextAlignmentOptions.MidlineLeft);
        Place(collapsed.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(640, 30));
        var view = root.AddComponent<PersonaView>();
        Wire(view, ("closeButton", close), ("integratedText", integrated), ("walletText", wallet), ("collapsedText", collapsed));
        return SavePrefab(root, "PersonaView", LOBBY_PREFAB_DIR);
    }

    // ---- 部品 ----

    private static void LoadFont()
    {
        _font = AssetDatabase.LoadAssetAtPath<GameObject>(NORMAL_BUTTON).GetComponentInChildren<TextMeshProUGUI>(true).font;
    }

    private static GameObject InstantiatePrefab(string path, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab) throw new InvalidOperationException($"プレハブが無い: {path}");
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
    }

    private static T Find<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
    {
        var found = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
        if (!found) throw new InvalidOperationException($"{typeof(T).Name} がシーンに無い");
        return found;
    }

    /// <summary>全画面の暗幕 + 中央パネル + 見出し (+ 閉じるボタン) を持つウィンドウ</summary>
    private static GameObject Window(GameObject parent, string name, string title, out Button close, bool showClose = true)
    {
        var root = Rect(name, parent, Vector2.zero);
        Stretch(root.GetComponent<RectTransform>());
        root.AddComponent<CanvasGroup>();
        var dim = Image(root, "Dim", OVERLAY_BG);
        Stretch(dim);
        dim.raycastTarget = true;
        Panel(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 540));
        var header = Text(root, "TitleText", title, 24, TextAlignmentOptions.Center);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(700, 60));
        close = null;
        if (showClose)
        {
            close = ButtonPrefab(root, $"{name}CloseButton", "閉じる", new Vector2(140, 38));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(140, 38));
        }
        return root;
    }

    private static GameObject Rect(string name, GameObject parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        return go;
    }

    private static GameObject Panel(GameObject parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color? bg = null)
    {
        var go = Rect(name, parent, size);
        Place(go.GetComponent<RectTransform>(), anchor, pos, size);
        var image = go.AddComponent<Image>();
        image.color = bg ?? PANEL_BG;
        return go;
    }

    private static Image Image(GameObject parent, string name, Color color)
    {
        var go = Rect(name, parent, new Vector2(100, 100));
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Text(GameObject parent, string name, string text, float size, TextAlignmentOptions alignment)
    {
        var go = Rect(name, parent, new Vector2(200, 30));
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button ButtonPrefab(GameObject parent, string name, string label, Vector2 size)
    {
        var go = InstantiatePrefab(NORMAL_BUTTON, parent.transform);
        go.name = name;
        go.GetComponent<RectTransform>().sizeDelta = size;
        go.GetComponentInChildren<TextMeshProUGUI>(true).text = label;
        return go.GetComponent<Button>();
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(Graphic g, float margin = 0)
    {
        Stretch(g.rectTransform, margin);
    }

    private static void Stretch(RectTransform rt, float margin = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    private static void Wire(Component target, params (string field, UnityEngine.Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
        {
            var prop = so.FindProperty(field);
            if (prop == null) throw new InvalidOperationException($"{target.GetType().Name} にフィールド {field} が無い (先にコンパイルが必要)");
            prop.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject SavePrefab(GameObject go, string name, string dir = PREFAB_DIR)
    {
        EnsureDir(dir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static IEnumerable<Transform> Children(Transform t)
    {
        for (var i = 0; i < t.childCount; i++) yield return t.GetChild(i);
    }
}
