using UnityEngine;

/// <summary>
/// プレイヤークラス
/// ユーザーが操作するプレイヤーを表す
/// </summary>
public class Player : BasePlayer
{
    [Header("プレイヤー専用設定")]
    [SerializeField] private float cardSpacing = 150f; // カード間の間隔
    [SerializeField] private float cardAngle = 5f; // カードの角度
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// 手札を扇状に配置
    /// </summary>
    protected override void ArrangeHand()
    {
        if (!handContainer) return;
        
        var cardCount = Hand.Value.Count;
        if (cardCount == 0) return;
        
        // 中心から左右に配置
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        
        for (int i = 0; i < cardCount; i++)
        {
            var card = Hand.Value[i];
            if (!card) continue;
            
            // 位置を設定
            var rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform)
            {
                rectTransform.anchoredPosition = new Vector2(startX + i * cardSpacing, 0);
                
                // 扇状に角度をつける
                var angle = (i - (cardCount - 1) * 0.5f) * cardAngle;
                rectTransform.rotation = Quaternion.Euler(0, 0, -angle);
            }
        }
    }
    
    /// <summary>
    /// カードが選択された時の処理
    /// </summary>
    protected override void OnCardSelected(Card card)
    {
        // 既に選択されているカードがあれば選択解除
        if (selectedCard.Value && selectedCard.Value != card)
        {
            HighlightCard(selectedCard.Value, false);
        }
        
        // 新しいカードを選択
        base.OnCardSelected(card);
        
        // 選択状態を視覚的に表示
        HighlightCard(card, true);
    }
    
    /// <summary>
    /// カードのハイライト表示
    /// </summary>
    private void HighlightCard(Card card, bool highlight)
    {
        if (!card) return;
        
        var rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform)
        {
            // 選択時は少し上に移動
            var currentPos = rectTransform.anchoredPosition;
            currentPos.y = highlight ? 30f : 0f;
            rectTransform.anchoredPosition = currentPos;
        }
    }
}