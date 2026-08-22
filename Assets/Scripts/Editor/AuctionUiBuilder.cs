using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// オークション画面のプレハブとシーンを組み立てるエディタツール
/// UI 階層はここで一括生成し、見た目の調整は生成後に Prefab 側で行う
/// </summary>
public static class AuctionUiBuilder
{
    private const string PREFAB_DIR = "Assets/Prefabs/AuctionSceneView";
    private const string NORMAL_BUTTON = "Assets/Prefabs/Button/NormalButton.prefab";
    private const string HOME_SCENE = "Assets/Scenes/HomeScene.unity";
    private const string AUCTION_SCENE = "Assets/Scenes/AuctionScene.unity";
    private const string ALL_FLOOR_DATA = "Assets/ScriptableObjects/Auction/AllFloorData.asset";
    private const string LOBBY_PREFAB_DIR = "Assets/Prefabs/HomeSceneView";
    private const string ROOT_SCOPE_PREFAB = "Assets/Prefabs/Root/RootLifetimeScope.prefab";

    private static readonly Color PANEL_BG = new(0.08f, 0.05f, 0.08f, 0.85f);
    private static readonly Color OVERLAY_BG = new(0.05f, 0.02f, 0.04f, 0.95f);

    private static TMP_FontAsset _font;

    [MenuItem("VoidRed/Auction/Build UI Prefabs")]
    public static void BuildPrefabs()
    {
        EnsureDir(PREFAB_DIR);
        _font = AssetDatabase.LoadAssetAtPath<GameObject>(NORMAL_BUTTON).GetComponentInChildren<TextMeshProUGUI>(true).font;

        var slot = BuildParticipantSlot();
        var bidItem = BuildEmotionBidItem();
        var wonEntry = BuildWonLotEntry();
        BuildAuctionView(slot, bidItem, wonEntry);
        AssetDatabase.SaveAssets();
        Debug.Log("[AuctionUiBuilder] プレハブ生成完了");
    }

    [MenuItem("VoidRed/Auction/Build Scene")]
    public static void BuildScene()
    {
        if (!AssetDatabase.CopyAsset(HOME_SCENE, AUCTION_SCENE) && AssetDatabase.LoadAssetAtPath<SceneAsset>(AUCTION_SCENE) == null) throw new InvalidOperationException("HomeScene の複製に失敗");
        var scene = EditorSceneManager.OpenScene(AUCTION_SCENE, OpenSceneMode.Single);

        var keep = new HashSet<string> { "Main Camera", "Global Light 2D", "Canvas", "SettingButton", "SettingsPanel", "DebugComponents", "EventSystem", "VersionText" };
        foreach (var root in scene.GetRootGameObjects().ToList())
        {
            if (root.name == "Canvas")
            {
                foreach (var child in Children(root.transform).ToList()) if (!keep.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
                continue;
            }
            if (!keep.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
        }

        var canvas = scene.GetRootGameObjects().First(g => g.name == "Canvas");
        var viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/AuctionView.prefab");
        var view = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, canvas.transform);
        view.transform.SetAsFirstSibling();

        var scopeGo = new GameObject("AuctionLifetimeScope");
        var scope = scopeGo.AddComponent<AuctionLifetimeScope>();
        var so = new SerializedObject(scope);
        so.FindProperty("allFloorData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AllFloorData>(ALL_FLOOR_DATA);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        RegisterSceneInBuild();
        Debug.Log("[AuctionUiBuilder] AuctionScene 生成完了");
    }

    [MenuItem("VoidRed/Auction/Build Lobby")]
    public static void BuildLobby()
    {
        _font = AssetDatabase.LoadAssetAtPath<GameObject>(NORMAL_BUTTON).GetComponentInChildren<TextMeshProUGUI>(true).font;

        // Root の AllFloorData 配線
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
        // 設定パネルより手前には出さない
        var settings = Children(canvas.transform).FirstOrDefault(c => c.name == "SettingsPanel");
        if (settings != null)
        {
            collection.transform.SetSiblingIndex(settings.GetSiblingIndex());
            persona.transform.SetSiblingIndex(settings.GetSiblingIndex());
        }

        var homeView = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<HomeView>(true)).First(v => v != null);
        Wire(homeView, ("progressText", progress), ("collectionView", collection.GetComponent<MemoryCollectionView>()), ("personaView", persona.GetComponent<PersonaView>()));
        // 旧デッキ / 図鑑ボタンを人格 / コレクションの入口として復活させる
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

    private static void Stretch(Graphic g, float margin = 0)
    {
        Stretch(g.rectTransform, margin);
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
        var root = Window("MemoryCollectionView", "記憶コレクション", out var close);
        var scroll = Rect("Scroll", root, new Vector2(660, 400));
        Place(scroll.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(660, 400));
        var scrollRect = scroll.AddComponent<ScrollRect>();
        var mask = scroll.AddComponent<RectMask2D>();
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
        var root = Window("PersonaView", "人格", out var close);
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

    /// <summary>
    /// 全画面の暗幕 + 中央パネル + 閉じるボタンを持つウィンドウ。BaseWindowView の CanvasGroup を付ける
    /// </summary>
    private static GameObject Window(string name, string title, out Button close)
    {
        var root = Rect(name, null, Vector2.zero);
        Stretch(root.GetComponent<RectTransform>());
        root.AddComponent<CanvasGroup>();
        var dim = Image(root, "Dim", OVERLAY_BG);
        Stretch(dim);
        dim.raycastTarget = true;
        var panel = Panel(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 520));
        var header = Text(root, "TitleText", title, 26, TextAlignmentOptions.Center);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 36));
        // CloseButton は設定パネルにもあるため、検証で名前引きできるよう個別名にする
        close = ButtonPrefab(root, $"{name}CloseButton", "閉じる", new Vector2(140, 38));
        Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(140, 38));
        return root;
    }

    private static void RegisterSceneInBuild()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => !s.path.EndsWith("BattleScene.unity") && s.path != AUCTION_SCENE).ToList();
        var novelIndex = scenes.FindIndex(s => s.path.EndsWith("NovelKitScene.unity"));
        scenes.Insert(novelIndex < 0 ? scenes.Count : novelIndex, new EditorBuildSettingsScene(AUCTION_SCENE, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject BuildParticipantSlot()
    {
        var root = Rect("ParticipantSlot", null, new Vector2(150, 190));
        var bg = Image(root, "Background", new Color(0.15f, 0.1f, 0.12f, 0.9f));
        Stretch(bg);
        var highlight = Image(root, "Highlight", new Color(1f, 0.85f, 0.3f, 0.35f));
        Stretch(highlight);
        highlight.enabled = false;
        var portrait = Image(root, "Portrait", Color.white);
        Place(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(90, 90));
        var nameText = Text(root, "NameText", "名前", 20, TextAlignmentOptions.Center);
        Place(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(150, 26));
        var resourceText = Text(root, "ResourceText", "40", 22, TextAlignmentOptions.Center);
        Place(resourceText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(150, 28));
        var bidText = Text(root, "BidText", "", 28, TextAlignmentOptions.Center);
        bidText.color = new Color(1f, 0.9f, 0.5f);
        Place(bidText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(150, 30));
        var winner = Text(root, "WinnerLabel", "WINNER", 22, TextAlignmentOptions.Center);
        winner.color = new Color(1f, 0.4f, 0.3f);
        Place(winner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(150, 28));
        winner.gameObject.SetActive(false);
        var outLabel = Text(root, "OutLabel", "OUT", 26, TextAlignmentOptions.Center);
        outLabel.color = new Color(0.6f, 0.6f, 0.6f);
        Place(outLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(150, 30));
        outLabel.gameObject.SetActive(false);
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var view = root.AddComponent<ParticipantSlotView>();
        Wire(view, ("portrait", portrait), ("nameText", nameText), ("resourceText", resourceText), ("bidText", bidText),
            ("winnerLabel", winner.gameObject), ("outLabel", outLabel.gameObject), ("highlight", highlight), ("selectButton", button));
        return SavePrefab(root, "ParticipantSlot");
    }

    private static GameObject BuildEmotionBidItem()
    {
        var root = Rect("EmotionBidItem", null, new Vector2(360, 36));
        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var bar = Image(root, "ColorBar", Color.white);
        bar.rectTransform.sizeDelta = new Vector2(14, 30);
        var nameText = Text(root, "EmotionName", "喜び", 20, TextAlignmentOptions.MidlineLeft);
        nameText.rectTransform.sizeDelta = new Vector2(70, 30);
        var owned = Text(root, "OwnedText", "5", 20, TextAlignmentOptions.Center);
        owned.rectTransform.sizeDelta = new Vector2(40, 30);
        var minus = ButtonPrefab(root, "MinusButton", "-", new Vector2(40, 32));
        var bidCount = Text(root, "BidCountText", "0", 22, TextAlignmentOptions.Center);
        bidCount.rectTransform.sizeDelta = new Vector2(40, 30);
        var plus = ButtonPrefab(root, "PlusButton", "+", new Vector2(40, 32));

        var view = root.AddComponent<EmotionBidItemView>();
        Wire(view, ("colorBar", bar), ("emotionNameText", nameText), ("ownedText", owned), ("bidCountText", bidCount), ("plusButton", plus), ("minusButton", minus));
        return SavePrefab(root, "EmotionBidItem");
    }

    private static GameObject BuildWonLotEntry()
    {
        var root = Rect("WonLotEntry", null, new Vector2(700, 90));
        var frame = Image(root, "Frame", Color.white);
        Stretch(frame);
        var inner = Image(root, "Inner", new Color(0.12f, 0.08f, 0.1f, 0.95f));
        Stretch(inner, 2);
        var title = Text(root, "TitleText", "ロット", 20, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(300, -16), new Vector2(580, 26));
        var detail = Text(root, "DetailText", "", 15, TextAlignmentOptions.TopLeft);
        Place(detail.rectTransform, new Vector2(0f, 1f), new Vector2(300, -56), new Vector2(580, 50));
        var distortion = Text(root, "DistortionText", "歪み", 18, TextAlignmentOptions.Center);
        Place(distortion.rectTransform, new Vector2(1f, 1f), new Vector2(-60, -16), new Vector2(110, 26));
        var integrate = ButtonPrefab(root, "IntegrateButton", "統合する", new Vector2(110, 34));
        Place(integrate.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-60, 24), new Vector2(110, 34));

        var view = root.AddComponent<WonLotEntryView>();
        Wire(view, ("titleText", title), ("detailText", detail), ("distortionText", distortion), ("integrateButton", integrate), ("frame", frame));
        return SavePrefab(root, "WonLotEntry");
    }

    private static void BuildAuctionView(GameObject slotPrefab, GameObject bidItemPrefab, GameObject wonEntryPrefab)
    {
        var root = Rect("AuctionView", null, Vector2.zero);
        Stretch(root.GetComponent<RectTransform>());

        var floorText = Text(root, "FloorText", "第 0 階層", 22, TextAlignmentOptions.MidlineLeft);
        Place(floorText.rectTransform, new Vector2(0f, 1f), new Vector2(110, -20), new Vector2(200, 30));
        var themeText = Text(root, "ThemeText", "記憶テーマ", 24, TextAlignmentOptions.Center);
        Place(themeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(500, 32));
        var messageText = Text(root, "MessageText", "", 20, TextAlignmentOptions.Center);
        Place(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(-120, -62), new Vector2(640, 48));

        var slotContainer = Rect("SlotContainer", root, new Vector2(780, 190));
        Place(slotContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 100), new Vector2(780, 190));
        var slotLayout = slotContainer.AddComponent<HorizontalLayoutGroup>();
        slotLayout.spacing = 6;
        slotLayout.childAlignment = TextAnchor.MiddleCenter;
        slotLayout.childControlWidth = false;
        slotLayout.childControlHeight = false;
        slotLayout.childForceExpandWidth = false;
        slotLayout.childForceExpandHeight = false;

        var lot = BuildLotView(root);
        var dialogue = BuildDialoguePanel(root);
        var counter = BuildCounterDialogue(root);
        var bid = BuildBidPanel(root, bidItemPrefab);
        var competition = BuildCompetitionPanel(root);
        var baptism = BuildBaptism(root, wonEntryPrefab);
        var gameOver = BuildGameOver(root);
        var next = ButtonPrefab(root, "NextButton", "次へ", new Vector2(140, 40));
        Place(next.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-90, 30), new Vector2(140, 40));

        var view = root.AddComponent<AuctionView>();
        Wire(view, ("floorText", floorText), ("themeText", themeText), ("messageText", messageText), ("slotContainer", slotContainer.transform), ("slotPrefab", slotPrefab),
            ("lotView", lot), ("dialoguePanel", dialogue), ("counterDialogue", counter), ("bidPanel", bid), ("competitionPanel", competition),
            ("baptismView", baptism), ("gameOverView", gameOver), ("nextButton", next));
        SavePrefab(root, "AuctionView");
    }

    private static LotView BuildLotView(GameObject parent)
    {
        var panel = Panel(parent, "LotView", new Vector2(0f, 0.5f), new Vector2(160, 60), new Vector2(280, 330));
        var image = Image(panel, "Image", Color.white);
        Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(110, 110));
        var number = Text(panel, "NumberText", "ロット 1", 18, TextAlignmentOptions.Center);
        Place(number.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(260, 24));
        var title = Text(panel, "TitleText", "『』", 22, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -172), new Vector2(260, 30));
        var flavor = Text(panel, "FlavorText", "", 15, TextAlignmentOptions.Top);
        Place(flavor.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -235), new Vector2(250, 80));
        var emotion = Text(panel, "EmotionText", "喜び", 18, TextAlignmentOptions.Center);
        Place(emotion.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(260, 26));
        var view = panel.AddComponent<LotView>();
        Wire(view, ("image", image), ("numberText", number), ("titleText", title), ("flavorText", flavor), ("emotionText", emotion));
        return view;
    }

    private static DialoguePanelView BuildDialoguePanel(GameObject parent)
    {
        var panel = Panel(parent, "DialoguePanel", new Vector2(1f, 0.5f), new Vector2(-160, 60), new Vector2(300, 330));
        var target = Text(panel, "TargetText", "対象を選んでください", 18, TextAlignmentOptions.Center);
        Place(target.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(280, 26));
        var observe = ButtonPrefab(panel, "ObserveButton", "観察する", new Vector2(130, 36));
        Place(observe.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(-70, -56), new Vector2(130, 36));
        var provoke = ButtonPrefab(panel, "ProvokeButton", "挑発する", new Vector2(130, 36));
        Place(provoke.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(70, -56), new Vector2(130, 36));
        var empathize = ButtonPrefab(panel, "EmpathizeButton", "共感する", new Vector2(130, 36));
        Place(empathize.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(-70, -98), new Vector2(130, 36));
        var persuade = ButtonPrefab(panel, "PersuadeButton", "説得する", new Vector2(130, 36));
        Place(persuade.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(70, -98), new Vector2(130, 36));
        var result = Text(panel, "ResultText", "", 15, TextAlignmentOptions.TopLeft);
        Place(result.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(270, 120));
        var toBidding = ButtonPrefab(panel, "ToBiddingButton", "入札へ", new Vector2(160, 38));
        Place(toBidding.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(160, 38));
        var view = panel.AddComponent<DialoguePanelView>();
        Wire(view, ("targetText", target), ("resultText", result), ("observeButton", observe), ("provokeButton", provoke), ("empathizeButton", empathize), ("persuadeButton", persuade), ("toBiddingButton", toBidding));
        return view;
    }

    private static CounterDialogueView BuildCounterDialogue(GameObject parent)
    {
        var panel = Panel(parent, "CounterDialogue", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(520, 220), OVERLAY_BG);
        var speaker = Text(panel, "SpeakerText", "", 20, TextAlignmentOptions.MidlineLeft);
        Place(speaker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(480, 28));
        var prompt = Text(panel, "PromptText", "", 18, TextAlignmentOptions.TopLeft);
        Place(prompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(480, 90));
        var a = ButtonPrefab(panel, "ChoiceAButton", "A", new Vector2(220, 40));
        Place(a.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-120, 30), new Vector2(220, 40));
        var b = ButtonPrefab(panel, "ChoiceBButton", "B", new Vector2(220, 40));
        Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(120, 30), new Vector2(220, 40));
        var view = panel.AddComponent<CounterDialogueView>();
        Wire(view, ("speakerText", speaker), ("promptText", prompt), ("choiceAButton", a), ("choiceBButton", b),
            ("choiceAText", a.GetComponentInChildren<TextMeshProUGUI>(true)), ("choiceBText", b.GetComponentInChildren<TextMeshProUGUI>(true)));
        return view;
    }

    private static BidPanelView BuildBidPanel(GameObject parent, GameObject itemPrefab)
    {
        var panel = Panel(parent, "BidPanel", new Vector2(1f, 0.5f), new Vector2(-200, 20), new Vector2(380, 400));
        var container = Rect("ItemContainer", panel, new Vector2(360, 320));
        Place(container.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(360, 320));
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var total = Text(panel, "TotalText", "合計 0 枚", 20, TextAlignmentOptions.Center);
        Place(total.rectTransform, new Vector2(0.5f, 0f), new Vector2(-80, 26), new Vector2(180, 30));
        var confirm = ButtonPrefab(panel, "ConfirmButton", "入札確定", new Vector2(140, 38));
        Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(100, 26), new Vector2(140, 38));
        var view = panel.AddComponent<BidPanelView>();
        Wire(view, ("itemContainer", container.transform), ("itemPrefab", itemPrefab), ("totalText", total), ("confirmButton", confirm));
        return view;
    }

    private static CompetitionPanelView BuildCompetitionPanel(GameObject parent)
    {
        var panel = Panel(parent, "CompetitionPanel", new Vector2(0.5f, 0.5f), new Vector2(-40, 40), new Vector2(300, 260));
        var title = Text(panel, "TitleText", "競合！", 22, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(280, 30));
        var totals = Text(panel, "TotalsText", "", 18, TextAlignmentOptions.TopLeft);
        Place(totals.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(260, 140));
        var timerBg = Image(panel, "TimerBg", new Color(0.3f, 0.3f, 0.3f, 0.8f));
        Place(timerBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(260, 16));
        var timer = Image(panel, "TimerFill", new Color(1f, 0.5f, 0.3f));
        Place(timer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(260, 16));
        timer.type = UnityEngine.UI.Image.Type.Filled;
        timer.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        timer.sprite = timerBg.sprite;
        var view = panel.AddComponent<CompetitionPanelView>();
        Wire(view, ("titleText", title), ("totalsText", totals), ("timerFill", timer));
        return view;
    }

    private static BaptismView BuildBaptism(GameObject parent, GameObject entryPrefab)
    {
        var panel = Panel(parent, "BaptismView", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, OVERLAY_BG);
        Stretch(panel.GetComponent<RectTransform>());
        var header = Text(panel, "HeaderText", "洗礼", 24, TextAlignmentOptions.Center);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(700, 60));
        var container = Rect("EntryContainer", panel, new Vector2(700, 380));
        Place(container.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(700, 380));
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var remaining = Text(panel, "RemainingText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(remaining.rectTransform, new Vector2(0f, 0f), new Vector2(220, 86), new Vector2(380, 26));
        var collapsed = Text(panel, "CollapsedText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(collapsed.rectTransform, new Vector2(0f, 0f), new Vector2(220, 58), new Vector2(380, 26));
        var selected = Text(panel, "SelectedText", "", 18, TextAlignmentOptions.MidlineLeft);
        Place(selected.rectTransform, new Vector2(0f, 0f), new Vector2(220, 30), new Vector2(380, 26));
        var finish = ButtonPrefab(panel, "FinishButton", "洗礼を受ける", new Vector2(180, 40));
        Place(finish.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-140, 40), new Vector2(180, 40));
        var view = panel.AddComponent<BaptismView>();
        Wire(view, ("headerText", header), ("entryContainer", container.transform), ("entryPrefab", entryPrefab), ("remainingText", remaining), ("collapsedText", collapsed), ("selectedText", selected), ("finishButton", finish));
        return view;
    }

    private static GameOverView BuildGameOver(GameObject parent)
    {
        var panel = Panel(parent, "GameOverView", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, OVERLAY_BG);
        Stretch(panel.GetComponent<RectTransform>());
        var message = Text(panel, "MessageText", "", 24, TextAlignmentOptions.Center);
        Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(600, 100));
        var retry = ButtonPrefab(panel, "RetryButton", "やり直す", new Vector2(180, 40));
        Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-110, -40), new Vector2(180, 40));
        var lobby = ButtonPrefab(panel, "LobbyButton", "ロビーへ", new Vector2(180, 40));
        Place(lobby.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(110, -40), new Vector2(180, 40));
        var view = panel.AddComponent<GameOverView>();
        Wire(view, ("messageText", message), ("retryButton", retry), ("lobbyButton", lobby));
        return view;
    }

    // ---- 部品 ----

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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NORMAL_BUTTON);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
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
        var path = $"{dir}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
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
