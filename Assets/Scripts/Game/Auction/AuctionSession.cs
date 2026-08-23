using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 1 階層分の記憶オークションの進行と判定 (純 C#)
/// 5 ロットを 1 つずつ「対話 → 入札 → 開示 → (競合) → 確定」で回し、最後に洗礼へ進む
/// </summary>
public class AuctionSession
{
    public FloorData Floor { get; }
    public AuctionPhase Phase { get; private set; } = AuctionPhase.ThemeAnnounce;
    public IReadOnlyList<MemoryLotData> Lots => _lots;
    public int CurrentLotIndex { get; private set; } = -1;
    public MemoryLotData CurrentLot => CurrentLotIndex >= 0 && CurrentLotIndex < _lots.Count ? _lots[CurrentLotIndex] : null;
    public IReadOnlyList<AuctionParticipant> Participants => _participants;
    public AuctionParticipant Player => _participants[0];
    public IReadOnlyList<AuctionParticipant> Rivals => _participants.Skip(1).ToList();
    public RevealResult LastReveal { get; private set; }

    /// <summary>一斉開示の演出を見せ終えたか (競合に入る前に必ず 1 度通す)</summary>
    public bool RevealShown { get; set; }
    public CompetitionState Competition { get; private set; }
    public AuctionParticipant LastWinner { get; private set; }

    public bool IsLastLot => CurrentLotIndex >= _lots.Count - 1;

    /// <summary>この階層に楽園への鍵があり、主人公がそれを取り逃したか</summary>
    public bool MissedKey => _lots.Any(l => l.IsKey) && !Player.WonLots.Any(w => w.Lot.IsKey);

    /// <summary>記憶テーマの鮮明化。出品の過半を落札したときだけ鮮明化後のテーマが開示される</summary>
    public bool IsThemeClarified => Player.WonLots.Count * 2 > _lots.Count && !string.IsNullOrEmpty(Floor.ClarifiedTheme);
    public bool IsPlayerGameOver => Phase == AuctionPhase.GameOver;

    /// <summary>このセッションの乱数。進行の再現性を保つため外でも同じ種を使う</summary>
    public System.Random Rng => _rng;

    private const int OBSERVE_SUCCESS_RATE = 85;
    private const int OTHER_SUCCESS_RATE = 25;

    private readonly List<MemoryLotData> _lots;
    private readonly List<AuctionParticipant> _participants;
    private readonly Random _rng;
    private readonly float _competitionTimeout;

    public AuctionSession(FloorData floor, EmotionWallet playerWallet, string playerName, Random rng, float competitionTimeout)
    {
        Floor = floor;
        _rng = rng;
        _competitionTimeout = competitionTimeout;
        _lots = floor.Lots.OrderBy(_ => rng.Next()).ToList();
        _participants = new List<AuctionParticipant> { new(null, playerWallet, true, playerName) };
        foreach (var rival in floor.Rivals)
        {
            var wallet = new EmotionWallet();
            wallet.Refill(GameConstants.EMOTION_REFILL_PER_FLOOR);
            _participants.Add(new AuctionParticipant(rival, wallet, false, rival.DisplayName));
        }
    }

    /// <summary>対話の初期対象 (入札に参加できる最初のライバル)</summary>
    public AuctionParticipant FirstAvailableRival() => Rivals.FirstOrDefault(r => r.CanBid) ?? Rivals[0];

    public bool CanUseDialogue(AuctionParticipant target, DialogueCommand command) => Phase == AuctionPhase.Dialogue && !target.IsPlayer && target.CanBid && !target.UsedCommandsThisLot.Contains(command);

    public bool TryPlayerRaise(EmotionType emotion, float now) => Competition.TryRaise(Player, emotion, now);

    /// <summary>
    /// 次のロットを場に出し、NPC の入札予定を組む
    /// </summary>
    public MemoryLotData BeginNextLot()
    {
        if (Phase != AuctionPhase.ThemeAnnounce && Phase != AuctionPhase.LotResult) throw new InvalidOperationException($"ロットを開始できるフェーズではない: {Phase}");
        CurrentLotIndex++;
        LastReveal = null;
        Competition = null;
        LastWinner = null;
        foreach (var p in _participants)
        {
            p.ResetForLot();
            if (!p.IsPlayer) p.PlannedBid = BidAI.Plan(p, CurrentLot, _rng);
        }
        Phase = AuctionPhase.Dialogue;
        return CurrentLot;
    }

    /// <summary>
    /// 対話コマンドを 1 回使う。各コマンドは各キャラに 1 ロット 1 回まで
    /// </summary>
    public DialogueOutcome UseDialogue(AuctionParticipant target, DialogueCommand command)
    {
        if (!CanUseDialogue(target, command)) throw new InvalidOperationException($"対話コマンドを使えない: {target.DisplayName} / {command}");
        target.UsedCommandsThisLot.Add(command);
        var profile = target.Data.Profile;
        var rate = command == DialogueCommand.Observe ? OBSERVE_SUCCESS_RATE : OTHER_SUCCESS_RATE;
        var success = _rng.Next(100) < rate;
        if (!success) return new DialogueOutcome(command, target, false, profile.FailLine);

        if (command == DialogueCommand.Observe)
        {
            // 逆対話は各ロット各キャラ 1 回まで
            var counter = !target.CounterFiredThisLot && _rng.Next(100) < profile.CounterDialogueChance ? profile.CounterDialogue : null;
            if (counter != null) target.CounterFiredThisLot = true;
            return new DialogueOutcome(command, target, true, profile.ObserveLine, target.PlannedBid.Total, counter);
        }

        var reaction = profile.ReactionFor(command);
        if (command == DialogueCommand.Persuade && profile.PersuadeBoostsFavorite && CurrentLot.Emotion == target.Data.Emotion) reaction = BidReaction.BigIncrease;
        BidAI.ApplyReaction(target, CurrentLot, reaction, profile.ReactionScale, _rng);
        return new DialogueOutcome(command, target, true, profile.LineFor(command));
    }

    /// <summary>
    /// 逆対話への二択の返答。入札予定が大幅に動く
    /// </summary>
    public void AnswerCounterDialogue(AuctionParticipant target, int choiceIndex)
    {
        var reaction = target.Data.Profile.CounterDialogue.ReactionFor(choiceIndex);
        BidAI.ApplyReaction(target, CurrentLot, reaction, 100, _rng);
    }

    public void EnterBidding()
    {
        if (Phase != AuctionPhase.Dialogue) throw new InvalidOperationException($"入札に進めるフェーズではない: {Phase}");
        Phase = AuctionPhase.Bidding;
    }

    /// <summary>
    /// プレイヤーの入札を確定し、全員分を一斉開示する。リソース 0 の参加者は卓から外れる
    /// </summary>
    public RevealResult SubmitPlayerBid(EmotionBid bid)
    {
        if (Phase != AuctionPhase.Bidding) throw new InvalidOperationException($"入札できるフェーズではない: {Phase}");
        if (!Player.Wallet.CanAfford(bid)) throw new InvalidOperationException("所持リソースを超える入札");
        Player.SubmittedBid = bid.Clone();
        foreach (var p in Rivals) p.SubmittedBid = p.CanBid ? p.PlannedBid.Clone() : null;

        // 0 枚は入札したことにならない。全員 0 枚なら流札
        var bidders = _participants.Where(p => p.SubmittedBid != null && p.SubmittedBid.Total > 0).ToList();
        LastReveal = new RevealResult(bidders);
        RevealShown = false;
        Phase = LastReveal.IsTie ? AuctionPhase.Competition : AuctionPhase.Reveal;
        if (Phase == AuctionPhase.Competition) Competition = new CompetitionState(CurrentLot, LastReveal.TiedParticipants, 0f, _competitionTimeout);
        return LastReveal;
    }

    /// <summary>
    /// 競合開始時刻を合わせる (開示演出の後に呼ぶ)
    /// </summary>
    public void StartCompetition(float now)
    {
        if (Phase != AuctionPhase.Competition) throw new InvalidOperationException($"競合フェーズではない: {Phase}");
        // 競合に入った時点で提出分を消費し、返却しない
        foreach (var c in Competition.Competitors) c.Wallet.TryConsume(c.SubmittedBid);
        Competition = new CompetitionState(CurrentLot, LastReveal.TiedParticipants, now, _competitionTimeout);
    }

    /// <summary>
    /// NPC の上乗せ判断。方針ごとに確率を変え、負けている間だけ追う
    /// </summary>
    public bool TryNpcRaise(AuctionParticipant npc, float now)
    {
        if (npc.IsPlayer || !Competition.Competitors.Contains(npc)) return false;
        var leader = Competition.Leader();
        if (leader == npc) return false;

        // 際限なく競り上げると決着しないため、提出額に応じた上限で降りる
        if (Competition.RaisesOf(npc).Total >= npc.SubmittedBid.Total + GameConstants.NPC_MAX_RAISE_MARGIN) return false;

        var isFavorite = CurrentLot.Emotion == npc.Data.Emotion;
        var chance = npc.Data.Profile.CompetitionPolicy switch
        {
            CompetitionPolicy.Never => 0,
            CompetitionPolicy.Rarely => 15,
            CompetitionPolicy.Normal => 50,
            CompetitionPolicy.FavoriteOnly => isFavorite ? 60 : 0,
            CompetitionPolicy.FavoriteAggressive => isFavorite ? 95 : 40,
            CompetitionPolicy.Random => 50,
            CompetitionPolicy.ByBidSize => Math.Min(90, npc.SubmittedBid.Total * 20),
            CompetitionPolicy.AllIn => 100,
            _ => 50,
        };
        if (_rng.Next(100) >= chance) return false;

        var emotion = EmotionWallet.ALL_EMOTIONS
            .OrderByDescending(e => e == npc.Data.Emotion ? 1 : 0)
            .ThenByDescending(e => npc.Wallet.Get(e))
            .First();
        return Competition.TryRaise(npc, emotion, now);
    }

    /// <summary>
    /// 競合を締めて落札者を決める。時間切れでも同数のままなら、同額の中から抽選で決める (流札にはしない)
    /// </summary>
    public AuctionParticipant ResolveCompetition()
    {
        if (Phase != AuctionPhase.Competition) throw new InvalidOperationException($"競合フェーズではない: {Phase}");
        Competition.End();
        var winner = Competition.Leader() ?? PickTiedLeader();
        var others = Competition.Competitors.ToDictionary(c => c.DisplayName, c => Competition.TotalOf(c));
        if (winner != null) winner.WonLots.Add(new WonLot(CurrentLot, CurrentLotIndex, Competition.FinalBidOf(winner), true, others));
        LastWinner = winner;
        Phase = AuctionPhase.LotResult;
        return winner;
    }

    /// <summary>
    /// 単独最高額の落札を確定する。落札者だけ消費し、他は全額返却 (元々消費していない)
    /// </summary>
    public AuctionParticipant ResolveReveal()
    {
        if (Phase != AuctionPhase.Reveal) throw new InvalidOperationException($"開示フェーズではない: {Phase}");
        var winner = LastReveal.Winner;
        if (winner != null)
        {
            winner.Wallet.TryConsume(winner.SubmittedBid);
            var others = LastReveal.Bidders.Where(b => b != winner).ToDictionary(b => b.DisplayName, b => b.SubmittedBid.Total);
            winner.WonLots.Add(new WonLot(CurrentLot, CurrentLotIndex, winner.SubmittedBid.Clone(), false, others));
        }
        LastWinner = winner;
        Phase = AuctionPhase.LotResult;
        return winner;
    }

    /// <summary>
    /// 全ロット終了後の判定。主人公が無落札、または楽園への鍵を取り逃したらゲームオーバー。NPC の無落札は人格崩壊
    /// </summary>
    public void FinishLots()
    {
        if (Phase != AuctionPhase.LotResult || !IsLastLot) throw new InvalidOperationException("全ロットが終わっていない");
        foreach (var p in Rivals) p.HasCollapsed = p.WonLots.Count == 0;
        Phase = Player.WonLots.Count == 0 || MissedKey ? AuctionPhase.GameOver : AuctionPhase.Baptism;
    }

    public void Finish()
    {
        if (Phase != AuctionPhase.Baptism) throw new InvalidOperationException($"洗礼フェーズではない: {Phase}");
        Phase = AuctionPhase.Finished;
    }

    private AuctionParticipant PickTiedLeader()
    {
        var max = Competition.Competitors.Max(Competition.TotalOf);
        var tied = Competition.Competitors.Where(c => Competition.TotalOf(c) == max).ToList();
        return tied[_rng.Next(tied.Count)];
    }
}
