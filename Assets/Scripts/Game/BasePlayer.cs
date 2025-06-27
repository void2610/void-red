using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// プレイヤーとNPCの基底クラス
/// カードデッキを持ち、カード選択の基本機能を提供
/// </summary>
public abstract class BasePlayer : MonoBehaviour
{
    [SerializeField] protected int maxHandSize = 5;
    [SerializeField] protected Transform handContainer; // 手札を配置するコンテナ
    [SerializeField] protected Card cardPrefab; // カードのプレハブ
    
    // デッキと手札
    protected readonly List<CardData> Deck = new ();
    protected readonly ReactiveProperty<List<Card>> Hand = new (new List<Card>());
    
    // 選択中のカード
    protected readonly ReactiveProperty<Card> selectedCard = new (null);
    
    public ReadOnlyReactiveProperty<Card> SelectedCard => selectedCard;
    
    protected virtual void Awake()
    {
    }
    
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public virtual void InitializeDeck(List<CardData> cardDataList)
    {
        Deck.Clear();
        Deck.AddRange(cardDataList);
        ShuffleDeck();
    }
    
    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    protected virtual void ShuffleDeck()
    {
        for (var i = 0; i < Deck.Count; i++)
        {
            var temp = Deck[i];
            var randomIndex = Random.Range(i, Deck.Count);
            Deck[i] = Deck[randomIndex];
            Deck[randomIndex] = temp;
        }
    }
    
    /// <summary>
    /// カードを引く
    /// </summary>
    public virtual void DrawCard(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            if (Deck.Count == 0 || Hand.Value.Count >= maxHandSize) break;
            
            var cardData = Deck[0];
            Deck.RemoveAt(0);
            
            // カードオブジェクトを生成
            var cardObject = CreateCardObject(cardData);
            Hand.Value.Add(cardObject);
        }
        
        // ReactivePropertyを更新
        Hand.ForceNotify();
        ArrangeHand();
    }
    
    /// <summary>
    /// カードオブジェクトを生成
    /// </summary>
    protected virtual Card CreateCardObject(CardData cardData)
    {
        var cardObject = Instantiate(cardPrefab, handContainer);
        cardObject.Init(cardData);
        
        // カードクリックイベントを登録
        if (cardObject.TryGetComponent<Button>(out var button))
            button.onClick.AddListener(() => OnCardSelected(cardObject));
        
        return cardObject;
    }
    
    /// <summary>
    /// 手札を整列
    /// </summary>
    protected virtual void ArrangeHand()
    {
        // 子クラスで実装
    }
    
    /// <summary>
    /// カードが選択された時の処理
    /// </summary>
    protected virtual void OnCardSelected(Card card)
    {
        selectedCard.Value = card;
    }
    
    /// <summary>
    /// 選択したカードをプレイ
    /// </summary>
    public virtual void PlaySelectedCard()
    {
        if (!selectedCard.Value) return;
        
        var playedCard = selectedCard.Value;
        Hand.Value.Remove(playedCard);
        Hand.ForceNotify();
        
        selectedCard.Value = null;
        ArrangeHand();
    }
}