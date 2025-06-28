using UnityEngine;

/// <summary>
/// プレイヤークラス
/// ユーザーが操作するプレイヤーを表す
/// </summary>
public class Player : BasePlayer
{
    protected override void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// カードが選択された時の処理
    /// </summary>
    protected override void OnCardSelected(Card card)
    {
        // 既に選択されているカードがあれば選択解除
        var previousCard = hand.SelectedCard.CurrentValue;
        if (previousCard && previousCard != card)
        {
            hand.HighlightCard(previousCard, false);
        }
        
        // 新しいカードを選択状態で表示
        hand.HighlightCard(card, true);
    }
}