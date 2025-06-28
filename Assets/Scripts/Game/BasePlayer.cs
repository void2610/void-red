using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤーとNPCの基底クラス
/// カードデッキを持ち、カード選択の基本機能を提供
/// </summary>
public abstract class BasePlayer : MonoBehaviour
{
    [SerializeField] protected int maxMentalPower = 20;
    [SerializeField] protected int maxHandSize = 5;
    [SerializeField] protected Hand hand; // 手札管理クラス
    
    // デッキと精神力
    private readonly List<CardData> _deck = new ();
    private readonly ReactiveProperty<int> _mentalPower = new ();
    
    public ReadOnlyReactiveProperty<Card> SelectedCard => hand.SelectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    public int MaxMentalPower => maxMentalPower;
    
    protected virtual void Awake()
    {
        // 精神力を最大値で初期化
        _mentalPower.Value = maxMentalPower;
        
        // 手札のカード選択イベントを購読
        hand.OnCardSelected += OnCardSelected;
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
        DrawCardAsync(count).Forget();
    }
    
    /// <summary>
    /// カードを引く（アニメーション付き）
    /// </summary>
    protected virtual async UniTask DrawCardAsync(int count = 1)
    {
        var cardDataList = new List<CardData>();
        
        for (var i = 0; i < count; i++)
        {
            if (_deck.Count == 0 || hand.Count >= maxHandSize) break;
            
            var cardData = _deck[0];
            _deck.RemoveAt(0);
            cardDataList.Add(cardData);
        }
        
        // 手札クラスにカードを追加（アニメーション付き）
        await hand.AddCardsAsync(cardDataList);
    }
    
    /// <summary>
    /// 手札を整列（Handクラスに委譲）
    /// </summary>
    protected virtual void ArrangeHand()
    {
        // Handクラスで自動的に処理される
    }
    
    /// <summary>
    /// カードが選択された時の処理
    /// </summary>
    protected virtual void OnCardSelected(Card card)
    {
        // Handクラスで既に選択状態は管理されているため、
        // 子クラスで必要な追加処理があれば実装
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
        PlaySelectedCard(false);
    }
    
    /// <summary>
    /// 選択したカードをプレイ
    /// </summary>
    /// <param name="shouldCollapse">カードが崩壊するかどうか</param>
    public virtual void PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = hand.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 手札からカードを削除
        hand.RemoveCard(selectedCard);
        
        // 崩壊しない場合はデッキに戻す
        if (!shouldCollapse)
        {
            ReturnCardToDeck(selectedCard.CardData);
        }
    }
    
    /// <summary>
    /// カードをデッキに戻す
    /// </summary>
    /// <param name="cardData">戻すカードデータ</param>
    protected virtual void ReturnCardToDeck(CardData cardData)
    {
        _deck.Add(cardData);
        ShuffleDeck();
    }
    
    /// <summary>
    /// 選択したカードを崩壊させる
    /// </summary>
    public virtual void CollapseSelectedCard()
    {
        var selectedCard = hand.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 崩壊演出で手札からカードを削除
        hand.CollapseCard(selectedCard);
    }
    
    /// <summary>
    /// 手札のインタラクト可能状態を設定
    /// </summary>
    public virtual void SetHandInteractable(bool interactable)
    {
        hand.SetInteractable(interactable);
    }
}