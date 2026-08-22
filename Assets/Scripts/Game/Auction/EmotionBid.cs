using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 1 ロットへの入札内訳 (属性ごとの枚数)
/// 勝敗判定は Total のみで行い、属性は歪みの計算にだけ使う
/// </summary>
public class EmotionBid
{
    public int Total => _counts.Values.Sum();

    private readonly Dictionary<EmotionType, int> _counts = EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, _ => 0);

    public int Get(EmotionType emotion) => _counts[emotion];

    public void Add(EmotionType emotion, int amount = 1) => _counts[emotion] += amount;

    public void Set(EmotionType emotion, int amount) => _counts[emotion] = amount < 0 ? 0 : amount;

    /// <summary>
    /// 歪み = 記憶の感情属性と一致しない枚数
    /// </summary>
    public int DistortionAgainst(EmotionType lotEmotion) => Total - _counts[lotEmotion];

    public IReadOnlyList<int> ToCounts() => EmotionWallet.ALL_EMOTIONS.Select(e => _counts[e]).ToList();

    public override string ToString() => string.Join(",", EmotionWallet.ALL_EMOTIONS.Where(e => _counts[e] > 0).Select(e => $"{e}:{_counts[e]}"));

    public void Clear()
    {
        foreach (var e in EmotionWallet.ALL_EMOTIONS) _counts[e] = 0;
    }

    public EmotionBid Clone()
    {
        var clone = new EmotionBid();
        foreach (var e in EmotionWallet.ALL_EMOTIONS) clone._counts[e] = _counts[e];
        return clone;
    }

    /// <summary>
    /// 最も多く入札した属性。同数なら感情の輪の順で先のもの
    /// </summary>
    public EmotionType DominantEmotion()
    {
        var best = EmotionWallet.ALL_EMOTIONS[0];
        foreach (var e in EmotionWallet.ALL_EMOTIONS) if (_counts[e] > _counts[best]) best = e;
        return best;
    }
}
