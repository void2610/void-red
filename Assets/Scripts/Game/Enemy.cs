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
    public CardView SelectCardByAI()
    {
        if (!handView || handView.Count == 0) return null;
        
        var cards = handView.Cards.CurrentValue;
        var selectedCard = cards[Random.Range(0, cards.Count)];
        handView.SetSelectedCard(selectedCard);
        return selectedCard;
    }
}