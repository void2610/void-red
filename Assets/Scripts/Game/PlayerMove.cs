using UnityEngine;

/// <summary>
/// プレイヤーの手（カード選択、プレイスタイル、精神ベット）をまとめたクラス
/// </summary>
[System.Serializable]
public class PlayerMove
{
    /// <summary>
    /// 選択されたカード
    /// </summary>
    public Card SelectedCard { get; private set; }
    
    /// <summary>
    /// プレイスタイル（迷い/衝動/確信）
    /// </summary>
    public PlayStyle PlayStyle { get; private set; }
    
    /// <summary>
    /// 精神ベット値
    /// </summary>
    public int MentalBet { get; private set; }
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="selectedCard">選択されたカード</param>
    /// <param name="playStyle">プレイスタイル</param>
    /// <param name="mentalBet">精神ベット値</param>
    public PlayerMove(Card selectedCard, PlayStyle playStyle, int mentalBet)
    {
        SelectedCard = selectedCard;
        PlayStyle = playStyle;
        MentalBet = mentalBet;
    }
    
    /// <summary>
    /// スコアを計算（テーマとの一致度 × 精神ベット × カード倍率）
    /// </summary>
    /// <param name="theme">テーマのカードステータス</param>
    /// <returns>計算されたスコア</returns>
    public float GetScore(CardStatus theme)
    {
        // テーマとの距離を計算
        var distance = SelectedCard.CardData.Effect.GetDistanceTo(theme);
        
        // 距離を一致度に変換（距離が小さいほど一致度が高い）
        // 距離の範囲を0～√3（最大距離）として、一致度を1.0～1.5に正規化
        var matchRate = 1.0f + (1.0f - (distance / Mathf.Sqrt(3f))) * 0.5f;
        
        // スコア = 一致度 × 精神ベット × カード固有の倍率
        return matchRate * MentalBet * SelectedCard.CardData.ScoreMultiplier;
    }
    
    /// <summary>
    /// カードの崩壊確率を計算
    /// </summary>
    /// <returns>崩壊確率（0.0～1.0）</returns>
    public float GetCollapseChance()
    {
        // 精神ベット値に基づいて崩壊確率を計算
        // ベット0: 0%, ベット1: 5%, ベット2: 10%, ..., ベット7: 35%
        return MentalBet * 0.05f;
    }
    
    /// <summary>
    /// カードが崩壊するかどうかを判定
    /// </summary>
    /// <returns>崩壊するかどうか</returns>
    public bool ShouldCollapse()
    {
        var chance = GetCollapseChance();
        var randomValue = UnityEngine.Random.Range(0f, 1f);
        return randomValue < chance;
    }
    
    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    public override string ToString()
    {
        var cardName = SelectedCard.CardData.CardName;
        var playStyleText = PlayStyle.ToJapaneseString();
        var collapseChance = GetCollapseChance() * 100f;
        return $"カード: {cardName}, スタイル: {playStyleText}, 精神ベット: {MentalBet}, 崩壊率: {collapseChance:F1}%";
    }
}