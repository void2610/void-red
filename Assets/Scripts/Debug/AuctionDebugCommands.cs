using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Void2610.LiminalPalette;

/// <summary>
/// 記憶オークションを LiminalPalette から起動 / 観測 / 操作するデバッグコマンド群
/// 操作は実 UI (感情ホイール / 入札ウィンドウ / 対話選択肢 / 参加者アイコン) を経由する
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

    [LiminalCommand("Auction/Floor", Description = "回している階層の番号を返す")]
    public int Floor() => Session().Floor.FloorIndex;

    [LiminalCommand("Auction/Phase", Description = "現在のフェーズを返す")]
    public string Phase() => Session().Phase.ToString();

    [LiminalCommand("Auction/LotIndex", Description = "現在のロット番号 (0 始まり) を返す")]
    public int LotIndex() => Session().CurrentLotIndex;

    [LiminalCommand("Auction/LotTitle", Description = "現在のロット名を返す")]
    public string LotTitle() => Session().CurrentLot.Title;

    [LiminalCommand("Auction/LotEmotion", Description = "現在のロットの感情属性を返す")]
    public string LotEmotion() => Session().CurrentLot.Emotion.ToString();

    [LiminalCommand("Auction/CurrentLotIsKey", Description = "今のロットが楽園への鍵か")]
    public bool CurrentLotIsKey() => Session().CurrentLot.IsKey;

    [LiminalCommand("Auction/MissedKey", Description = "楽園への鍵を取り逃したか")]
    public bool MissedKey() => Session().MissedKey;

    [LiminalCommand("Auction/ThemeClarified", Description = "記憶テーマが鮮明化したか (出品の過半を落札)")]
    public bool ThemeClarified() => Session().IsThemeClarified;

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

    [LiminalCommand("Auction/DialogueTarget", Description = "現在選択中の対話相手を返す")]
    public string DialogueTarget() => View().SelectedTarget?.DisplayName ?? "";

    [LiminalCommand("Auction/SelectedEmotion", Description = "感情ホイールで選択中の属性を返す")]
    public EmotionType SelectedEmotion() => View().Auction.GetComponentInChildren<EmotionResourceDisplayView>(true).SelectedEmotion;

    [LiminalCommand("Auction/Competing", Description = "競合フェーズ中かを返す")]
    public bool Competing() => Session().Phase == AuctionPhase.Competition;

    [LiminalCommand("Auction/CompetitionTotal", Description = "競合中の指定参加者の現在額を返す (主人公は ノア)")]
    public int CompetitionTotal(string name) => Session().Competition.TotalOf(Participant(name));

    [LiminalCommand("Auction/LastWinner", Description = "直前のロットの落札者名を返す (流札なら空)")]
    public string LastWinner() => Session().LastWinner?.DisplayName ?? "";

    [LiminalCommand("Auction/LastBidderCount", Description = "直前の開示で入札に参加した人数を返す (0 枚は不参加)")]
    public int LastBidderCount() => Session().LastReveal.Bidders.Count;

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

    [LiminalCommand("Auction/RememberPlanned", Description = "指定ライバルの入札予定枚数を控える (PlannedDelta 用)")]
    public int RememberPlanned(string name) => _rememberedPlanned = Rival(name).PlannedBid.Total;

    [LiminalCommand("Auction/PlannedDelta", Description = "控えた値からの入札予定の増減を返す")]
    public int PlannedDelta(string name) => Rival(name).PlannedBid.Total - _rememberedPlanned;

    [LiminalCommand("Auction/RememberLotEmotion", Description = "今のロットの属性を控える (PersonaEmotionIsRemembered 用)")]
    public string RememberLotEmotion() => (_rememberedEmotion = Session().CurrentLot.Emotion).ToString();

    [LiminalCommand("Auction/BidAmounts", Description = "現在積んでいる入札の合計枚数を返す")]
    public int BidAmounts() => Session().Player.SubmittedBid?.Total ?? 0;

    [LiminalCommand("Progress/PersonaEmotion", Description = "セーブ済みの主人公の感情状態を返す (未定なら None)")]
    public string PersonaEmotion() => _progress.Persona.EmotionState?.ToString() ?? "None";

    [LiminalCommand("Progress/PersonaEmotionIsRemembered", Description = "セーブ済みの感情状態が控えたロット属性と一致するか")]
    public bool PersonaEmotionIsRemembered() => _progress.Persona.EmotionState == _rememberedEmotion;

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

    [LiminalCommand("Auction/DialogueReady", Description = "対話フェーズの入力を受け付けているか")]
    public bool DialogueReady() => View().IsWaitingDialogueInput;

    [LiminalCommand("Auction/LotSettled", Description = "今のロットの決着 (落札 / 流札) がついたか")]
    public bool LotSettled() => Session().Phase is AuctionPhase.LotResult or AuctionPhase.Baptism or AuctionPhase.GameOver or AuctionPhase.Dialogue && Session().LastReveal != null;

    [LiminalCommand("Auction/ActivePanel", Description = "表示中の主なパネル (Dialogue / Bid / Competition / Baptism / GameOver / None) を返す")]
    public string ActivePanel() => Session().Phase switch
    {
        AuctionPhase.Baptism => "Baptism",
        AuctionPhase.GameOver => "GameOver",
        AuctionPhase.Competition => "Competition",
        AuctionPhase.Bidding => "Bid",
        AuctionPhase.Dialogue => "Dialogue",
        _ => "None",
    };

    [LiminalCommand("Auction/GameOverMessageMentionsKey", Description = "ゲームオーバーの本文が鍵の取り逃しを伝えているか")]
    public bool GameOverMessageMentionsKey() => View().GameOver.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t => t.text.Contains("鍵"));

    [LiminalCommand("Auction/BaptismReady", Description = "洗礼画面に落札した記憶の札が並び終えたか")]
    public bool BaptismReady() => View().Baptism.GetComponentsInChildren<AcquiredCardView>(true).Length == Session().Player.WonLots.Count;

    [LiminalCommand("Auction/BaptismSelected", Description = "洗礼で統合する記憶が選ばれているか")]
    public bool BaptismSelected() => View().Baptism.SelectedLot != null;

    [LiminalCommand("Auction/Speed", Description = "演出の早送り倍率を変える (検証用)")]
    public float Speed(float value = 1f) => Time.timeScale = Mathf.Clamp(value, 0.1f, 20f);

    [LiminalCommand("Auction/BaptismHeaderClarified", Description = "洗礼の見出しに鮮明化後のテーマが出ているか")]
    public bool BaptismHeaderClarified()
    {
        var clarified = Session().Floor.ClarifiedTheme;
        if (string.IsNullOrEmpty(clarified)) return false;
        return View().Baptism.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t => t.text.Contains(clarified));
    }

    [LiminalCommand("Auction/SelectTarget", Description = "参加者アイコンを押して対話相手を選ぶ")]
    public string SelectTarget(string name)
    {
        var icon = Icons().First(i => i.Participant.DisplayName == name);
        if (!icon.GetComponentInChildren<Button>(true).interactable) throw new InvalidOperationException($"今は選べない: {name}");
        icon.GetComponentInChildren<Button>(true).onClick.Invoke();
        return name;
    }

    [LiminalCommand("Auction/UseDialogue", Description = "対話コマンドのボタンを押す (Observe / Provoke / Empathize / Persuade)")]
    public string UseDialogue(string command)
    {
        var parsed = Enum.Parse<DialogueCommand>(command);
        var button = ChoiceButtons().FirstOrDefault(b => b.name == $"DialogueChoice_{parsed}") ?? throw new InvalidOperationException($"対話ボタンが無い: {command}");
        if (!button.interactable) throw new InvalidOperationException($"押せない: {command}");
        button.onClick.Invoke();
        return command;
    }

    [LiminalCommand("Auction/SelectEmotion", Description = "感情ホイールを回して指定属性を選ぶ")]
    public async UniTask<string> SelectEmotion(EmotionType emotion)
    {
        var wheel = View().Auction.GetComponentInChildren<EmotionResourceDisplayView>(true);
        var button = wheel.GetComponentInChildren<Button>(true);
        for (var i = 0; i < EmotionWallet.ALL_EMOTIONS.Length; i++)
        {
            if (SelectedEmotion() == emotion) return emotion.ToString();
            button.onClick.Invoke();
            await UniTask.WaitUntil(() => SelectedEmotion() == wheel.SelectedEmotion, cancellationToken: View().destroyCancellationToken);
            await UniTask.Delay(350, cancellationToken: View().destroyCancellationToken);
        }
        if (SelectedEmotion() != emotion) throw new InvalidOperationException($"ホイールで {emotion} を選べなかった");
        return emotion.ToString();
    }

    [LiminalCommand("Auction/Increase", Description = "入札ウィンドウの + を count 回押す")]
    public int Increase(int count = 1)
    {
        var button = BidButton("IncreaseButton");
        var pressed = 0;
        for (var i = 0; i < count && button.interactable; i++)
        {
            button.onClick.Invoke();
            pressed++;
        }
        return pressed;
    }

    [LiminalCommand("Auction/Decrease", Description = "入札ウィンドウの - を count 回押す")]
    public int Decrease(int count = 1)
    {
        var button = BidButton("DecreaseButton");
        var pressed = 0;
        for (var i = 0; i < count && button.interactable; i++)
        {
            button.onClick.Invoke();
            pressed++;
        }
        return pressed;
    }

    [LiminalCommand("Auction/Confirm", Description = "入札確定 (対話フェーズでは入札フェーズへ進む) ボタンを押す")]
    public string Confirm()
    {
        var button = View().Auction.GetComponentsInChildren<Button>(true).First(b => b.name == "ConfirmButton");
        if (!button.interactable) throw new InvalidOperationException("確定ボタンが押せない");
        button.onClick.Invoke();
        return "confirmed";
    }

    [LiminalCommand("Auction/Raise", Description = "競合フェーズの上乗せボタンを count 回押す")]
    public int Raise(int count = 1)
    {
        var button = View().Competition.GetComponentsInChildren<Button>(true).First(b => b.name == "RaiseButton");
        var pressed = 0;
        for (var i = 0; i < count && button.interactable; i++)
        {
            button.onClick.Invoke();
            pressed++;
        }
        return pressed;
    }

    [LiminalCommand("Auction/CompetitionLosersRefunded", Description = "直前の競合で負けた競合者に入札が返っているか (仕様では返らないので False)")]
    public bool CompetitionLosersRefunded()
    {
        var s = Session();
        var c = s.Competition;
        var losers = c.Competitors.Where(x => x != s.LastWinner && !x.IsPlayer).ToList();
        if (losers.Count == 0) throw new InvalidOperationException("負けた NPC 競合者がいない");
        return losers.Any(l => l.Wallet.Total == GameConstants.EMOTION_REFILL_PER_FLOOR * EmotionWallet.ALL_EMOTIONS.Length);
    }

    [LiminalCommand("Auction/DrainRival", Description = "指定ライバルの手持ちを 0 にする (破産状態の再現)")]
    public string DrainRival(string name)
    {
        var r = Rival(name);
        r.Wallet.LoadCounts(new int[EmotionWallet.ALL_EMOTIONS.Length]);
        View().RefreshParticipants();
        return r.DisplayName;
    }

    [LiminalCommand("Auction/ClickIntegrate", Description = "洗礼で指定ロット番号 (0 始まり) の札を選ぶ")]
    public string ClickIntegrate(int lotIndex)
    {
        var card = View().Baptism.GetComponentsInChildren<AcquiredCardView>(true).First(c => c.WonLot.LotIndex == lotIndex);
        var button = card.GetComponentsInChildren<Button>(true).First(b => b.interactable);
        button.onClick.Invoke();
        return card.WonLot.Lot.LotId;
    }

    [LiminalCommand("Auction/BidAll", Description = "手持ち全部を積む (ホイールを回しながら実 UI で + を押す)")]
    public async UniTask<int> BidAll()
    {
        var placed = 0;
        foreach (var e in EmotionWallet.ALL_EMOTIONS)
        {
            var owned = Session().Player.Wallet.Get(e);
            if (owned == 0) continue;
            await SelectEmotion(e);
            placed += Increase(owned);
        }
        return placed;
    }

    [LiminalCommand("Auction/BidAmount", Description = "指定属性を count 枚積む (ホイールを回してから + を押す)")]
    public async UniTask<int> BidAmount(EmotionType emotion, int count)
    {
        await SelectEmotion(emotion);
        return Increase(count);
    }

    [LiminalCommand("Auction/BidAboveTopRival", Description = "ライバルの入札予定の最大枚数 + 1 枚を積む (確実に単独最高額にする)")]
    public async UniTask<int> BidAboveTopRival()
    {
        var target = MaxPlannedBid() + 1;
        var placed = 0;
        foreach (var e in EmotionWallet.ALL_EMOTIONS)
        {
            if (placed >= target) break;
            var owned = Session().Player.Wallet.Get(e);
            if (owned == 0) continue;
            await SelectEmotion(e);
            placed += Increase(Math.Min(owned, target - placed));
        }
        if (placed != target) throw new InvalidOperationException($"手持ち不足: {placed}/{target}");
        return placed;
    }

    [LiminalCommand("Auction/BidAboveTopRivalIfKey", Description = "鍵のロットなら最大予定 + 1 枚を積む。winKey が false のときは最初のロットだけ積む")]
    public async UniTask<int> BidAboveTopRivalIfKey(bool winKey)
    {
        if (winKey ? CurrentLotIsKey() : !CurrentLotIsKey() && LotIndex() == 0) return await BidAboveTopRival();
        return 0;
    }

    [LiminalCommand("Auction/BidToTieTopRival", Description = "ライバルの入札予定の最大枚数とちょうど同じ枚数を積む (競合を起こす)")]
    public async UniTask<int> BidToTieTopRival()
    {
        var target = MaxPlannedBid();
        var placed = 0;
        foreach (var e in EmotionWallet.ALL_EMOTIONS)
        {
            if (placed >= target) break;
            var owned = Session().Player.Wallet.Get(e);
            if (owned == 0) continue;
            await SelectEmotion(e);
            placed += Increase(Math.Min(owned, target - placed));
        }
        if (placed != target) throw new InvalidOperationException($"手持ち不足: {placed}/{target}");
        return placed;
    }

    [LiminalCommand("Auction/BidMatchingAndMismatched", Description = "今のロットと同じ属性を matching 枚、それ以外を 1 属性 perEmotion 枚ずつ合計 mismatched 枚積む")]
    public async UniTask<string> BidMatchingAndMismatched(int matching, int mismatched, int perEmotion = 4)
    {
        var lotEmotion = Session().CurrentLot.Emotion;
        await BidAmount(lotEmotion, matching);
        var remaining = mismatched;
        foreach (var e in EmotionWallet.ALL_EMOTIONS.Where(x => x != lotEmotion))
        {
            if (remaining == 0) break;
            var take = Math.Min(Math.Min(remaining, perEmotion), Session().Player.Wallet.Get(e));
            if (take == 0) continue;
            await BidAmount(e, take);
            remaining -= take;
        }
        return $"{lotEmotion}x{matching} + other x{mismatched - remaining}";
    }

    [LiminalCommand("Auction/RaiseUntilLeading", Description = "競合中、他の競合者の最大額を margin 枚上回るまで上乗せする")]
    public int RaiseUntilLeading(int margin = 5)
    {
        var s = Session();
        var c = s.Competition;
        var raised = 0;
        while (c.TotalOf(s.Player) < c.Competitors.Where(x => !x.IsPlayer).Max(c.TotalOf) + margin)
        {
            if (Raise() == 0) throw new InvalidOperationException("上乗せできない");
            raised++;
        }
        return raised;
    }

    private static Button BidButton(string name)
    {
        return View().Auction.GetComponentsInChildren<Button>(true).First(b => b.name == name);
    }

    private static Button[] ChoiceButtons()
    {
        return View().Dialogue.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("DialogueChoice_")).ToArray();
    }

    private static ParticipantIconView[] Icons()
    {
        return View().GetComponentsInChildren<ParticipantIconView>(true);
    }

    private static AuctionSession Session()
    {
        var scope = LifetimeScope.Find<AuctionLifetimeScope>();
        if (scope == null) throw new InvalidOperationException("AuctionScene ではない");
        return scope.Container.Resolve<AuctionSession>();
    }

    private static AuctionSceneView View()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<AuctionSceneView>();
        if (view == null) throw new InvalidOperationException("AuctionSceneView が無い");
        return view;
    }

    private static AuctionParticipant Participant(string name)
    {
        return Session().Participants.FirstOrDefault(p => p.DisplayName == name) ?? throw new ArgumentException($"参加者が見つからない: {name}");
    }

    private static AuctionParticipant Rival(string name)
    {
        var p = Participant(name);
        if (p.IsPlayer) throw new ArgumentException("主人公は対象にできない");
        return p;
    }

    [LiminalCommand("Auction/Start", Description = "進行度と無関係に指定階層のオークションを開始する。seed 0 はランダム、timeout は競合の確定秒数、speed は演出の早送り倍率")]
    public string Start(int floor = 0, int seed = 1, float timeout = 10f, float speed = 1f)
    {
        _request.Set(floor, seed, timeout);
        Time.timeScale = Mathf.Clamp(speed, 0.1f, 20f);
        _sceneTransition.TransitionToSceneWithFade(SceneType.Auction).Forget();
        return $"floor={floor} seed={seed} speed={Time.timeScale}";
    }
}
