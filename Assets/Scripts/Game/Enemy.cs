using UnityEngine;
using System.Linq;

/// <summary>
/// NPCクラス
/// AIによって制御される対戦相手を表す
/// </summary>
public class Enemy : BasePlayer
{
    /// <summary>
    /// AIによるカード選択
    /// </summary>
    public Card SelectCardByAI()
    {
        if (hand.Count == 0) return null;
        
        var selectedCard = SelectRandomCard();
        // AIが選択したカードを設定（Handクラス経由）
        hand.SetSelectedCard(selectedCard);
        return selectedCard;
    }
    
    /// <summary>
    /// ランダムにカードを選択
    /// </summary>
    private Card SelectRandomCard()
    {
        var cards = hand.Cards.CurrentValue;
        var randomIndex = Random.Range(0, cards.Count);
        return cards[randomIndex];
    }
}