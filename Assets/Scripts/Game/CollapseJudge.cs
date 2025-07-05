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
    /// <param name="move">プレイヤーの手</param>
    /// <returns>崩壊確率（0.0～1.0）</returns>
    private static float CalculateCollapseChance(PlayerMove move)
    {
        if (move == null || !move.SelectedCard) return 0f;
        
        var threshold = move.SelectedCard.CollapseThreshold;
        if (move.MentalBet < threshold) return 0f; // 閾値未満では崩壊しない
        
        // 基本崩壊確率を計算
        var baseChance = (move.MentalBet - threshold) * 0.2f;
        
        // プレイスタイルによる倍率を適用
        var playStyleMultiplier = move.PlayStyle.GetCollapseMultiplier();
        
        return baseChance * playStyleMultiplier;
    }
    
    /// <summary>
    /// カードが崩壊するかどうかを判定
    /// </summary>
    /// <param name="move">プレイヤーの手</param>
    /// <returns>崩壊するかどうか</returns>
    public static bool ShouldCollapse(PlayerMove move)
    {
        var chance = CalculateCollapseChance(move);
        var randomValue = Random.Range(0f, 1f);
        return randomValue < chance;
    }
}