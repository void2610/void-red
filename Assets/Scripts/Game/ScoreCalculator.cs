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
    /// <param name="theme">テーマのカードステータス</param>
    /// <returns>計算されたスコア</returns>
    public static float CalculateScore(CardData selectedCard, int mentalBet, CardStatus theme)
    {
        if (!selectedCard || theme == null) return 0f;
        
        // テーマとの距離を計算
        var distance = selectedCard.Effect.GetDistanceTo(theme);
        
        // 距離を一致度に変換（距離が小さいほど一致度が高い）
        // 距離の範囲を0～√3（最大距離）として、一致度を1.0～1.5に正規化
        var matchRate = 1.0f + (1.0f - (distance / Mathf.Sqrt(3f))) * 0.5f;
        
        // スコア = 一致度 × 精神ベット × カード固有の倍率
        return matchRate * mentalBet * selectedCard.ScoreMultiplier;
    }
    
    /// <summary>
    /// 2つのカードステータス間の一致度を計算
    /// </summary>
    /// <param name="cardStatus">カードのステータス</param>
    /// <param name="theme">テーマのステータス</param>
    /// <returns>一致度（1.0～1.5）</returns>
    public static float CalculateMatchRate(CardStatus cardStatus, CardStatus theme)
    {
        if (cardStatus == null || theme == null) return 1.0f;
        
        var distance = cardStatus.GetDistanceTo(theme);
        return 1.0f + (1.0f - (distance / Mathf.Sqrt(3f))) * 0.5f;
    }
}