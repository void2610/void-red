using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// テスト用のオークションセッションを組み立てる
/// ScriptableObject は Inspector 前提なのでリフレクションで値を入れる
/// </summary>
public static class AuctionTestFactory
{
    public static AuctionSession CreateSession(
        int rivalBid,
        float competitionTimeout = 10f,
        string clarifiedTheme = "",
        int keyLotIndex = -1,
        int reactionScale = 100,
        bool distinctRivalBids = false,
        int rivalCount = 4,
        CompetitionPolicy policy = CompetitionPolicy.Never,
        int lotCount = GameConstants.LOTS_PER_FLOOR,
        int resonance = 0)
    {
        var floor = ScriptableObject.CreateInstance<FloorData>();
        var lots = Enumerable.Range(0, lotCount).Select(i => CreateLot(i, i == keyLotIndex, resonance)).ToList();
        var rivals = Enumerable.Range(0, rivalCount)
            .Select(i => CreateRival(i, distinctRivalBids ? rivalBid + i : rivalBid, reactionScale, policy))
            .ToList();
        SetPrivate(floor, "floorIndex", 0);
        SetPrivate(floor, "themeTitle", "テスト");
        SetPrivate(floor, "clarifiedTheme", clarifiedTheme);
        SetPrivate(floor, "lots", lots);
        SetPrivate(floor, "rivals", rivals);

        var wallet = new EmotionWallet();
        wallet.Refill(GameConstants.EMOTION_REFILL_PER_FLOOR);
        return new AuctionSession(floor, wallet, "ノア", new System.Random(1), competitionTimeout);
    }

    public static MemoryLotData CreateLot(int index, bool isKey = false, int resonance = 0)
    {
        var lot = ScriptableObject.CreateInstance<MemoryLotData>();
        lot.name = $"lot{index}";
        SetPrivate(lot, "title", $"記憶{index}");
        SetPrivate(lot, "emotion", EmotionType.Joy);
        SetPrivate(lot, "resonance", resonance);
        SetPrivate(lot, "isKey", isKey);
        return lot;
    }

    public static ParticipantData CreateRival(int index, int bid, int reactionScale = 100, CompetitionPolicy policy = CompetitionPolicy.Never)
    {
        var data = ScriptableObject.CreateInstance<ParticipantData>();
        data.name = $"rival{index}";
        SetPrivate(data, "displayName", $"ライバル{index}");
        SetPrivate(data, "emotion", EmotionType.Anger);
        var profile = new BiddingProfile();
        SetPrivate(profile, "baseBid", bid);
        SetPrivate(profile, "favoriteBid", bid);
        SetPrivate(profile, "spread", 0);
        SetPrivate(profile, "competitionPolicy", policy);
        SetPrivate(profile, "counterDialogueChance", 0);
        SetPrivate(profile, "reactionScale", reactionScale);
        SetPrivate(data, "profile", profile);
        return data;
    }

    public static void SetPrivate(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(info, $"{target.GetType().Name} に {field} が無い");
        info.SetValue(target, value);
    }
}
