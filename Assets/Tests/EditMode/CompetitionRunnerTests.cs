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

    /// <summary>決着するまで時間を進める。戻り値は経過秒</summary>
    private static float RunToEnd(AuctionSession session)
    {
        var runner = new CompetitionRunner(session, session.Rng, 0f);
        var now = 0f;
        while (runner.Step(now) && now < GIVE_UP_SECONDS) now += STEP;
        return now;
    }

    /// <summary>全員同額で競合に入った状態のセッションを作る</summary>
    private static AuctionSession CreateTiedSession(int rivalCount, CompetitionPolicy policy, float competitionTimeout)
    {
        var floor = ScriptableObject.CreateInstance<FloorData>();
        var lots = Enumerable.Range(0, GameConstants.LOTS_PER_FLOOR).Select(CreateLot).ToList();
        var rivals = Enumerable.Range(0, rivalCount).Select(i => CreateRival(i, policy)).ToList();
        SetPrivate(floor, "floorIndex", 0);
        SetPrivate(floor, "themeTitle", "テスト");
        SetPrivate(floor, "clarifiedTheme", "");
        SetPrivate(floor, "lots", lots);
        SetPrivate(floor, "rivals", rivals);

        var wallet = new EmotionWallet();
        wallet.Refill(GameConstants.EMOTION_REFILL_PER_FLOOR);
        var session = new AuctionSession(floor, wallet, "ノア", new System.Random(1), competitionTimeout);

        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        var reveal = session.SubmitPlayerBid(bid);
        Assert.IsTrue(reveal.IsTie, "全員同額で競合に入る前提");
        session.StartCompetition(0f);
        return session;
    }

    private static MemoryLotData CreateLot(int index)
    {
        var lot = ScriptableObject.CreateInstance<MemoryLotData>();
        lot.name = $"lot{index}";
        SetPrivate(lot, "title", $"記憶{index}");
        SetPrivate(lot, "emotion", EmotionType.Joy);
        return lot;
    }

    private static ParticipantData CreateRival(int index, CompetitionPolicy policy)
    {
        var data = ScriptableObject.CreateInstance<ParticipantData>();
        data.name = $"rival{index}";
        SetPrivate(data, "displayName", $"ライバル{index}");
        SetPrivate(data, "emotion", EmotionType.Joy);
        var profile = new BiddingProfile();
        SetPrivate(profile, "baseBid", 2);
        SetPrivate(profile, "favoriteBid", 2);
        SetPrivate(profile, "spread", 0);
        SetPrivate(profile, "competitionPolicy", policy);
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
