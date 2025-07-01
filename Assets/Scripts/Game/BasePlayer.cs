using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// プレイヤーとNPCの基底クラス
/// カードデッキを持ち、カード選択の基本機能を提供
/// 簡略化されたMVPパターンでHandViewを直接使用
/// </summary>
public abstract class BasePlayer
{
    public ReadOnlyReactiveProperty<CardView> SelectedCard => _handView?.SelectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    public static int MaxMentalPower => MAX_MENTAL_POWER;
    
    private const int MAX_MENTAL_POWER = 20;
    private const int MAX_HAND_SIZE = 3;
    
    private readonly ReactiveProperty<int> _mentalPower = new ();
    private DeckModel _deckModel;
    private readonly HandView _handView;
    
    public void DrawCard(int count = 1) => DrawCardAsync(count).Forget();
    public void SetHandInteractable(bool interactable) => _handView.SetInteractable(interactable);
    public void RestoreMentalPower(int amount) => _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, MAX_MENTAL_POWER);

    public BasePlayer(HandView handView)
    {
        _handView = handView;
        _mentalPower.Value = MAX_MENTAL_POWER;
    }
        
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deckModel = new DeckModel(cardDataList);
    }
    
    public CardData GetRandomCardDataFromHand()
    {
        // 手札からランダムにカードを選択
        var randomIndex = Random.Range(0, _handView.Count);
        return _handView.Cards.CurrentValue[randomIndex].CardData;
    }
    
    /// <summary>
    /// 精神力を消費する
    /// </summary>
    /// <param name="amount">消費する精神力</param>
    public void ConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return;
        
        _mentalPower.Value -= amount;
    }
    
    /// <summary>
    /// 選択したカードをプレイ
    /// </summary>
    /// <param name="shouldCollapse">カードが崩壊するかどうか</param>
    public void PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = _handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 手札からカードを削除
        if (shouldCollapse)
        {
            _handView.CollapseCard(selectedCard);
        }
        else
        {
            _handView.RemoveCard(selectedCard);
            // 崩壊しない場合はデッキに戻す
            _deckModel.ReturnCard(selectedCard.CardData);
        }
    }
    
    /// <summary>
    /// 選択したカードを崩壊させる
    /// </summary>
    public void CollapseSelectedCard()
    {
        var selectedCard = _handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 崩壊演出で手札からカードを削除
        _handView.CollapseCard(selectedCard);
    }
    
    /// <summary>
    /// 手札をデッキに戻す
    /// </summary>
    public async UniTask ReturnHandToDeck()
    {
        // 現在の手札のカードデータを取得
        var cardDataList = _handView.Cards.CurrentValue.Select(cardView => cardView.CardData).ToList();

        // 手札をデッキに戻すアニメーション
        await _handView.ReturnCardsToDeck();
        
        // カードデータをデッキに追加
        _deckModel.ReturnCards(cardDataList);
    }
    
    /// <summary>
    /// カードを引く（アニメーション付き）
    /// </summary>
    private async UniTask DrawCardAsync(int count = 1)
    {
        var cardDataList = new List<CardData>();
        
        for (var i = 0; i < count; i++)
        {
            if (_deckModel.IsEmpty || _handView.Count >= MAX_HAND_SIZE) break;
            
            var cardData = _deckModel.DrawCard();
            if (cardData) cardDataList.Add(cardData);
        }
        
        await _handView.AddCardsAsync(cardDataList);
    }

}