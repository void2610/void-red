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
    [SerializeField] protected int maxMentalPower = 20;
    [SerializeField] protected int maxHandSize = 5;
    [SerializeField] protected Transform handContainer; // 手札を配置するコンテナ
    [SerializeField] protected Card cardPrefab; // カードのプレハブ
    
    // デッキと手札
    private readonly List<CardData> _deck = new ();
    protected readonly ReactiveProperty<List<Card>> Hand = new (new List<Card>());
    protected readonly ReactiveProperty<Card> selectedCard = new (null);
    private readonly ReactiveProperty<int> _mentalPower = new ();
    
    public ReadOnlyReactiveProperty<Card> SelectedCard => selectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    public int MaxMentalPower => maxMentalPower;
    
    protected virtual void Awake()
    {
        // 精神力を最大値で初期化
        _mentalPower.Value = maxMentalPower;
    }
    
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public virtual void InitializeDeck(List<CardData> cardDataList)
    {
        _deck.Clear();
        _deck.AddRange(cardDataList);
        ShuffleDeck();
    }
    
    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    protected virtual void ShuffleDeck()
    {
        for (var i = 0; i < _deck.Count; i++)
        {
            var temp = _deck[i];
            var randomIndex = Random.Range(i, _deck.Count);
            _deck[i] = _deck[randomIndex];
            _deck[randomIndex] = temp;
        }
    }
    
    /// <summary>
    /// カードを引く
    /// </summary>
    public virtual void DrawCard(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            if (_deck.Count == 0 || Hand.Value.Count >= maxHandSize) break;
            
            var cardData = _deck[0];
            _deck.RemoveAt(0);
            
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
    /// 精神力を消費する
    /// </summary>
    /// <param name="amount">消費する精神力</param>
    /// <returns>消費に成功したかどうか</returns>
    public virtual bool ConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return false;
        
        _mentalPower.Value -= amount;
        return true;
    }
    
    /// <summary>
    /// 精神力を回復する
    /// </summary>
    /// <param name="amount">回復する精神力</param>
    public virtual void RestoreMentalPower(int amount)
    {
        _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, maxMentalPower);
    }
    
    /// <summary>
    /// 指定した精神ベットが可能かチェック
    /// </summary>
    /// <param name="betAmount">精神ベット値</param>
    /// <returns>ベット可能かどうか</returns>
    public virtual bool CanMentalBet(int betAmount)
    {
        return _mentalPower.Value >= betAmount;
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