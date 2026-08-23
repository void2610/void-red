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

    [Test]
    public void 逆対話は観察の成否によらず各ロット一度だけ起こる()
    {
        // 必ず逆対話を仕掛ける相手を作る
        var session = AuctionTestFactory.CreateSession(rivalBid: 2, rivalCount: 1);
        var rival = session.Rivals[0];
        AuctionTestFactory.SetPrivate(rival.Data.Profile, "counterDialogueChance", 100);
        session.BeginNextLot();

        var first = session.UseDialogue(rival, DialogueCommand.Observe);
        Assert.IsNotNull(first.Counter, "観察を仕掛けたら (成否によらず) 逆対話が起こる");
        Assert.IsNotEmpty(first.Line, "失敗してもセリフは返る");
        Assert.IsTrue(rival.CounterFiredThisLot, "同じロットでは二度起こらない");

        // 次のロットへ進める
        session.EnterBidding();
        session.SubmitPlayerBid(new EmotionBid());
        session.ResolveReveal();
        session.BeginNextLot();

        var second = session.UseDialogue(rival, DialogueCommand.Observe);
        Assert.IsNotNull(second.Counter, "ロットが変われば再び起こる");
    }

    [Test]
    public void 逆対話への返答で相手の入札予定が動く()
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 3, rivalCount: 1);
        var rival = session.Rivals[0];
        AuctionTestFactory.SetPrivate(rival.Data.Profile, "counterDialogueChance", 100);
        session.BeginNextLot();

        var outcome = session.UseDialogue(rival, DialogueCommand.Observe);
        var before = rival.PlannedBid.Total;
        session.AnswerCounterDialogue(rival, 0);

        Assert.AreNotEqual(before, rival.PlannedBid.Total, "選択肢 A は入札予定を大きく動かす");
    }

    private static EmotionType MismatchedEmotion(EmotionType lotEmotion) =>
        EmotionWallet.ALL_EMOTIONS.First(e => e != lotEmotion);

    private static AuctionSession CreateSession(int rivalBid, float competitionTimeout = 10f, string clarifiedTheme = "", int keyLotIndex = -1, int reactionScale = 100, bool distinctRivalBids = false) =>
        AuctionTestFactory.CreateSession(rivalBid, competitionTimeout, clarifiedTheme, keyLotIndex, reactionScale, distinctRivalBids);

    [Test]
    public void 共鳴の高い記憶ほどライバルの入札が厚くなる()
    {
        var plain = PlannedTotal(resonance: 0);
        var headline = PlannedTotal(resonance: 100);

        Assert.Greater(headline, plain, "目玉の記憶には入札が集まる");
    }

    /// <summary>指定した共鳴値のロットに対するライバルの入札予定枚数</summary>
    private static int PlannedTotal(int resonance)
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 2, resonance: resonance);
        session.BeginNextLot();
        return session.Rivals[0].PlannedBid.Total;
    }
}
