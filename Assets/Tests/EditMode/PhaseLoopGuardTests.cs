using NUnit.Framework;

/// <summary>
/// 進行が止まったことを検知できるか
/// これが働かないと Play Mode で画面が固まったまま戻らなくなる
/// </summary>
public class PhaseLoopGuardTests
{
    [Test]
    public void 同じ状態が続くと異常として検知する()
    {
        var guard = new PhaseLoopGuard();

        for (var i = 0; i < PhaseLoopGuard.STUCK_THRESHOLD - 1; i++)
        {
            Assert.IsFalse(guard.IsStuck("LotResultPhase", AuctionPhase.LotResult, 0), $"{i} 回目はまだ許容する");
        }

        Assert.IsTrue(guard.IsStuck("LotResultPhase", AuctionPhase.LotResult, 0), "閾値を超えたら止まっているとみなす");
    }

    [Test]
    public void 状態が進んでいれば何度回っても異常にならない()
    {
        var guard = new PhaseLoopGuard();

        for (var lot = 0; lot < 100; lot++)
        {
            Assert.IsFalse(guard.IsStuck("DialoguePhase", AuctionPhase.Dialogue, lot));
            Assert.IsFalse(guard.IsStuck("BiddingPhase", AuctionPhase.Bidding, lot));
            Assert.IsFalse(guard.IsStuck("LotResultPhase", AuctionPhase.LotResult, lot));
        }
    }
}
