using System.Linq;
using NUnit.Framework;

/// <summary>
/// フェーズ遷移に穴が無いことを確かめる
/// 実行できるフェーズが無い状態に落ちると進行が止まり、画面が固まったまま戻れなくなる
/// </summary>
public class AuctionFlowTests
{
    [Test]
    public void 一巡ぶん進めても常に実行できるフェーズがある()
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 1);
        var phases = AuctionFlow.CreatePhases();

        AssertRunnable(phases, session, "開始直後");

        session.BeginNextLot();
        AssertRunnable(phases, session, "ロット提示後");

        session.EnterBidding();
        AssertRunnable(phases, session, "入札フェーズ");

        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 3);
        session.SubmitPlayerBid(bid);
        AssertRunnable(phases, session, "開示直後");

        session.ResolveReveal();
        AssertRunnable(phases, session, "落札確定後");
    }

    [Test]
    public void 同数で競合に入った直後も実行できるフェーズがある()
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 2);
        var phases = AuctionFlow.CreatePhases();

        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 2);
        session.SubmitPlayerBid(bid);

        AssertRunnable(phases, session, "競合に入った直後");
        session.StartCompetition(0f);
        AssertRunnable(phases, session, "競合中");
        session.ResolveCompetition();
        AssertRunnable(phases, session, "競合の決着後");
    }

    [Test]
    public void 洗礼とゲームオーバーにも実行できるフェーズがある()
    {
        var cleared = PlayFloor(winEveryLot: true);
        Assert.AreEqual(AuctionPhase.Baptism, cleared.Phase);
        AssertRunnable(AuctionFlow.CreatePhases(), cleared, "洗礼");

        var lost = PlayFloor(winEveryLot: false);
        Assert.AreEqual(AuctionPhase.GameOver, lost.Phase);
        AssertRunnable(AuctionFlow.CreatePhases(), lost, "ゲームオーバー");
    }

    [Test]
    public void 開示の演出は一つのロットにつき一度だけ選ばれる()
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 1);
        var phases = AuctionFlow.CreatePhases();
        session.BeginNextLot();
        session.EnterBidding();
        var bid = new EmotionBid();
        bid.Set(session.CurrentLot.Emotion, 3);
        session.SubmitPlayerBid(bid);

        Assert.IsInstanceOf<RevealPhase>(Select(phases, session), "まず開示の演出");
        session.RevealShown = true;
        Assert.IsNotInstanceOf<RevealPhase>(Select(phases, session), "同じロットで二度は選ばれない");
    }

    private static AuctionSession PlayFloor(bool winEveryLot)
    {
        var session = AuctionTestFactory.CreateSession(rivalBid: 0);
        for (var i = 0; i < GameConstants.LOTS_PER_FLOOR; i++)
        {
            session.BeginNextLot();
            session.EnterBidding();
            var bid = new EmotionBid();
            if (winEveryLot) bid.Set(session.CurrentLot.Emotion, 1);
            session.SubmitPlayerBid(bid);
            session.ResolveReveal();
        }
        session.FinishLots();
        return session;
    }

    private static IAuctionPhase Select(System.Collections.Generic.IReadOnlyList<IAuctionPhase> phases, AuctionSession session) =>
        phases.FirstOrDefault(p => p.CanRun(session));

    private static void AssertRunnable(System.Collections.Generic.IReadOnlyList<IAuctionPhase> phases, AuctionSession session, string situation)
    {
        Assert.IsNotNull(Select(phases, session), $"{situation} ({session.Phase}) で実行できるフェーズが無い");
    }
}
