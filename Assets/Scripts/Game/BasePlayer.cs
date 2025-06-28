using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤーとNPCの基底クラス
/// カードデッキを持ち、カード選択の基本機能を提供
/// 簡略化されたMVPパターンでHandViewを直接使用
/// </summary>
public abstract class BasePlayer : MonoBehaviour
{
    [SerializeField] protected int maxMentalPower = 20;
    [SerializeField] protected int maxHandSize = 3;
    [SerializeField] protected HandView handView;
    
    // デッキと精神力
    private readonly List<CardData> _deck = new ();
    private readonly ReactiveProperty<int> _mentalPower = new ();
    
    public ReadOnlyReactiveProperty<CardView> SelectedCard => handView?.SelectedCard;
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
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deck.Clear();
        _deck.AddRange(cardDataList);
        ShuffleDeck();
    }
    
    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    protected void ShuffleDeck()
    {
        for (var i = 0; i < _deck.Count; i++)
        {
            var temp = _deck[i];
            var randomIndex = Random.Range(i, _deck.Count);
            _deck[i] = _deck[randomIndex];
            _deck[randomIndex] = temp;
        }
    }
    
    public void DrawCard(int count = 1) => DrawCardAsync(count).Forget();
    
    /// <summary>
    /// カードを引く（アニメーション付き）
    /// </summary>
    private async UniTask DrawCardAsync(int count = 1)
    {
        var cardDataList = new List<CardData>();
        
        for (var i = 0; i < count; i++)
        {
            if (_deck.Count == 0 || handView.Count >= maxHandSize) break;
            
            var cardData = _deck[0];
            _deck.RemoveAt(0);
            cardDataList.Add(cardData);
        }
        
        await handView.AddCardsAsync(cardDataList);
    }
    
    /// <summary>
    /// 精神力を消費する
    /// </summary>
    /// <param name="amount">消費する精神力</param>
    /// <returns>消費に成功したかどうか</returns>
    public bool ConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return false;
        
        _mentalPower.Value -= amount;
        return true;
    }
    
    /// <summary>
    /// 精神力を回復する
    /// </summary>
    /// <param name="amount">回復する精神力</param>
    public void RestoreMentalPower(int amount)
    {
        _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, maxMentalPower);
    }
    
    /// <summary>
    /// 選択したカードをプレイ
    /// </summary>
    /// <param name="shouldCollapse">カードが崩壊するかどうか</param>
    public void PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 手札からカードを削除
        if (shouldCollapse)
        {
            handView.CollapseCard(selectedCard);
        }
        else
        {
            handView.RemoveCard(selectedCard);
            // 崩壊しない場合はデッキに戻す
            ReturnCardToDeck(selectedCard.CardData);
        }
    }
    
    /// <summary>
    /// カードをデッキに戻す
    /// </summary>
    /// <param name="cardData">戻すカードデータ</param>
    protected void ReturnCardToDeck(CardData cardData)
    {
        _deck.Add(cardData);
        ShuffleDeck();
    }
    
    /// <summary>
    /// 選択したカードを崩壊させる
    /// </summary>
    public void CollapseSelectedCard()
    {
        var selectedCard = handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 崩壊演出で手札からカードを削除
        handView.CollapseCard(selectedCard);
    }
    
    /// <summary>
    /// 手札をデッキに戻す
    /// </summary>
    public async UniTask ReturnHandToDeck()
    {
        // 現在の手札のカードデータを取得
        var cardDataList = new List<CardData>();
        
        foreach (var cardView in handView.Cards.CurrentValue)
        {
            if (cardView?.CardData)
            {
                cardDataList.Add(cardView.CardData);
            }
        }
        
        // 手札をデッキに戻すアニメーション
        await handView.ReturnCardsToDeck();
        
        // カードデータをデッキに追加
        foreach (var cardData in cardDataList)
        {
            _deck.Add(cardData);
        }
        
        // デッキをシャッフル
        ShuffleDeck();
    }
    
    /// <summary>
    /// 手札のインタラクト可能状態を設定
    /// </summary>
    public void SetHandInteractable(bool interactable)
    {
        handView.SetInteractable(interactable);
    }
}