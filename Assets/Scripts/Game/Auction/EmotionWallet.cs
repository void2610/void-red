using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 参加者が所持する感情リソース (8 属性 × 枚数)
/// </summary>
public class EmotionWallet
{
    public int Total => _counts.Values.Sum();
    public static readonly EmotionType[] ALL_EMOTIONS = (EmotionType[])Enum.GetValues(typeof(EmotionType));

    private readonly Dictionary<EmotionType, int> _counts = ALL_EMOTIONS.ToDictionary(e => e, _ => 0);

    public int Get(EmotionType emotion) => _counts[emotion];

    public void Add(EmotionType emotion, int amount) => _counts[emotion] += amount;

    public bool CanAfford(EmotionBid bid) => ALL_EMOTIONS.All(e => _counts[e] >= bid.Get(e));

    public IReadOnlyList<int> ToCounts() => ALL_EMOTIONS.Select(e => _counts[e]).ToList();

    public override string ToString() => string.Join(",", ALL_EMOTIONS.Select(e => $"{e}:{_counts[e]}"));

    public EmotionWallet Clone()
    {
        var clone = new EmotionWallet();
        foreach (var e in ALL_EMOTIONS) clone._counts[e] = _counts[e];
        return clone;
    }

    /// <summary>
    /// 各属性に規定枚数を補充する (階層開始時。前階層の残りはそのまま持ち越す)
    /// </summary>
    public void Refill(int perEmotion)
    {
        foreach (var e in ALL_EMOTIONS) _counts[e] += perEmotion;
    }

    public bool TryConsume(EmotionType emotion, int amount)
    {
        if (amount < 0 || _counts[emotion] < amount) return false;
        _counts[emotion] -= amount;
        return true;
    }

    /// <summary>
    /// 入札内訳をまとめて消費する。足りなければ何も消費せず false
    /// </summary>
    public bool TryConsume(EmotionBid bid)
    {
        if (!CanAfford(bid)) return false;
        foreach (var e in ALL_EMOTIONS) _counts[e] -= bid.Get(e);
        return true;
    }

    public void Refund(EmotionBid bid)
    {
        foreach (var e in ALL_EMOTIONS) _counts[e] += bid.Get(e);
    }

    public void LoadCounts(IReadOnlyList<int> counts)
    {
        for (var i = 0; i < ALL_EMOTIONS.Length && i < counts.Count; i++) _counts[ALL_EMOTIONS[i]] = counts[i];
    }
}
