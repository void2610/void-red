using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using System;

/// <summary>
/// 手札を管理するクラス
/// カードの生成、アニメーション、配置を担当
/// </summary>
public class Hand : MonoBehaviour
{
    [Header("手札設定")]
    [SerializeField] private Transform handContainer; // 手札を配置するコンテナ
    [SerializeField] private Card cardPrefab; // カードのプレハブ
    [SerializeField] private Transform deckPosition; // デッキの位置（アニメーション開始位置）
    [SerializeField] private int maxHandSize = 5;
    
    [Header("配置設定")]
    [SerializeField] private float cardSpacing = 150f; // カード間の間隔
    [SerializeField] private float cardAngle = 5f; // カードの角度
    
    // 手札のカードリスト
    private readonly ReactiveProperty<List<Card>> _cards = new(new List<Card>());
    private readonly ReactiveProperty<Card> _selectedCard = new(null);
    
    // イベント
    public event Action<Card> OnCardSelected;
    
    // プロパティ
    public ReadOnlyReactiveProperty<List<Card>> Cards => _cards;
    public ReadOnlyReactiveProperty<Card> SelectedCard => _selectedCard;
    public int Count => _cards.Value.Count;
    
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
        await AnimateCardFromDeck(cardObject);
        
        // 手札を再配置
        ArrangeCards();
        
        // ReactivePropertyを更新
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 複数のカードを順番に追加
    /// </summary>
    public async UniTask AddCardsAsync(List<CardData> cardDataList)
    {
        foreach (var cardData in cardDataList)
        {
            await AddCardAsync(cardData);
            await UniTask.Delay(100); // 少し間を置く
        }
    }
    
    /// <summary>
    /// カードを手札から削除
    /// </summary>
    public void RemoveCard(Card card)
    {
        if (_cards.Value.Remove(card))
        {
            if (_selectedCard.Value == card)
                _selectedCard.Value = null;
                
            ArrangeCards();
            _cards.ForceNotify();
        }
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
    /// カードオブジェクトを生成
    /// </summary>
    private Card CreateCardObject(CardData cardData)
    {
        var cardObject = Instantiate(cardPrefab, handContainer);
        cardObject.Init(cardData);
        
        // カードクリックイベントを登録
        if (cardObject.TryGetComponent<Button>(out var button))
            button.onClick.AddListener(() => SelectCard(cardObject));
        
        return cardObject;
    }
    
    /// <summary>
    /// カードを選択（内部用）
    /// </summary>
    private void SelectCard(Card card)
    {
        SetSelectedCard(card);
    }
    
    /// <summary>
    /// カードを選択（外部用）
    /// </summary>
    public void SetSelectedCard(Card card)
    {
        _selectedCard.Value = card;
        OnCardSelected?.Invoke(card);
    }
    
    /// <summary>
    /// デッキからカードがドローされるアニメーション
    /// </summary>
    private async UniTask AnimateCardFromDeck(Card card)
    {
        var rectTransform = card.GetComponent<RectTransform>();
        
        // デッキ位置が設定されていない場合はスキップ
        if (!deckPosition) return;
        
        // 初期位置をデッキ位置に設定
        var targetPosition = rectTransform.anchoredPosition;
        var deckPos = handContainer.InverseTransformPoint(deckPosition.position);
        rectTransform.anchoredPosition = new Vector2(deckPos.x, deckPos.y);
        
        // 初期スケールを小さく設定
        rectTransform.localScale = Vector3.one * 0.1f;
        
        // 移動とスケールのアニメーション
        var moveTask = LMotion.Create(rectTransform.anchoredPosition, targetPosition, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToAnchoredPosition(rectTransform)
            .AddTo(card.gameObject)
            .ToUniTask();
            
        var scaleTask = LMotion.Create(Vector3.one * 0.1f, Vector3.one, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToLocalScale(rectTransform)
            .AddTo(card.gameObject)
            .ToUniTask();
        
        await UniTask.WhenAll(moveTask, scaleTask);
    }
    
    /// <summary>
    /// 手札を扇状に配置
    /// </summary>
    private void ArrangeCards()
    {
        var cardCount = _cards.Value.Count;
        if (cardCount == 0) return;
        
        // 中心から左右に配置
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        
        for (int i = 0; i < cardCount; i++)
        {
            var card = _cards.Value[i];
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
    /// カードのハイライト表示
    /// </summary>
    public void HighlightCard(Card card, bool highlight)
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