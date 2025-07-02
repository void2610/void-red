using UnityEngine;

/// <summary>
/// プレイヤーの手（カード選択、プレイスタイル、精神ベット）をまとめたクラス
/// </summary>
[System.Serializable]
public class PlayerMove
{
    /// <summary>
    /// 選択されたカードデータ
    /// </summary>
    public CardData SelectedCard { get; private set; }
    
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
    /// <param name="selectedCard">選択されたCardData</param>
    /// <param name="playStyle">プレイスタイル</param>
    /// <param name="mentalBet">精神ベット値</param>
    public PlayerMove(CardData selectedCard, PlayStyle playStyle, int mentalBet)
    {
        SelectedCard = selectedCard;
        PlayStyle = playStyle;
        MentalBet = mentalBet;
    }
    
    /// <summary>
    /// スコアを計算（ScoreCalculatorを使用）
    /// </summary>
    /// <param name="theme">テーマのカードステータス</param>
    /// <returns>計算されたスコア</returns>
    public float GetScore(CardStatus theme)
    {
        return ScoreCalculator.CalculateScore(SelectedCard, MentalBet, theme);
    }
    
    /// <summary>
    /// カードが崩壊するかどうかを判定
    /// </summary>
    /// <returns>崩壊するかどうか</returns>
    public bool ShouldCollapse()
    {
        return CollapseJudge.ShouldCollapse(SelectedCard, MentalBet);
    }
}