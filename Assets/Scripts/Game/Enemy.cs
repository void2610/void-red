using UnityEngine;
using System.Linq;

/// <summary>
/// NPCクラス
/// AIによって制御される対戦相手を表す
/// </summary>
public class Enemy : BasePlayer
{
    /// <summary>
    /// NPCの手札は非表示または整列表示
    /// </summary>
    protected override void ArrangeHand()
    {
        var cardCount = Hand.Value.Count;
        if (cardCount == 0) return;
        
        for (var i = 0; i < cardCount; i++)
        {
            var card = Hand.Value[i];
            if (!card) continue;
            
            var rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform)
            {
                // 横一列に配置
                var spacing = 120f;
                var startX = -(cardCount - 1) * spacing * 0.5f;
                rectTransform.anchoredPosition = new Vector2(startX + i * spacing, 0);
                rectTransform.rotation = Quaternion.identity;
            }
        }
    }
    
    /// <summary>
    /// AIによるカード選択
    /// </summary>
    public Card SelectCardByAI()
    {
        if (Hand.Value.Count == 0) return null;
        selectedCard.Value = SelectRandomCard();
        return selectedCard.Value;
    }
    
    /// <summary>
    /// ランダムにカードを選択
    /// </summary>
    private Card SelectRandomCard()
    {
        var randomIndex = Random.Range(0, Hand.Value.Count);
        return Hand.Value[randomIndex];
    }
}