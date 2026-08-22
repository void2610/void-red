using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Void2610.LiminalPalette;

/// <summary>
/// 記憶オークションを LiminalPalette から起動 / 観測 / 操作するデバッグコマンド群
/// 操作は実 UI のボタンを経由し、観測はシーンローカルの AuctionSession を引いて読む
/// </summary>
public sealed class AuctionDebugCommands
{
    private readonly AuctionStartRequest _request;
    private readonly SceneTransitionManager _sceneTransition;
    private readonly GameProgressService _progress;
    private int _rememberedPlanned;
    private EmotionType _rememberedEmotion;

    public AuctionDebugCommands(AuctionStartRequest request, SceneTransitionManager sceneTransition, GameProgressService progress)
    {
        _request = request;
        _sceneTransition = sceneTransition;
        _progress = progress;
    }

    [LiminalCommand("Auction/Phase", Description = "現在のフェーズを返す")]
    public string Phase() => Session().Phase.ToString();

    [LiminalCommand("Auction/LotIndex", Description = "現在のロット番号 (0 始まり) を返す")]
    public int LotIndex() => Session().CurrentLotIndex;

    [LiminalCommand("Auction/LotTitle", Description = "現在のロット名を返す")]
    public string LotTitle() => Session().CurrentLot.Title;

    [LiminalCommand("Auction/LotEmotion", Description = "現在のロットの感情属性を返す")]
    public string LotEmotion() => Session().CurrentLot.Emotion.ToString();

    [LiminalCommand("Auction/PlayerResources", Description = "主人公の所持リソース総数を返す")]
    public int PlayerResources() => Session().Player.Wallet.Total;

    [LiminalCommand("Auction/PlayerResource", Description = "主人公の指定属性の所持枚数を返す")]
    public int PlayerResource(EmotionType emotion) => Session().Player.Wallet.Get(emotion);

    [LiminalCommand("Auction/RivalResources", Description = "ライバルの所持リソース総数を name:total のカンマ区切りで返す")]
    public string RivalResources() => string.Join(",", Session().Rivals.Select(r => $"{r.DisplayName}:{r.Wallet.Total}"));

    [LiminalCommand("Auction/PlannedBid", Description = "指定ライバルが今のロットに入れる予定の枚数を返す (検証用の覗き見)")]
    public int PlannedBid(string name) => Rival(name).PlannedBid.Total;

    [LiminalCommand("Auction/MaxPlannedBid", Description = "ライバル全員の入札予定の最大枚数を返す")]
    public int MaxPlannedBid() => Session().Rivals.Where(r => r.CanBid).Select(r => r.PlannedBid.Total).DefaultIfEmpty(0).Max();

    [LiminalCommand("Auction/CanUseDialogue", Description = "指定ライバルに対話コマンドを使えるかを返す")]
    public bool CanUseDialogue(string name, string command) => Session().CanUseDialogue(Rival(name), Enum.Parse<DialogueCommand>(command));

    [LiminalCommand("Auction/CounterPending", Description = "逆対話の二択が表示中かを返す")]
    public bool CounterPending() => View().CounterDialogue.gameObject.activeSelf;

    [LiminalCommand("Auction/Competing", Description = "競合フェーズ中かを返す")]
    public bool Competing() => Session().Phase == AuctionPhase.Competition;

    [LiminalCommand("Auction/CompetitionTotal", Description = "競合中の指定参加者の現在額を返す (主人公は ノア)")]
    public int CompetitionTotal(string name) => Session().Competition.TotalOf(Participant(name));

    [LiminalCommand("Auction/LastWinner", Description = "直前のロットの落札者名を返す (流札なら空)")]
    public string LastWinner() => Session().LastWinner?.DisplayName ?? "";

    [LiminalCommand("Auction/WonCount", Description = "指定参加者の落札数を返す (主人公は ノア)")]
    public int WonCount(string name) => Participant(name).WonLots.Count;

    [LiminalCommand("Auction/WonDistortion", Description = "主人公の落札記憶 (0 始まりの順) の歪みを返す")]
    public int WonDistortion(int index) => Session().Player.WonLots[index].Distortion;

    [LiminalCommand("Auction/WonViaCompetition", Description = "主人公の落札記憶 (0 始まりの順) が競合経由かを返す")]
    public bool WonViaCompetition(int index) => Session().Player.WonLots[index].ViaCompetition;

    [LiminalCommand("Auction/IsCollapsed", Description = "指定ライバルが人格崩壊したかを返す")]
    public bool IsCollapsed(string name) => Rival(name).HasCollapsed;

    [LiminalCommand("Auction/CollapseConsistent", Description = "全ライバルについて (崩壊 == 無落札) が成り立つかを返す")]
    public bool CollapseConsistent() => Session().Rivals.All(r => r.HasCollapsed == (r.WonLots.Count == 0));

    [LiminalCommand("Progress/PersonaEmotion", Description = "セーブ済みの主人公の感情状態を返す (未定なら None)")]
    public string PersonaEmotion() => _progress.Persona.EmotionState?.ToString() ?? "None";

    [LiminalCommand("Progress/IntegratedCount", Description = "人格に統合した記憶の数を返す")]
    public int IntegratedCount() => _progress.Persona.IntegratedLotIds.Count;

    [LiminalCommand("Progress/CollectionCount", Description = "コレクションに入った記憶の数を返す")]
    public int CollectionCount() => _progress.Persona.CollectionLotIds.Count;

    [LiminalCommand("Progress/TotalDistortion", Description = "統合した記憶の歪みの累計を返す")]
    public int TotalDistortion() => _progress.Persona.TotalDistortion;

    [LiminalCommand("Progress/WalletTotal", Description = "持ち越している感情リソースの総数を返す")]
    public int WalletTotal() => _progress.PlayerWallet.Total;

    [LiminalCommand("Progress/CollapsedIds", Description = "人格崩壊した参加者 ID をカンマ区切りで返す")]
    public string CollapsedIds() => string.Join(",", _progress.CollapsedParticipantIds.OrderBy(x => x));

    [LiminalCommand("Auction/ClickNextIfTie", Description = "競合の案内で止まっていれば「次へ」を押して競合を始める。押したら True")]
    public bool ClickNextIfTie() => WaitingFor() == "Tie" && ClickNextIfWaiting();

    [LiminalCommand("Auction/LastBidderCount", Description = "直前の開示で入札に参加した人数を返す (0 枚は不参加)")]
    public int LastBidderCount() => Session().LastReveal.Bidders.Count;

    [LiminalCommand("Auction/DialogueResultShown", Description = "対話パネルに結果のセリフが表示されているか")]
    public bool DialogueResultShown() => !string.IsNullOrEmpty(View().DialoguePanel.ResultText);

    [LiminalCommand("Auction/ObservedMatchesPlanned", Description = "観察結果に表示された枚数が相手の入札予定と一致するか")]
    public bool ObservedMatchesPlanned(string name) => View().DialoguePanel.ResultText.Contains($"(入札予定: {Rival(name).PlannedBid.Total} 枚)");

    [LiminalCommand("Auction/RememberPlanned", Description = "指定ライバルの入札予定枚数を控える (PlannedDelta 用)")]
    public int RememberPlanned(string name) => _rememberedPlanned = Rival(name).PlannedBid.Total;

    [LiminalCommand("Auction/PlannedDelta", Description = "控えた値からの入札予定の増減を返す")]
    public int PlannedDelta(string name) => Rival(name).PlannedBid.Total - _rememberedPlanned;

    [LiminalCommand("Auction/RememberLotEmotion", Description = "今のロットの属性を控える (PersonaEmotionIsRemembered 用)")]
    public string RememberLotEmotion() => (_rememberedEmotion = Session().CurrentLot.Emotion).ToString();

    [LiminalCommand("Progress/PersonaEmotionIsRemembered", Description = "セーブ済みの感情状態が控えたロット属性と一致するか")]
    public bool PersonaEmotionIsRemembered() => _progress.Persona.EmotionState == _rememberedEmotion;

    [LiminalCommand("Auction/ThemeClarified", Description = "記憶テーマが鮮明化したか (出品の過半を落札)")]
    public bool ThemeClarified() => Session().IsThemeClarified;

    [LiminalCommand("Auction/BaptismHeaderClarified", Description = "洗礼画面の見出しに鮮明化後のテーマが含まれているか")]
    public bool BaptismHeaderClarified() => View().Baptism.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(t => t.name == "HeaderText").text.Contains(Session().Floor.ClarifiedTheme);

    [LiminalCommand("Auction/ActivePanel", Description = "表示中のパネル名 (Counter / Dialogue / Bid / Competition / Baptism / GameOver / Next / None) を返す")]
    public string ActivePanel()
    {
        var v = View();
        if (v.CounterDialogue.gameObject.activeSelf) return "Counter";
        if (v.Baptism.gameObject.activeSelf) return "Baptism";
        if (v.GameOver.gameObject.activeSelf) return "GameOver";
        if (v.CompetitionPanel.gameObject.activeSelf) return "Competition";
        if (v.DialoguePanel.gameObject.activeSelf) return "Dialogue";
        if (v.BidPanel.gameObject.activeSelf) return "Bid";
        return v.IsWaitingNext ? "Next" : "None";
    }

    [LiminalCommand("Auction/ClickPlus", Description = "入札パネルの指定属性の + を押す (同名ボタンが 8 個あるため属性で引く)")]
    public string ClickPlus(EmotionType emotion)
    {
        var item = View().BidPanel.GetComponentsInChildren<EmotionBidItemView>(true).First(i => i.Emotion == emotion);
        var plus = item.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "PlusButton");
        if (!plus.interactable) throw new InvalidOperationException($"{emotion} の + は押せない");
        plus.onClick.Invoke();
        return emotion.ToString();
    }

    [LiminalCommand("Auction/ClickIntegrate", Description = "洗礼で指定ロット番号 (0 始まり) の「統合する」を押す")]
    public string ClickIntegrate(int lotIndex)
    {
        var entry = View().Baptism.GetComponentsInChildren<WonLotEntryView>(true).First(e => e.WonLot.LotIndex == lotIndex);
        entry.GetComponentInChildren<UnityEngine.UI.Button>(true).onClick.Invoke();
        return entry.WonLot.Lot.LotId;
    }

    [LiminalCommand("Auction/WaitingFor", Description = "「次へ」待ちの区切りを返す (Theme / LotIntro / Tie / LotResult / None)")]
    public string WaitingFor()
    {
        var v = View();
        if (!v.IsWaitingNext) return "None";
        var m = v.Message;
        if (m.StartsWith("記憶テーマ")) return "Theme";
        if (m.StartsWith("ロット")) return "LotIntro";
        if (m.Contains("競合に入る")) return "Tie";
        return "LotResult";
    }

    [LiminalCommand("Auction/RevealReached", Description = "一斉開示が終わって落札表示か競合の案内で止まっているか")]
    public bool RevealReached()
    {
        var w = WaitingFor();
        return w == "LotResult" || w == "Tie";
    }

    [LiminalCommand("Auction/ClickNextIfWaiting", Description = "「次へ」が出ていれば押す。押したら True")]
    public bool ClickNextIfWaiting()
    {
        var v = View();
        if (!v.IsWaitingNext) return false;
        v.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "NextButton").onClick.Invoke();
        return true;
    }

    [LiminalCommand("Auction/LotIntroOrEndReached", Description = "次ロットの提示、または洗礼 / ゲームオーバーに到達したか")]
    public bool LotIntroOrEndReached()
    {
        var panel = ActivePanel();
        return panel == "Baptism" || panel == "GameOver" || WaitingFor() == "LotIntro";
    }

    [LiminalCommand("Auction/BidAll", Description = "入札パネルで手持ち全部を + で積む (実 UI 経由)")]
    public int BidAll()
    {
        var count = 0;
        foreach (var item in View().BidPanel.GetComponentsInChildren<EmotionBidItemView>(true))
        {
            var plus = item.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "PlusButton");
            while (plus.interactable) { plus.onClick.Invoke(); count++; }
        }
        return count;
    }

    [LiminalCommand("Auction/BidMatchingAndMismatched", Description = "今のロットと同じ属性を matching 枚、それ以外の属性を 1 属性 perEmotion 枚ずつ合計 mismatched 枚積む")]
    public string BidMatchingAndMismatched(int matching, int mismatched, int perEmotion = 4)
    {
        var lotEmotion = Session().CurrentLot.Emotion;
        for (var i = 0; i < matching; i++) ClickPlus(lotEmotion);
        var remaining = mismatched;
        foreach (var e in EmotionWallet.ALL_EMOTIONS.Where(x => x != lotEmotion))
        {
            var take = Math.Min(Math.Min(remaining, perEmotion), Session().Player.Wallet.Get(e));
            for (var i = 0; i < take; i++) ClickPlus(e);
            remaining -= take;
            if (remaining == 0) break;
        }
        return $"{lotEmotion}x{matching} + other x{mismatched - remaining}";
    }

    [LiminalCommand("Auction/BidToTieTopRival", Description = "ライバルの入札予定の最大枚数とちょうど同じ枚数を積む (競合を起こす)")]
    public int BidToTieTopRival()
    {
        var target = MaxPlannedBid();
        var placed = 0;
        foreach (var e in EmotionWallet.ALL_EMOTIONS)
        {
            var owned = Session().Player.Wallet.Get(e);
            for (var i = 0; i < owned && placed < target; i++) { ClickPlus(e); placed++; }
        }
        if (placed != target) throw new InvalidOperationException($"手持ち不足: {placed}/{target}");
        return placed;
    }

    [LiminalCommand("Auction/RaiseUntilLeading", Description = "競合中、他の競合者の最大額を margin 枚上回るまで 1 枚ずつ上乗せする (実 UI 経由)")]
    public int RaiseUntilLeading(int margin = 5)
    {
        var s = Session();
        var c = s.Competition;
        var raised = 0;
        while (c.TotalOf(s.Player) < c.Competitors.Where(x => !x.IsPlayer).Max(c.TotalOf) + margin)
        {
            var e = EmotionWallet.ALL_EMOTIONS.First(x => s.Player.Wallet.Get(x) > 0);
            ClickPlus(e);
            raised++;
        }
        return raised;
    }

    [LiminalCommand("Auction/CompetitionLosersRefunded", Description = "直前の競合で負けた競合者に入札が返っているか (仕様では返らないので False)")]
    public bool CompetitionLosersRefunded()
    {
        var s = Session();
        var c = s.Competition;
        var losers = c.Competitors.Where(x => x != s.LastWinner && !x.IsPlayer).ToList();
        if (losers.Count == 0) throw new InvalidOperationException("負けた NPC 競合者がいない");
        // 階層開始時の 40 枚から競合の最終内訳ぶん減っていれば未返却
        return losers.Any(l => l.Wallet.Total != GameConstants.EMOTION_REFILL_PER_FLOOR * EmotionWallet.ALL_EMOTIONS.Length - c.FinalBidOf(l).Total);
    }

    [LiminalCommand("Auction/DrainRival", Description = "指定ライバルの手持ちを 0 にする (破産状態の再現)")]
    public string DrainRival(string name)
    {
        var r = Rival(name);
        r.Wallet.LoadCounts(new int[EmotionWallet.ALL_EMOTIONS.Length]);
        View().RefreshSlots();
        return r.DisplayName;
    }

    [LiminalCommand("Auction/BidAboveTopRival", Description = "ライバルの入札予定の最大枚数 + 1 枚を積む (確実に単独最高額にする)")]
    public int BidAboveTopRival()
    {
        var target = MaxPlannedBid() + 1;
        var placed = 0;
        foreach (var e in EmotionWallet.ALL_EMOTIONS)
        {
            var owned = Session().Player.Wallet.Get(e);
            for (var i = 0; i < owned && placed < target; i++) { ClickPlus(e); placed++; }
        }
        if (placed != target) throw new InvalidOperationException($"手持ち不足: {placed}/{target}");
        return placed;
    }

    [LiminalCommand("Auction/AutoPlayFloor", Description = "テーマ公開から 5 ロットを実 UI で自動進行する。最初の winLots ロットは最大予定 + 1 枚で落札を狙い、残りは 0 枚で流す")]
    public async UniTask<string> AutoPlayFloor(int winLots = 1)
    {
        var ct = View().destroyCancellationToken;
        await UniTask.WaitUntil(() => WaitingFor() == "Theme", cancellationToken: ct);
        ClickNextIfWaiting();
        for (var lot = 0; lot < GameConstants.LOTS_PER_FLOOR; lot++)
        {
            await UniTask.WaitUntil(() => WaitingFor() == "LotIntro", cancellationToken: ct);
            ClickNextIfWaiting();
            await UniTask.WaitUntil(() => ActivePanel() == "Dialogue", cancellationToken: ct);
            Click("ToBiddingButton");
            await UniTask.WaitUntil(() => ActivePanel() == "Bid", cancellationToken: ct);
            if (lot < winLots) BidAboveTopRival();
            Click("ConfirmButton");
            await UniTask.WaitUntil(RevealReached, cancellationToken: ct);
            ClickNextIfTie();
            await UniTask.WaitUntil(() => WaitingFor() == "LotResult", cancellationToken: ct);
            ClickNextIfWaiting();
        }
        await UniTask.WaitUntil(() => ActivePanel() is "Baptism" or "GameOver", cancellationToken: ct);
        return $"won={WonCount("ノア")} panel={ActivePanel()}";
    }

    private static void Click(string name)
    {
        View().GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == name && b.interactable).onClick.Invoke();
    }

    private static AuctionParticipant Participant(string name)
    {
        return Session().Participants.FirstOrDefault(p => p.DisplayName == name) ?? throw new ArgumentException($"参加者が見つからない: {name}");
    }

    private static AuctionSession Session()
    {
        var scope = LifetimeScope.Find<AuctionLifetimeScope>();
        if (scope == null) throw new InvalidOperationException("AuctionScene ではない");
        return scope.Container.Resolve<AuctionSession>();
    }

    private static AuctionView View()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<AuctionView>();
        if (view == null) throw new InvalidOperationException("AuctionView が無い");
        return view;
    }

    private static AuctionParticipant Rival(string name)
    {
        var p = Participant(name);
        if (p.IsPlayer) throw new ArgumentException("主人公は対象にできない");
        return p;
    }

    [LiminalCommand("Auction/Start", Description = "進行度と無関係に指定階層のオークションを開始する。seed 0 はランダム、timeout は競合の確定秒数")]
    public string Start(int floor = 0, int seed = 1, float timeout = 10f)
    {
        _request.Set(floor, seed, timeout);
        _sceneTransition.TransitionToSceneWithFade(SceneType.Auction).Forget();
        return $"floor={floor} seed={seed}";
    }
}
