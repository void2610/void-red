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
    /// テーマとの距離を計算（プレイスタイルと精神ベットによる補正を含む）
    /// </summary>
    /// <param name="theme">テーマのカードステータス</param>
    /// <returns>補正後の距離</returns>
    public float GetDistanceTo(CardStatus theme)
    {
        // 基本距離を計算
        float baseDistance = SelectedCard.CardData.Effect.GetDistanceTo(theme);
        
        // プレイスタイルによる補正（将来的な拡張用）
        float playStyleModifier = 1.0f;
        switch (PlayStyle)
        {
            case PlayStyle.Hesitation:
                // 迷いは距離をわずかに増やす（不利になる）
                playStyleModifier = 1.1f;
                break;
            case PlayStyle.Impulse:
                // 衝動は変化なし
                playStyleModifier = 1.0f;
                break;
            case PlayStyle.Conviction:
                // 確信は距離をわずかに減らす（有利になる）
                playStyleModifier = 0.9f;
                break;
        }
        
        // 精神ベットによる補正（将来的な拡張用）
        // 高い精神ベットほどリスクとリターンが大きくなる
        float mentalBetModifier = 1.0f + (MentalBet - 3) * 0.02f; // ベット3を基準に、±2%ずつ変化
        
        // 最終的な距離を計算
        return baseDistance * playStyleModifier * mentalBetModifier;
    }
    
    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    public override string ToString()
    {
        var cardName = SelectedCard.CardData.CardName;
        var playStyleText = PlayStyle.ToJapaneseString();
        return $"カード: {cardName}, スタイル: {playStyleText}, 精神ベット: {MentalBet}";
    }
}