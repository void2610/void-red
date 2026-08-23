using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// オークションのルール判定を Play Mode 抜きで検証する
/// 演出を挟まないぶん高速で、Unity を占有しない
/// </summary>
public class AuctionSessionTests
{
    [Test]
    public void 合計枚数だけで勝敗が決まり属性は歪みにしか効かない()
    {
        var session = CreateSession(rivalBid: 2);
        session.BeginNextLot();
        session.EnterBidding();

        var bid = new EmotionBid();
        // ロットの属性と違う属性でも、枚数が上なら勝てる
        bid.Set(MismatchedEmotion(session.CurrentLot.Emotion), 3);
        var reveal = session.SubmitPlayerBid(bid);

        Assert.AreSame(session.Player, reveal.Winner);
        session.ResolveReveal();
        Assert.AreEqual(3, session.Player.WonLots[0].Distortion, "一致しない属性の枚数がそのまま歪みになる");
    }

    [Test]
    public void 同数なら競合に入り単独最高額で落札できる()
    {
        var session = CreateSession(rivalBid: 2);
        session.BeginNextLot();
        session.EnterBidding();

        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        var reveal = session.SubmitPlayerBid(bid);
        Assert.IsTrue(reveal.IsTie, "同数なら競合へ");

        session.StartCompetition(0f);
        Assert.IsTrue(session.TryPlayerRaise(session.CurrentLot.Emotion, 0.1f));
        var winner = session.ResolveCompetition();

        Assert.AreSame(session.Player, winner);
        Assert.IsTrue(session.Player.WonLots[0].ViaCompetition);
    }

    [Test]
    public void 競合は最後の上乗せから確定し全体でも打ち切られる()
    {
        var session = CreateSession(rivalBid: 2, competitionTimeout: 5f);
        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        session.SubmitPlayerBid(bid);
        session.StartCompetition(0f);

        Assert.IsFalse(session.Competition.IsTimedOut(4f), "確定時間の前は続く");
        Assert.IsTrue(session.Competition.IsTimedOut(6f), "最後の上乗せから確定時間で終わる");

        // 上乗せが続いても、全体の打ち切り時間を超えたら必ず終わる
        for (var t = 1f; t < 100f; t += 1f) session.TryPlayerRaise(session.CurrentLot.Emotion, t);
        Assert.IsTrue(session.Competition.IsTimedOut(5f * GameConstants.COMPETITION_HARD_LIMIT_RATIO + 1f));
    }

    [Test]
    public void 零枚の入札は不参加として扱われる()
    {
        var session = CreateSession(rivalBid: 0);
        session.BeginNextLot();
        session.EnterBidding();

        var reveal = session.SubmitPlayerBid(new EmotionBid());

        Assert.AreEqual(0, reveal.Bidders.Count, "全員 0 枚なら誰も入札していない");
        Assert.IsNull(session.ResolveReveal(), "流札になる");
    }

    [Test]
    public void 無落札ならライバルは人格崩壊し主人公はゲームオーバー()
    {
        var session = CreateSession(rivalBid: 0);
        for (var i = 0; i < GameConstants.LOTS_PER_FLOOR; i++)
        {
            session.BeginNextLot();
            session.EnterBidding();
            session.SubmitPlayerBid(new EmotionBid());
            session.ResolveReveal();
        }
        session.FinishLots();

        Assert.AreEqual(AuctionPhase.GameOver, session.Phase);
        Assert.IsTrue(session.Rivals.All(r => r.HasCollapsed));
    }

    [Test]
    public void 対話コマンドは各ライバルに一度ずつしか使えない()
    {
        var session = CreateSession(rivalBid: 2);
        session.BeginNextLot();
        var rival = session.Rivals[0];

        Assert.IsTrue(session.CanUseDialogue(rival, DialogueCommand.Provoke));
        session.UseDialogue(rival, DialogueCommand.Provoke);
        Assert.IsFalse(session.CanUseDialogue(rival, DialogueCommand.Provoke));
        Assert.IsTrue(session.CanUseDialogue(rival, DialogueCommand.Persuade), "別のコマンドは使える");
        Assert.IsTrue(session.CanUseDialogue(session.Rivals[1], DialogueCommand.Provoke), "別のライバルには使える");
    }

    [Test]
    public void 統合した記憶の主属性が感情状態になる()
    {
        var session = CreateSession(rivalBid: 0);
        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 3);
        bid.Set(MismatchedEmotion(session.CurrentLot.Emotion), 1);
        session.SubmitPlayerBid(bid);
        session.ResolveReveal();

        var persona = new PersonaState();
        persona.Integrate(session.Player.WonLots[0], session.Player.WonLots);

        Assert.AreEqual(session.Lots[0].Emotion, persona.EmotionState);
        Assert.AreEqual(1, persona.TotalDistortion);
        Assert.AreEqual(1, persona.CollectionLotIds.Count);
    }

    [Test]
    public void 破産したライバルは卓から外れる()
    {
        var session = CreateSession(rivalBid: 2);
        session.BeginNextLot();
        var broke = session.Rivals[0];
        broke.Wallet.LoadCounts(new int[EmotionWallet.ALL_EMOTIONS.Length]);

        Assert.IsFalse(broke.CanBid);
        Assert.IsFalse(session.CanUseDialogue(broke, DialogueCommand.Observe), "卓から外れた相手には対話できない");

        session.EnterBidding();
        var reveal = session.SubmitPlayerBid(new EmotionBid());
        Assert.IsFalse(reveal.Bidders.Contains(broke), "破産者は入札に参加しない");
        Assert.AreEqual(session.Rivals.Count - 1, reveal.Bidders.Count);
    }

    [Test]
    public void 出品の過半を落札すると記憶テーマが鮮明化する()
    {
        var session = CreateSession(rivalBid: 0, clarifiedTheme: "鮮明化後");
        for (var i = 0; i < GameConstants.LOTS_PER_FLOOR; i++)
        {
            session.BeginNextLot();
            session.EnterBidding();
            var bid = new EmotionBid();
            // 過半 (3 つ) だけ落札する
            if (i < 3) bid.Set(session.CurrentLot.Emotion, 1);
            session.SubmitPlayerBid(bid);
            session.ResolveReveal();
            if (i == 1) Assert.IsFalse(session.IsThemeClarified, "過半に届くまでは鮮明化しない");
        }

        Assert.AreEqual(3, session.Player.WonLots.Count);
        Assert.IsTrue(session.IsThemeClarified);
    }

    [Test]
    public void 最終階層は楽園への鍵を落札しないとゲームオーバーになる()
    {
        var session = CreateSession(rivalBid: 0, keyLotIndex: 4);
        for (var i = 0; i < GameConstants.LOTS_PER_FLOOR; i++)
        {
            session.BeginNextLot();
            session.EnterBidding();
            var bid = new EmotionBid();
            // 鍵以外は落札する
            if (!session.CurrentLot.IsKey) bid.Set(session.CurrentLot.Emotion, 1);
            session.SubmitPlayerBid(bid);
            session.ResolveReveal();
        }
        session.FinishLots();

        Assert.IsTrue(session.MissedKey);
        Assert.AreEqual(AuctionPhase.GameOver, session.Phase, "落札していても鍵が無ければ洗礼を受けられない");
    }

    [Test]
    public void 観察は相手の入札予定を返し対話は入札予定を動かす()
    {
        var session = CreateSession(rivalBid: 3, reactionScale: 100);
        session.BeginNextLot();
        var rival = session.Rivals[0];
        var planned = rival.PlannedBid.Total;

        var observed = session.UseDialogue(rival, DialogueCommand.Observe);
        if (observed.Success) Assert.AreEqual(planned, observed.ObservedTotal, "観察が成功したら予定枚数がそのまま返る");
        Assert.IsNotEmpty(observed.Line, "失敗してもセリフは返る");

        session.UseDialogue(rival, DialogueCommand.Provoke);
        Assert.AreNotEqual(0, rival.PlannedBid.Total, "対話後も入札予定は残る");
    }

    [Test]
    public void 通常入札は落札できなければリソースが減らない()
    {
        // ライバルの入札をばらけさせ、単独最高額の落札者を作る
        var session = CreateSession(rivalBid: 5, distinctRivalBids: true);
        session.BeginNextLot();
        session.EnterBidding();
        var before = session.Player.Wallet.Total;

        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 1);
        session.SubmitPlayerBid(bid);
        var winner = session.ResolveReveal();

        Assert.AreNotSame(session.Player, winner);
        Assert.AreEqual(before, session.Player.Wallet.Total, "落札できなければ返ってくる");
    }

    [Test]
    public void 競合に入った入札は負けても返らない()
    {
        var session = CreateSession(rivalBid: 2);
        session.BeginNextLot();
        session.EnterBidding();
        var before = session.Player.Wallet.Total;

        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        session.SubmitPlayerBid(bid);
        session.StartCompetition(0f);

        Assert.AreEqual(before - 2, session.Player.Wallet.Total, "競合に入った時点で消費される");
    }

    private static EmotionType MismatchedEmotion(EmotionType lotEmotion) =>
        EmotionWallet.ALL_EMOTIONS.First(e => e != lotEmotion);

    /// <summary>
    /// ライバル全員が同じ枚数を入れる決定的なセッションを組む
    /// </summary>
    private static AuctionSession CreateSession(int rivalBid, float competitionTimeout = 10f, string clarifiedTheme = "", int keyLotIndex = -1, int reactionScale = 100, bool distinctRivalBids = false)
    {
        var floor = ScriptableObject.CreateInstance<FloorData>();
        var lots = Enumerable.Range(0, GameConstants.LOTS_PER_FLOOR).Select(i => CreateLot(i, i == keyLotIndex)).ToList();
        var rivals = Enumerable.Range(0, 4).Select(i => CreateRival(i, distinctRivalBids ? rivalBid + i : rivalBid, reactionScale)).ToList();
        SetPrivate(floor, "floorIndex", 0);
        SetPrivate(floor, "themeTitle", "テスト");
        SetPrivate(floor, "clarifiedTheme", clarifiedTheme);
        SetPrivate(floor, "lots", lots);
        SetPrivate(floor, "rivals", rivals);

        var wallet = new EmotionWallet();
        wallet.Refill(GameConstants.EMOTION_REFILL_PER_FLOOR);
        return new AuctionSession(floor, wallet, "ノア", new System.Random(1), competitionTimeout);
    }

    private static MemoryLotData CreateLot(int index, bool isKey = false)
    {
        var lot = ScriptableObject.CreateInstance<MemoryLotData>();
        lot.name = $"lot{index}";
        SetPrivate(lot, "title", $"記憶{index}");
        SetPrivate(lot, "emotion", EmotionType.Joy);
        SetPrivate(lot, "isKey", isKey);
        return lot;
    }

    private static ParticipantData CreateRival(int index, int bid, int reactionScale = 100)
    {
        var data = ScriptableObject.CreateInstance<ParticipantData>();
        data.name = $"rival{index}";
        SetPrivate(data, "displayName", $"ライバル{index}");
        SetPrivate(data, "emotion", EmotionType.Anger);
        var profile = new BiddingProfile();
        SetPrivate(profile, "baseBid", bid);
        SetPrivate(profile, "favoriteBid", bid);
        SetPrivate(profile, "spread", 0);
        SetPrivate(profile, "competitionPolicy", CompetitionPolicy.Never);
        SetPrivate(profile, "reactionScale", reactionScale);
        SetPrivate(profile, "counterDialogueChance", 0);
        SetPrivate(data, "profile", profile);
        return data;
    }

    private static void SetPrivate(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(info, $"{target.GetType().Name} に {field} が無い");
        info.SetValue(target, value);
    }
}
