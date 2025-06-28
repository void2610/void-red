using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using System;
using Void2610.UnityTemplate;

/// <summary>
/// 手札を管理するViewクラス
/// 元のHand.csをベースにCardViewを使用するように調整した簡略化されたMVPパターン
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("手札設定")]
    [SerializeField] private Transform handContainer; // 手札を配置するコンテナ
    [SerializeField] private CardView cardPrefab; // カードのプレハブ
    [SerializeField] private Transform deckPosition; // デッキの位置（アニメーション開始位置）
    [SerializeField] private int maxHandSize = 3;
    
    [Header("配置設定")]
    [SerializeField] private float cardSpacing = 150f; // カード間の間隔
    [SerializeField] private float cardAngle = 5f; // カードの角度
    [SerializeField] private bool isInteractable = true; // カードが選択可能かどうか
    
    // 手札のカードリスト
    private readonly ReactiveProperty<List<CardView>> _cards = new(new List<CardView>());
    private readonly ReactiveProperty<CardView> _selectedCard = new(null);
    
    // イベント
    private readonly Subject<CardView> _onCardSelected = new();
    public Observable<CardView> OnCardSelected => _onCardSelected;
    
    // プロパティ
    public ReadOnlyReactiveProperty<List<CardView>> Cards => _cards;
    public ReadOnlyReactiveProperty<CardView> SelectedCard => _selectedCard;
    public int Count => _cards.Value.Count;
    
    /// <summary>
    /// カードのインタラクト可能状態を設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        
        // 既存のカードにも適用
        foreach (var card in _cards.Value)
        {
            if (card)
            {
                card.SetInteractable(interactable);
            }
        }
    }
    
    /// <summary>
    /// カードを手札に追加（アニメーション付き）
    /// </summary>
    public async UniTask AddCardAsync(CardData cardData)
    {
        if (_cards.Value.Count >= maxHandSize) return;
        
        // カードオブジェクトを生成
        var cardObject = CreateCardObject(cardData);
        _cards.Value.Add(cardObject);
        
        // アニメーション実行
        SeManager.Instance.PlaySe("Card");
        var deckPos = handContainer.InverseTransformPoint(deckPosition.position);
        await cardObject.PlayDrawAnimation(new Vector2(deckPos.x, deckPos.y));
        
        // 手札を再配置
        await ArrangeCardsAsync();
        
        // 選択中のカードがあれば再度ハイライト
        if (_selectedCard.Value) _selectedCard.Value.SetHighlight(true);
        
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 複数のカードを順番に追加
    /// </summary>
    public async UniTask AddCardsAsync(List<CardData> cardDataList)
    {
        foreach (var cardData in cardDataList)
        {
            AddCardAsync(cardData).Forget();
            await UniTask.Delay(250);
        }
    }
    
    public void RemoveCard(CardView card) => RemoveCardAsync(card, false).Forget();
    public void CollapseCard(CardView card) => RemoveCardAsync(card, true).Forget();
    
    /// <summary>
    /// カードを手札から削除（アニメーション付き）
    /// </summary>
    private async UniTask RemoveCardAsync(CardView card, bool isCollapse)
    {
        if (!_cards.Value.Contains(card)) return;
        
        // カードを削除
        _cards.Value.Remove(card);
        if (_selectedCard.Value == card)
            _selectedCard.Value = null;
        
        // 削除アニメーション
        await card.PlayRemoveAnimation(isCollapse);
        
        // カードオブジェクトを破棄
        Destroy(card.gameObject);
        
        // 残りのカードを再配置
        await ArrangeCardsAsync();
        
        // 選択中のカードがあれば再度ハイライト
        if (_selectedCard.Value)
        {
            _selectedCard.Value.SetHighlight(true);
        }
        
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 手札をクリア
    /// </summary>
    public void Clear()
    {
        foreach (var card in _cards.Value)
        {
            if (card) Destroy(card.gameObject);
        }
        _cards.Value.Clear();
        _selectedCard.Value = null;
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 手札をデッキに戻す（アニメーション付き）
    /// </summary>
    public async UniTask ReturnCardsToDeck()
    {
        var cards = new List<CardView>(_cards.Value);
        if (cards.Count == 0) return;
        
        // 選択状態をクリア
        _selectedCard.Value = null;
        
        // 各カードをデッキ位置にアニメーション
        var animationTasks = new List<UniTask>();
        
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (!card) continue;
            
            if (deckPosition)
            {
                // デッキ位置を計算
                var deckPos = handContainer.InverseTransformPoint(deckPosition.position);
                var targetPosition = new Vector2(deckPos.x, deckPos.y);
                
                // 少しずつ時間差をつけてアニメーション
                var delay = i * 0.1f;
                animationTasks.Add(DelayedReturnToDeck(card, targetPosition, delay));
            }
        }
        
        // 全てのアニメーション完了まで待機
        await UniTask.WhenAll(animationTasks);
        
        // カードオブジェクトを削除
        foreach (var card in cards)
        {
            if (card) Destroy(card.gameObject);
        }
        
        // 手札データをクリア
        _cards.Value.Clear();
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 遅延付きでカードをデッキに戻す
    /// </summary>
    private async UniTask DelayedReturnToDeck(CardView card, Vector2 deckPos, float delay)
    {
        if (delay > 0)
            await UniTask.Delay((int)(delay * 1000));
        
        await card.PlayReturnToDeckAnimation(deckPos);
    }
    
    /// <summary>
    /// カードオブジェクトを生成
    /// </summary>
    private CardView CreateCardObject(CardData cardData)
    {
        var cardObject = Instantiate(cardPrefab, handContainer);
        cardObject.Initialize(cardData);
        
        // カードクリックイベントを登録
        cardObject.OnClicked.Subscribe(SelectCard).AddTo(cardObject);
        cardObject.SetInteractable(isInteractable);
        
        return cardObject;
    }
    
    /// <summary>
    /// カードを選択（内部用）
    /// </summary>
    private void SelectCard(CardView card)
    {
        SetSelectedCard(card);
    }
    
    /// <summary>
    /// カードを選択（外部用）
    /// </summary>
    public void SetSelectedCard(CardView card)
    {
        var previousCard = _selectedCard.Value;
        
        // 前に選択されていたカードのハイライトを解除
        if (previousCard && previousCard != card)
        {
            previousCard.SetHighlight(false);
        }
        
        _selectedCard.Value = card;
        
        // 新しいカードをハイライト
        if (card)
        {
            card.SetHighlight(true);
        }
        
        _onCardSelected.OnNext(card);
    }
    
    
    /// <summary>
    /// 手札を扇状に配置
    /// </summary>
    private async UniTask ArrangeCardsAsync()
    {
        var cardCount = _cards.Value.Count;
        if (cardCount == 0) return;
        
        // 中心から左右に配置
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        var animationTasks = new List<UniTask>();
        
        for (int i = 0; i < cardCount; i++)
        {
            var card = _cards.Value[i];
            if (!card) continue;
            
            // 位置を設定
            var targetPosition = new Vector2(startX + i * cardSpacing, 0);
            var targetRotation = Quaternion.Euler(0, 0, -(i - (cardCount - 1) * 0.5f) * cardAngle);
            
            // カード位置を更新してアニメーション
            card.UpdateOriginalPosition(targetPosition);
            animationTasks.Add(card.PlayArrangeAnimation(targetPosition, targetRotation));
        }
        
        // 全てのアニメーションが完了するまで待つ
        await UniTask.WhenAll(animationTasks);
    }
    
    
    private void OnDestroy()
    {
        _onCardSelected?.Dispose();
        _cards?.Dispose();
        _selectedCard?.Dispose();
    }
}