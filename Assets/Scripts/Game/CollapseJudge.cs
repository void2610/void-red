using UnityEngine;

/// <summary>
/// カードの崩壊判定を担当する静的クラス
/// カードの崩壊に関するゲームルールを集約
/// </summary>
public static class CollapseJudge
{
    /// <summary>
    /// カードの崩壊確率を計算
    /// </summary>
    /// <param name="selectedCard">選択されたカード</param>
    /// <param name="mentalBet">精神ベット値</param>
    /// <returns>崩壊確率（0.0～1.0）</returns>
    private static float CalculateCollapseChance(CardData selectedCard, int mentalBet)
    {
        if (!selectedCard) return 0f;
        
        var threshold = selectedCard.CollapseThreshold;
        if (mentalBet < threshold) return 0f; // 閾値未満では崩壊しない
        
        // 閾値を超えた場合、崩壊確率を計算
        return (mentalBet - threshold) * 0.2f;
    }
    
    /// <summary>
    /// カードが崩壊するかどうかを判定
    /// </summary>
    /// <param name="selectedCard">選択されたカード</param>
    /// <param name="mentalBet">精神ベット値</param>
    /// <returns>崩壊するかどうか</returns>
    public static bool ShouldCollapse(CardData selectedCard, int mentalBet)
    {
        var chance = CalculateCollapseChance(selectedCard, mentalBet);
        var randomValue = Random.Range(0f, 1f);
        return randomValue < chance;
    }
}