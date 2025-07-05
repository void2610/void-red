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
    /// <param name="move">プレイヤーの手（カード選択、プレイスタイル、精神ベット）</param>
    /// <param name="theme">テーマデータ</param>
    /// <returns>計算されたスコア</returns>
    public static float CalculateScore(PlayerMove move, ThemeData theme)
    {
        if (move == null || !theme) return 0f;
        
        // テーマから該当属性の倍率を取得
        var attributeMultiplier = theme.GetMultiplier(move.SelectedCard.Attribute);
        
        // スコア = 属性倍率 × 精神ベット × カード固有の倍率
        return attributeMultiplier * move.MentalBet * move.SelectedCard.ScoreMultiplier;
    }
}