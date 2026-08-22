using System;
using System.Linq;

/// <summary>
/// NPC の入札予定を組み立て、対話への反応で書き換える
/// </summary>
public static class BidAI
{
    private const int BIG_STEP = 3;
    private const int SMALL_STEP = 1;

    /// <summary>
    /// ロット提示時の入札予定。基準枚数 ± 乱数 + 前ロットからの持ち越し
    /// </summary>
    public static EmotionBid Plan(AuctionParticipant p, MemoryLotData lot, Random rng)
    {
        var profile = p.Data.Profile;
        var isFavorite = lot.Emotion == p.Data.Emotion;
        var amount = (isFavorite ? profile.FavoriteBid : profile.BaseBid) + rng.Next(-profile.Spread, profile.Spread + 1) + p.CarryToNext;
        p.CarryToNext = 0;
        return Compose(p, lot, Math.Max(0, amount));
    }

    /// <summary>
    /// 反応を入札予定に適用する。scale は反応の効き具合 (%)
    /// </summary>
    public static void ApplyReaction(AuctionParticipant p, MemoryLotData lot, BidReaction reaction, int scale, Random rng)
    {
        var current = p.PlannedBid.Total;
        var big = Math.Max(1, BIG_STEP * scale / 100);
        var small = Math.Max(1, SMALL_STEP * scale / 100);
        if (scale == 0) return;

        var next = reaction switch
        {
            BidReaction.Increase => current + small,
            BidReaction.BigIncrease => current + big,
            BidReaction.Decrease => current - small,
            BidReaction.BigDecrease => current - big,
            BidReaction.Random => current + rng.Next(-big, big + 1),
            _ => current,
        };

        switch (reaction)
        {
            case BidReaction.Withdraw:
                if (rng.Next(100) < 50) p.Withdrawn = true;
                break;
            case BidReaction.ShiftToNext:
                next = current - small;
                p.CarryToNext += small;
                break;
            case BidReaction.PullFromNext:
                next = current + small;
                p.CarryToNext -= small;
                break;
        }

        p.PlannedBid = Compose(p, lot, Math.Max(0, next));
    }

    /// <summary>
    /// 枚数を属性に割り付ける。司る感情 → ロットの感情 → 残りは多い順
    /// </summary>
    private static EmotionBid Compose(AuctionParticipant p, MemoryLotData lot, int amount)
    {
        var bid = new EmotionBid();
        var remaining = Math.Min(amount, p.Wallet.Total);
        var order = new[] { p.Data.Emotion, lot.Emotion }
            .Concat(EmotionWallet.ALL_EMOTIONS.OrderByDescending(e => p.Wallet.Get(e)))
            .Distinct();
        foreach (var e in order)
        {
            if (remaining == 0) break;
            var take = Math.Min(remaining, p.Wallet.Get(e));
            bid.Set(e, take);
            remaining -= take;
        }
        return bid;
    }
}
