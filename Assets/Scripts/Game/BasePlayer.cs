using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// プレイヤーとNPCの基底クラス（Presenterとして機能）
/// HandModelとHandViewを仲介し、ゲームロジックを制御
/// </summary>
public abstract class BasePlayer : IDisposable
{
    // 公開プロパティ
    public ReadOnlyReactiveProperty<CardData> SelectedCard => _handModel.SelectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    public static int MaxMentalPower => MAX_MENTAL_POWER;
    public int HandCount => _handModel.Count;
    
    // 定数
    private const int MAX_MENTAL_POWER = 20;
    
    // プライベートフィールド
    private readonly ReactiveProperty<int> _mentalPower = new();
    private readonly HandModel _handModel;
    private readonly HandView _handView;
    private readonly CompositeDisposable _disposables = new();
    private DeckModel _deckModel;
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="handView">対応するHandView</param>
    protected BasePlayer(HandView handView)
    {
        _handView = handView;
        _handModel = new HandModel(3); // 最大手札数3
        _mentalPower.Value = MAX_MENTAL_POWER;
        
        // HandViewとHandModelをバインド
        _handView.BindModel(_handModel);
        
        // Viewのイベントを購読
        _handView.OnCardClicked
            .Subscribe(_handModel.SelectCardAt)
            .AddTo(_disposables);
    }
    
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deckModel = new DeckModel(cardDataList);
    }
    
    public void DrawCard(int c = 1) => DrawCardAsync(c).Forget();
    public CardData GetRandomCardDataFromHand() => _handModel.GetRandomCard();
    public void SelectCard(CardData cardData) => _handModel.SelectCard(cardData);
    public void SetHandInteractable(bool interactable) => _handView.SetInteractable(interactable);
    public void SelectCardAt(int index) => _handModel.SelectCardAt(index);
    public void DeselectCard() => _handModel.DeselectCard();
    
    /// <summary>
    /// カードを引く(待機可能)
    /// </summary>
    public async UniTask DrawCardAsync(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            if (_deckModel.IsEmpty || _handModel.Count >= _handModel.MaxHandSize) 
                break;
            
            var cardData = _deckModel.DrawCard();
            if (cardData)
            {
                // TODO: 手札に追加できなかった時にデッキに戻す処理が必要？
                _handModel.TryAddCard(cardData);
                if (i < count - 1) await UniTask.Delay(100);
            }
        }
        
        // 全てのカード追加後、アニメーション完了まで待機
        if (count > 0) await UniTask.Delay(600);
    }
    
    public void ConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return;
        _mentalPower.Value -= amount;
    }
    
    public void RestoreMentalPower(int amount)
    {
        _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, MAX_MENTAL_POWER);
    }
    
    public void PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = _handModel.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 手札から削除
        _handModel.TryRemoveCard(selectedCard);
        
        if (!shouldCollapse && _deckModel != null)
        {
            // 崩壊しない場合はデッキに戻す
            _deckModel.ReturnCard(selectedCard);
        }
    }
    
    /// <summary>
    /// 選択したカードを崩壊させる
    /// </summary>
    public void CollapseSelectedCard()
    {
        var selectedCard = _handModel.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 選択されたカードのインデックスを取得
        var selectedIndex = _handModel.SelectedIndex.CurrentValue;
        
        // 手札から削除
        _handModel.TryRemoveCard(selectedCard);
        
        // 崩壊アニメーションを再生
        if (selectedIndex >= 0)
        {
            _handView.PlayCollapseAnimation(selectedIndex).Forget();
        }
    }
    
    public async UniTask ReturnHandToDeck()
    {
        // 手札のカードを取得（データのみ、表示は残す）
        var handCards = _handModel.GetAllCards();
        
        // デッキに戻すアニメーション
        await _handView.ReturnCardsToDeck();
        
        // 手札データをクリア
        _handModel.Clear();
        
        // カードデータをデッキに追加
        _deckModel.ReturnCards(handCards);
    }
    
    /// <summary>
    /// リソースの解放
    /// </summary>
    public virtual void Dispose()
    {
        _disposables?.Dispose();
        _mentalPower?.Dispose();
    }
}