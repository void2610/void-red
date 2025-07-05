/// <summary>
/// カードの属性を表すenum
/// </summary>
public enum CardAttribute
{
    /// <summary>赦し</summary>
    Forgiveness,
    /// <summary>怒り</summary>
    Anger,
    /// <summary>不安</summary>
    Anxiety,
    /// <summary>拒絶</summary>
    Rejection,
    /// <summary>喪失</summary>
    Loss,
    /// <summary>希望</summary>
    Hope
}

/// <summary>
/// CardAttribute enumの拡張メソッド
/// </summary>
public static class CardAttributeExtensions
{
    /// <summary>
    /// 属性の日本語名を取得
    /// </summary>
    /// <param name="attribute">カード属性</param>
    /// <returns>日本語名</returns>
    public static string ToJapaneseName(this CardAttribute attribute)
    {
        return attribute switch
        {
            CardAttribute.Forgiveness => "赦し",
            CardAttribute.Anger => "怒り",
            CardAttribute.Anxiety => "不安",
            CardAttribute.Rejection => "拒絶",
            CardAttribute.Loss => "喪失",
            CardAttribute.Hope => "希望",
            _ => "不明"
        };
    }
}