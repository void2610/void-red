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
    private const string DIALOGUE_PORTRAIT_PREFAB = "Assets/Prefabs/NewBattleSceneView/DialoguePhase/DialoguePortraitView.prefab";
    private const string DIALOGUE_CHOICES_PREFAB = "Assets/Prefabs/NewBattleSceneView/DialoguePhase/DialogueChoicesView.prefab";
    private const string ACQUISITION_PREFAB = "Assets/Prefabs/NewBattleSceneView/RewardPhase/CardAcquisitionView.prefab";
    private const string ACQUIRED_TEXT_PREFAB = "Assets/Prefabs/NewBattleSceneView/RewardPhase/AcquiredCardTextView.prefab";

    // 旧ルール専用で、新オークションでは使わないプレハブインスタンス
    private static readonly string[] OBSOLETE_OBJECTS =
    {
        "CardBattleView", "DeckSelectionView", "RewardPhaseView", "MemoryGrowthView", "SkillButtonView", "TutorialView", "TutorialGizmoHelper",
        // 立ち絵は対話 View に一本化したので、旧バトルの敵立ち絵は使わない
        "EnemyView",
        // 透明なまま blocksRaycasts が立ちっぱなしで全画面のクリックを奪う。オークションにポーズは無い
        "BattlePauseView",
    };

    // 対話コマンドのボタン (DialogueCommand の順) に振るラベルとアイコン
    private static readonly (string label, string sprite)[] DIALOGUE_COMMANDS =
    {
        ("観察する", "Assets/Sprites/Auction/Dialogue/silence.png"),
        ("挑発する", "Assets/Sprites/Auction/Dialogue/provoke.png"),
        ("共感する", "Assets/Sprites/Auction/Dialogue/empathize.png"),
        ("説得する", "Assets/Sprites/Auction/Dialogue/persuade.png"),
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
        BuildDialoguePhasePrefab();
        BuildParticipantIcon();
        BuildBaptism();
        BuildGameOver();
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

        // 旧シーンのインスタンスは構造の上書きを持ち、プレハブ側の組み替えが反映されない。作り直す
        foreach (var stale in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DialoguePhaseView>(true)).ToList())
        {
            UnityEngine.Object.DestroyImmediate(stale.gameObject);
        }
        var dialogue = InstantiatePrefab(DIALOGUE_PHASE_PREFAB, canvas.transform);
        var participantBar = BuildParticipantBar(canvas);
        var baptism = InstantiatePrefab($"{PREFAB_DIR}/BaptismView.prefab", canvas.transform).GetComponent<BaptismView>();
        var gameOver = InstantiatePrefab($"{PREFAB_DIR}/GameOverView.prefab", canvas.transform).GetComponent<GameOverView>();

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
            ("playerPortrait", LoadSprite("Assets/Sprites/Character/Protagonist/Protagonist_default.png")),
            ("participantBar", participantBar.transform),
            ("participantIconPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ParticipantIcon.prefab").GetComponent<ParticipantIconView>()));

        var auctionView = Find<AuctionView>(scene);
        RenameConfirmButton(auctionView);
        ApplyAuctionLayout(auctionView, participantBar);

        ApplyCompetitionLayout(Find<CompetitionView>(scene));
        ApplyDrawOrder(canvas, auctionView, dialogue, participantBar, baptism, gameOver);

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
        Place(progress.rectTransform, new Vector2(0f, 1f), new Vector2(275, -44), new Vector2(440, 28));
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

    /// <summary>
    /// 対話フェーズのプレハブを新ルール用に組み替える
    /// 旧構成では選択肢が立ち絵の子で、立ち絵を動かすと一緒に動いてしまうため親を付け替える
    /// (インスタンス側では親の付け替えが保存できないのでプレハブ本体を編集する)
    /// </summary>
    private static void BuildDialoguePhasePrefab()
    {
        // 選択肢は旧構成で立ち絵プレハブの内部に入っている。立ち絵を動かすと一緒に動くので中から出す
        var portraitRoot = PrefabUtility.LoadPrefabContents(DIALOGUE_PORTRAIT_PREFAB);
        try
        {
            foreach (var nested in portraitRoot.GetComponentsInChildren<DialogueChoicesView>(true).ToList()) UnityEngine.Object.DestroyImmediate(nested.gameObject);
            PrefabUtility.SaveAsPrefabAsset(portraitRoot, DIALOGUE_PORTRAIT_PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(portraitRoot);
        }

        var root = PrefabUtility.LoadPrefabContents(DIALOGUE_PHASE_PREFAB);
        try
        {
            var portrait = root.GetComponentInChildren<DialoguePortraitView>(true);
            var cutIn = root.GetComponentInChildren<DialogueCutInView>(true);

            // 立ち絵は右端に小さく置き、参加者バーに被らせない (Canvas の参照解像度は 800x600)
            var pso = new SerializedObject(portrait);
            pso.FindProperty("hiddenX").floatValue = 520f;
            pso.FindProperty("shownX").floatValue = 250f;
            pso.ApplyModifiedPropertiesWithoutUndo();
            SetRect(portrait.transform, new Vector2(250, -70), 0.2f);

            // 立ち絵の背後にある旧演出の黒板は使わない
            var portraitBack = portrait.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "PlayerBack");
            if (portraitBack) portraitBack.gameObject.SetActive(false);

            // 立ち絵は見せるだけ。クリックを受けると入札ウィンドウのボタンを覆ってしまう
            foreach (var img in portrait.GetComponentsInChildren<Image>(true))
            {
                var iso = new SerializedObject(img);
                iso.FindProperty("m_RaycastTarget").boolValue = false;
                iso.ApplyModifiedPropertiesWithoutUndo();
            }

            // カットインは再生時だけ出す
            cutIn.gameObject.SetActive(false);
            AddDialogueTextBackdrop(cutIn);

            // 対話中の暗幕。全画面に伸ばして薄くし、背景を潰さない
            var back = root.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "Back");
            if (back)
            {
                var bso = new SerializedObject(back);
                bso.FindProperty("m_Color.a").floatValue = 0.35f;
                bso.ApplyModifiedPropertiesWithoutUndo();
                var rso = new SerializedObject(back.rectTransform);
                rso.FindProperty("m_AnchorMin.x").floatValue = 0f;
                rso.FindProperty("m_AnchorMin.y").floatValue = 0f;
                rso.FindProperty("m_AnchorMax.x").floatValue = 1f;
                rso.FindProperty("m_AnchorMax.y").floatValue = 1f;
                rso.FindProperty("m_SizeDelta.x").floatValue = 0f;
                rso.FindProperty("m_SizeDelta.y").floatValue = 0f;
                rso.FindProperty("m_AnchoredPosition.x").floatValue = 0f;
                rso.FindProperty("m_AnchoredPosition.y").floatValue = 0f;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }

            var choices = root.GetComponentsInChildren<DialogueChoicesView>(true).FirstOrDefault();
            if (!choices)
            {
                var instance = InstantiatePrefab(DIALOGUE_CHOICES_PREFAB, root.transform);
                choices = instance.GetComponent<DialogueChoicesView>();
            }
            SetRect(choices.transform, new Vector2(-295, 35), 0.55f);
            SetRect(choices.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Buttons"), Vector2.zero, 1f);
            ApplyDialogueCommands(choices);
            OrderChoiceButtons(choices);

            var so = new SerializedObject(root.GetComponent<DialoguePhaseView>());
            so.FindProperty("choicesView").objectReferenceValue = choices;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DIALOGUE_PHASE_PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// 画面配置のラフに寄せる: 中央にロット、左下に感情ホイール、右下に確定、下端に参加者
    /// 位置はここで一括管理する (プレハブを直接いじらない)
    /// </summary>
    /// <summary>
    /// 操作系 (ホイール / 対話コマンド / 確定ボタン) を画面左に集め、立ち絵と参加者バーに重ねない (ラフ ui-bid-screen 準拠)
    /// </summary>
    private static void ApplyAuctionLayout(AuctionView auction, GameObject participantBar)
    {
        var so = new SerializedObject(auction);
        SetRect(so.FindProperty("cardContainer").objectReferenceValue as Transform, new Vector2(-40, 70), 1.1f);
        SetRect((so.FindProperty("emotionResourceDisplayView").objectReferenceValue as Component)?.transform, new Vector2(-430, -110), 0.42f);
        SetRect((so.FindProperty("confirmBiddingButton").objectReferenceValue as Component)?.transform, new Vector2(0, -138), 0.85f);
        SetRect((so.FindProperty("bidWindowView").objectReferenceValue as Component)?.transform, new Vector2(75, 75), 0.92f);
        SetRect(participantBar.transform, new Vector2(0, 78), 1f);

        // 入札ウィンドウの「外側をクリックで閉じる」全画面ボタンが、感情ホイールと +/- のクリックを奪う
        foreach (var back in auction.GetComponentsInChildren<Button>(true).Where(b => b.name == "BackButton"))
        {
            back.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 重なり順を明示する。奥から 背景 → 場 → 対話 → 参加者 → 競合 → 告知 → 全画面窓
    /// </summary>
    private static void ApplyDrawOrder(GameObject canvas, AuctionView auction, GameObject dialogue, GameObject participantBar, BaptismView baptism, GameOverView gameOver)
    {
        var order = new List<Transform>
        {
            auction.transform,
            dialogue.transform,
            participantBar.transform,
            canvas.GetComponentInChildren<CompetitionView>(true)?.transform,
            canvas.GetComponentInChildren<AnnouncementView>(true)?.transform,
            baptism.transform,
            gameOver.transform,
        };
        foreach (var t in order.Where(t => t)) t.SetAsLastSibling();

        // 入札ウィンドウは場の中で最前面に出す
        var bidWindow = new SerializedObject(auction).FindProperty("bidWindowView").objectReferenceValue as Component;
        bidWindow?.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 競合画面: ホイールを画面内に収め、左右の立ち絵を競合者として配線する
    /// </summary>
    private static void ApplyCompetitionLayout(CompetitionView competition)
    {
        var so = new SerializedObject(competition);
        SetRect((so.FindProperty("emotionResourceDisplayView").objectReferenceValue as Component)?.transform, new Vector2(0, -170), 0.42f);

        // タイマーは見出しのリボンに重ならないよう右へ寄せる
        SetRect((so.FindProperty("timerImage").objectReferenceValue as Component)?.transform, new Vector2(-430, 245), 0.9f);

        // 見出しはリボンの装飾に埋もれるため一段下げる
        SetRect((so.FindProperty("instructionText").objectReferenceValue as Component)?.transform, new Vector2(0, -20), 1f);

        // 検証や配線から引けるよう、上乗せボタンに名前を付ける
        var raise = so.FindProperty("raiseButton").objectReferenceValue as Button;
        if (raise) raise.gameObject.name = "RaiseButton";

        // 天秤の裏に隠れると押せることが分からないため、ホイールの隣に出す
        SetRect(raise?.transform, new Vector2(-235, -168), 0.28f);

        var portraits = competition.GetComponentsInChildren<Image>(true).Where(i => i.name is "Player" or "Enemy").ToList();
        so.FindProperty("playerPortrait").objectReferenceValue = portraits.FirstOrDefault(i => i.name == "Player");
        so.FindProperty("rivalPortrait").objectReferenceValue = portraits.FirstOrDefault(i => i.name == "Enemy");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRect(Transform target, Vector2 position, float scale)
    {
        if (!target) return;
        var so = new SerializedObject((RectTransform)target);
        so.FindProperty("m_AnchoredPosition.x").floatValue = position.x;
        so.FindProperty("m_AnchoredPosition.y").floatValue = position.y;
        so.FindProperty("m_LocalScale.x").floatValue = scale;
        so.FindProperty("m_LocalScale.y").floatValue = scale;
        so.FindProperty("m_LocalScale.z").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 入札確定ボタンを検証や配線から引きやすい名前にする
    /// </summary>
    private static void RenameConfirmButton(AuctionView auction)
    {
        var so = new SerializedObject(auction);
        var button = so.FindProperty("confirmBiddingButton").objectReferenceValue as Button;
        if (!button) throw new InvalidOperationException("AuctionView の confirmBiddingButton が未設定");
        button.gameObject.name = "ConfirmButton";
    }

    /// <summary>
    /// 旧リザルトの文言を洗礼のものに差し替え、進行用の「次へ」は洗礼ボタンに一本化する
    /// </summary>
    private static void ApplyAcquisitionTexts(CardAcquisitionView acquisition)
    {
        foreach (var text in acquisition.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            var so = new SerializedObject(text);
            var current = so.FindProperty("m_text");
            if (current.stringValue.Contains("獲得カード")) current.stringValue = "落札した記憶";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var next = acquisition.GetComponentsInChildren<Button>(true).FirstOrDefault();
        if (next) next.gameObject.SetActive(false);
    }

    /// <summary>
    /// 洗礼の内訳は 3 行 (競売番号 / 入札と歪み / 他の入札) 出すため、行の高さと文字を詰める
    /// </summary>
    private static void ApplyAcquisitionEntryLayout()
    {
        var root = PrefabUtility.LoadPrefabContents(ACQUIRED_TEXT_PREFAB);
        try
        {
            // 内訳は 3 行あるので、1 件分の高さを確保しないと隣の札の説明と重なって読めなくなる
            var rso = new SerializedObject(root.GetComponent<RectTransform>());
            rso.FindProperty("m_SizeDelta.x").floatValue = 300f;
            rso.FindProperty("m_SizeDelta.y").floatValue = 170f;
            rso.ApplyModifiedPropertiesWithoutUndo();

            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var tso = new SerializedObject(text);
                tso.FindProperty("m_fontSize").floatValue = 15f;
                tso.FindProperty("m_fontSizeBase").floatValue = 15f;
                tso.FindProperty("m_enableAutoSizing").boolValue = false;
                tso.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, ACQUIRED_TEXT_PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        ApplyAcquisitionTextGrid();
    }

    /// <summary>
    /// 内訳を札と同じ 3 列に並べ、1 件ずつ札の真下に来るようにする
    /// </summary>
    private static void ApplyAcquisitionTextGrid()
    {
        var root = PrefabUtility.LoadPrefabContents(ACQUISITION_PREFAB);
        try
        {
            var container = root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "TextContainer");
            if (!container) return;

            var grid = container.GetComponent<GridLayoutGroup>();
            if (grid)
            {
                var gso = new SerializedObject(grid);
                gso.FindProperty("m_CellSize.x").floatValue = 310f;
                gso.FindProperty("m_CellSize.y").floatValue = 175f;
                gso.FindProperty("m_Constraint").enumValueIndex = (int)GridLayoutGroup.Constraint.FixedColumnCount;
                gso.FindProperty("m_ConstraintCount").intValue = 3;
                gso.FindProperty("m_ChildAlignment").enumValueIndex = (int)TextAnchor.UpperCenter;
                gso.ApplyModifiedPropertiesWithoutUndo();
            }

            var cso = new SerializedObject(container);
            cso.FindProperty("m_AnchoredPosition.x").floatValue = 0f;
            cso.FindProperty("m_AnchoredPosition.y").floatValue = -215f;
            cso.FindProperty("m_SizeDelta.x").floatValue = 960f;
            cso.FindProperty("m_SizeDelta.y").floatValue = 180f;
            cso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ACQUISITION_PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// セリフは背景の壁と同系色で潰れるので、文字の後ろに帯を敷く (カットインと一緒に動くよう同じ親に置く)
    /// </summary>
    private static void AddDialogueTextBackdrop(DialogueCutInView cutIn)
    {
        var text = cutIn.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "DialogueText");
        if (!text) return;

        var existing = text.transform.parent.Find("DialogueTextBackdrop");
        if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        var backdrop = new GameObject("DialogueTextBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(text.transform.parent, false);
        backdrop.transform.SetSiblingIndex(text.transform.GetSiblingIndex());

        var image = backdrop.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = false;

        var rt = backdrop.GetComponent<RectTransform>();
        rt.anchorMin = text.rectTransform.anchorMin;
        rt.anchorMax = text.rectTransform.anchorMax;
        rt.pivot = text.rectTransform.pivot;
        rt.anchoredPosition = text.rectTransform.anchoredPosition;
        rt.sizeDelta = text.rectTransform.sizeDelta + new Vector2(60f, -30f);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// 対話コマンドのボタンを DialogueCommand の並び順に揃え、ラベルとアイコンを設定する
    /// </summary>
    /// <summary>
    /// 画面上の縦並びも DialogueCommand の順にする
    /// </summary>
    private static void OrderChoiceButtons(DialogueChoicesView choices)
    {
        var so = new SerializedObject(choices);
        var list = so.FindProperty("choiceButtons");
        var buttons = Enumerable.Range(0, list.arraySize).Select(i => (Button)list.GetArrayElementAtIndex(i).objectReferenceValue).ToList();

        // 既存の縦位置を上から順に取り直し、DialogueCommand の並びに割り当てる
        var slots = buttons.Select(b => b.GetComponent<RectTransform>().anchoredPosition).OrderByDescending(p => p.y).ToList();
        for (var i = 0; i < buttons.Count; i++)
        {
            buttons[i].transform.SetSiblingIndex(i);
            var rso = new SerializedObject(buttons[i].GetComponent<RectTransform>());
            rso.FindProperty("m_AnchoredPosition.x").floatValue = slots[i].x;
            rso.FindProperty("m_AnchoredPosition.y").floatValue = slots[i].y;
            rso.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ApplyDialogueCommands(DialogueChoicesView choices)
    {
        var buttons = choices.GetComponentsInChildren<Button>(true).OrderBy(b => b.name).ToList();
        if (buttons.Count != DIALOGUE_COMMANDS.Length) throw new InvalidOperationException($"対話ボタンの数が合わない: {buttons.Count}");

        // 旧レイアウトは 挑発 / 共感 / 説得 / 沈黙 の順。沈黙のボタンを観察に読み替えて先頭へ回す
        var ordered = new List<Button> { buttons[3], buttons[0], buttons[1], buttons[2] };
        for (var i = 0; i < ordered.Count; i++)
        {
            var (label, spritePath) = DIALOGUE_COMMANDS[i];
            ordered[i].name = $"DialogueChoice_{(DialogueCommand)i}";
            var text = ordered[i].GetComponentInChildren<TextMeshProUGUI>(true);
            var tso = new SerializedObject(text);
            tso.FindProperty("m_text").stringValue = label;
            tso.ApplyModifiedPropertiesWithoutUndo();
            var background = ordered[i].GetComponentsInChildren<Image>(true).FirstOrDefault(img => img.name == "Background");
            var sprite = LoadSprite(spritePath);
            if (background && sprite)
            {
                var iso = new SerializedObject(background);
                iso.FindProperty("m_Sprite").objectReferenceValue = sprite;
                iso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        var so = new SerializedObject(choices);
        var list = so.FindProperty("choiceButtons");
        list.arraySize = ordered.Count;
        for (var i = 0; i < ordered.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
        so.ApplyModifiedPropertiesWithoutUndo();
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
        var root = Rect("ParticipantIcon", null, new Vector2(128, 128));
        var bg = Image(root, "Background", new Color(0.06f, 0.03f, 0.05f, 0.55f));
        Stretch(bg);
        // 対話相手を選ぶ当たり判定になるので、この Image だけはクリックを受ける
        bg.raycastTarget = true;
        // 枠は所属色の下線として使う (塗りつぶすと立ち絵が読めなくなる)
        var frame = Image(root, "Frame", Color.white);
        Place(frame.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 3), new Vector2(128, 4));
        var icon = Image(root, "Icon", Color.white);
        Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -42), new Vector2(70, 70));
        var nameText = Text(root, "NameText", "名前", 19, TextAlignmentOptions.Center);
        Place(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(128, 22));
        var resourceText = Text(root, "ResourceText", "40", 20, TextAlignmentOptions.Center);
        Place(resourceText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(128, 24));
        var bidText = Text(root, "BidText", "", 30, TextAlignmentOptions.Center);
        bidText.color = new Color(1f, 0.88f, 0.5f);
        // アイコンの絵柄に重ねると数字が読めないので、枠の上に浮かせる
        Place(bidText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 42), new Vector2(128, 40));
        var winner = Text(root, "WinnerMark", "WINNER", 20, TextAlignmentOptions.Center);
        winner.color = new Color(1f, 0.45f, 0.35f);
        Place(winner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 12), new Vector2(128, 26));
        winner.gameObject.SetActive(false);
        var outMark = Text(root, "OutMark", "OUT", 24, TextAlignmentOptions.Center);
        outMark.color = new Color(1f, 0.35f, 0.35f);
        Place(outMark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128, 28));
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
        Place(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 78), new Vector2(700, 130));
        var layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return bar;
    }

    private static GameObject BuildBaptism()
    {
        var root = Window(null, "BaptismView", "洗礼", out _, showClose: false);
        root.AddComponent<CanvasGroup>();
        // 窓の論理サイズは 1067x600。鮮明化後テーマまで入るので幅を取り、折り返さない大きさにする
        var header = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "TitleText");
        // 背景画像の上端に「オークション結果」が焼き込まれているので、その下に置く
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(90, -58), new Vector2(760, 44));
        var hso = new SerializedObject(header);
        hso.FindProperty("m_fontSize").floatValue = 18f;
        hso.FindProperty("m_fontSizeBase").floatValue = 18f;
        hso.ApplyModifiedPropertiesWithoutUndo();

        // 落札した記憶の一覧は旧リザルトの演出 (札 + 内訳のスタガー表示) を流用する
        var acquisition = InstantiatePrefab(ACQUISITION_PREFAB, root.transform).GetComponent<CardAcquisitionView>();
        SetRect(acquisition.transform, new Vector2(0, 55), 1f);
        ApplyAcquisitionTexts(acquisition);
        ApplyAcquisitionEntryLayout();

        // 窓は 1067x600 しかないので、内訳の並び (中央) を避けて上下の端に置く
        // 内訳の並びが窓の下半分を占めるので、状況説明は札より上に置く
        var selected = Text(root, "SelectedText", "", 18, TextAlignmentOptions.MidlineRight);
        Place(selected.rectTransform, new Vector2(0.5f, 1f), new Vector2(300, -118), new Vector2(440, 26));
        var remaining = Text(root, "RemainingText", "", 16, TextAlignmentOptions.MidlineLeft);
        Place(remaining.rectTransform, new Vector2(0.5f, 1f), new Vector2(-190, -110), new Vector2(300, 22));
        var collapsed = Text(root, "CollapsedText", "", 16, TextAlignmentOptions.MidlineLeft);
        Place(collapsed.rectTransform, new Vector2(0.5f, 1f), new Vector2(-110, -132), new Vector2(560, 22));
        var finish = ButtonPrefab(root, "FinishButton", "洗礼を受ける", new Vector2(200, 44));
        Place(finish.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(390, 28), new Vector2(200, 44));

        // 見出しは札の一覧より後ろに回ると隠れるので最前面に出す
        header.transform.SetAsLastSibling();

        var view = root.AddComponent<BaptismView>();
        Wire(view, ("headerText", header), ("remainingText", remaining), ("collapsedText", collapsed), ("selectedText", selected),
            ("acquisitionView", acquisition), ("finishButton", finish));
        return SavePrefab(root, "BaptismView");
    }

    private static GameObject BuildGameOver()
    {
        var root = Window(null, "GameOverView", "", out _, showClose: false);
        var message = Text(root, "MessageText", "", 26, TextAlignmentOptions.Center);
        Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(700, 120));
        var retry = ButtonPrefab(root, "RetryButton", "やり直す", new Vector2(200, 44));
        Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-120, -50), new Vector2(200, 44));
        var lobby = ButtonPrefab(root, "LobbyButton", "ロビーへ", new Vector2(200, 44));
        Place(lobby.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(120, -50), new Vector2(200, 44));
        var view = root.AddComponent<GameOverView>();
        Wire(view, ("messageText", message), ("retryButton", retry), ("lobbyButton", lobby));
        return SavePrefab(root, "GameOverView");
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
        // 窓の左上はホームの進行案内と重なるので、見出しの真下に置く
        var summary = Text(root, "SummaryText", "", 18, TextAlignmentOptions.Center);
        Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(300, 26));
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
