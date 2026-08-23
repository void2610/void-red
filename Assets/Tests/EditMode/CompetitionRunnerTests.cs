using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 競合が必ず決着することを保証する
/// ここが破れると Play Mode でループが終わらず Editor ごと固まるため、条件を変えて総当たりで確かめる
/// </summary>
public class CompetitionRunnerTests
{
    private const float STEP = 0.1f;
    private const float GIVE_UP_SECONDS = 600f;

    [Test]
    public void どのライバル構成でも競合は必ず決着する(
        [Values(1, 2, 3, 4)] int rivalCount,
        [Values(CompetitionPolicy.AllIn, CompetitionPolicy.FavoriteAggressive, CompetitionPolicy.Normal, CompetitionPolicy.ByBidSize)] CompetitionPolicy policy)
    {
        var session = CreateTiedSession(rivalCount, policy, competitionTimeout: 10f);
        var elapsed = RunToEnd(session);

        Assert.Less(elapsed, GIVE_UP_SECONDS, $"{rivalCount} 人 / {policy} で競合が終わらない");
        Assert.IsTrue(session.Competition.IsTimedOut(elapsed) || session.Competition.IsDeadlocked());
        Assert.IsNotNull(session.ResolveCompetition(), "決着したら落札者が決まる");
    }

    [Test]
    public void 競合は確定時間の打ち切り内で必ず終わる()
    {
        var session = CreateTiedSession(rivalCount: 4, CompetitionPolicy.AllIn, competitionTimeout: 4f);
        var elapsed = RunToEnd(session);

        var hardLimit = 4f * GameConstants.COMPETITION_HARD_LIMIT_RATIO;
        Assert.LessOrEqual(elapsed, hardLimit + 1f, "上乗せが続いても打ち切り時間で終わる");
    }

    [Test]
    public void 手持ちが尽きても競合は終わる()
    {
        var session = CreateTiedSession(rivalCount: 2, CompetitionPolicy.AllIn, competitionTimeout: 10f);
        foreach (var rival in session.Rivals) rival.Wallet.LoadCounts(new int[EmotionWallet.ALL_EMOTIONS.Length]);

        var elapsed = RunToEnd(session);

        Assert.Less(elapsed, GIVE_UP_SECONDS);
    }

    [Test]
    public void 無落札のまま終盤に入っても競合は決着する(
        [Values(1, 2, 3, 4)] int rivalCount,
        [Values(CompetitionPolicy.AllIn, CompetitionPolicy.FavoriteAggressive, CompetitionPolicy.Normal, CompetitionPolicy.ByBidSize)] CompetitionPolicy policy)
    {
        // 残りロットが尽きかけた無落札の NPC は上乗せ上限が広がるため、決着保証を別途確かめる
        var session = CreateTiedSession(rivalCount, policy, competitionTimeout: 10f, lotCount: GameConstants.DESPERATE_REMAINING_LOTS);
        var elapsed = RunToEnd(session);

        Assert.Less(elapsed, GIVE_UP_SECONDS, $"{rivalCount} 人 / {policy} で競合が終わらない");
        Assert.IsNotNull(session.ResolveCompetition(), "決着したら落札者が決まる");
    }

    [Test]
    public void 無落札のまま終盤に入った相手は通常の上限を超えて食い下がる()
    {
        var session = CreateTiedSession(rivalCount: 2, CompetitionPolicy.AllIn, competitionTimeout: 10f, lotCount: GameConstants.DESPERATE_REMAINING_LOTS);
        RunToEnd(session);

        var deepest = session.Rivals.Max(r => session.Competition.RaisesOf(r).Total);
        Assert.Greater(deepest, GameConstants.NPC_MAX_RAISE_MARGIN, "終盤の無落札キャラは上限を広げて競り上げる");
    }

    /// <summary>決着するまで時間を進める。戻り値は経過秒</summary>
    private static float RunToEnd(AuctionSession session)
    {
        var runner = new CompetitionRunner(session, session.Rng, 0f);
        var now = 0f;
        while (runner.Step(now) && now < GIVE_UP_SECONDS) now += STEP;
        return now;
    }

    /// <summary>全員同額で競合に入った状態のセッションを作る</summary>
    private static AuctionSession CreateTiedSession(int rivalCount, CompetitionPolicy policy, float competitionTimeout, int lotCount = GameConstants.LOTS_PER_FLOOR)
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 2, competitionTimeout: competitionTimeout, rivalCount: rivalCount, policy: policy, lotCount: lotCount);
        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        var reveal = session.SubmitPlayerBid(bid);
        Assert.IsTrue(reveal.IsTie, "全員同額で競合に入る前提");
        session.StartCompetition(0f);
        return session;
    }
}
