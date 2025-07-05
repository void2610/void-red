using UnityEngine;

/// <summary>
/// スコア計算を担当する静的クラス
/// ゲームルールに関するスコア計算ロジックを集約
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// プレイヤーの手のスコアを計算
    /// </summary>
    /// <param name="selectedCard">選択されたカード</param>
    /// <param name="mentalBet">精神ベット値</param>
    /// <param name="themeAttribute">テーマの属性</param>
    /// <returns>計算されたスコア</returns>
    public static float CalculateScore(CardData selectedCard, int mentalBet, CardAttribute themeAttribute)
    {
        if (!selectedCard) return 0f;
        
        // 属性が一致する場合は1.5倍、一致しない場合は1.0倍
        var matchRate = selectedCard.Attribute == themeAttribute ? 1.5f : 1.0f;
        
        // スコア = 一致度 × 精神ベット × カード固有の倍率
        return matchRate * mentalBet * selectedCard.ScoreMultiplier;
    }
}