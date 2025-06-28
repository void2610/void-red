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
/// 手札を管理するクラス
/// カードの生成、アニメーション、配置を担当
/// </summary>
public class Hand : MonoBehaviour
{
    [Header("手札設定")]
    [SerializeField] private Transform handContainer; // 手札を配置するコンテナ
    [SerializeField] private Card cardPrefab; // カードのプレハブ
    [SerializeField] private Transform deckPosition; // デッキの位置（アニメーション開始位置）
    [SerializeField] private int maxHandSize = 3;
    
    [Header("配置設定")]
    [SerializeField] private float cardSpacing = 150f; // カード間の間隔
    [SerializeField] private float cardAngle = 5f; // カードの角度
    [SerializeField] private bool isInteractable = true; // カードが選択可能かどうか
    
    // 手札のカードリスト
    private readonly ReactiveProperty<List<Card>> _cards = new(new List<Card>());
    private readonly ReactiveProperty<Card> _selectedCard = new(null);
    
    // カードの元の位置を記憶
    private readonly Dictionary<Card, float> _originalYPositions = new();
    
    // イベント
    public event Action<Card> OnCardSelected;
    
    // プロパティ
    public ReadOnlyReactiveProperty<List<Card>> Cards => _cards;
    public ReadOnlyReactiveProperty<Card> SelectedCard => _selectedCard;
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
            if (card && card.TryGetComponent<Button>(out var button))
            {
                button.interactable = interactable;
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
        await AnimateCardFromDeck(cardObject);
        
        // 手札を再配置
        await ArrangeCardsAsync();
        
        // 選択中のカードがあれば再度ハイライト
        if (_selectedCard.Value)
        {
            HighlightCard(_selectedCard.Value, true);
        }
        
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
            AddCardAsync(cardData).Forget();
            await UniTask.Delay(250);
        }
    }
    
    /// <summary>
    /// カードを手札から削除
    /// </summary>
    public void RemoveCard(Card card)
    {
        RemoveCardAsync(card, false).Forget();
    }
    
    /// <summary>
    /// カードを手札から削除（崩壊演出）
    /// </summary>
    public void CollapseCard(Card card)
    {
        RemoveCardAsync(card, true).Forget();
    }
    
    /// <summary>
    /// カードを手札から削除（アニメーション付き）
    /// </summary>
    private async UniTask RemoveCardAsync(Card card, bool isCollapse)
    {
        if (!_cards.Value.Contains(card)) return;
        
        // カードを削除
        _cards.Value.Remove(card);
        if (_selectedCard.Value == card)
            _selectedCard.Value = null;
        
        // 元の位置情報も削除
        _originalYPositions.Remove(card);
        
        // カードのアニメーション
        var rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform)
        {
            if (isCollapse)
            {
                // 崩壊アニメーション：ランダムな方向に飛び散りながら回転
                var randomDirection = new Vector2(
                    UnityEngine.Random.Range(-200f, 200f),
                    UnityEngine.Random.Range(100f, 300f)
                );
                var currentPos = rectTransform.anchoredPosition;
                var targetPos = currentPos + randomDirection;
                
                var moveTask = LMotion.Create(currentPos, targetPos, 0.5f)
                    .WithEase(Ease.OutCubic)
                    .BindToAnchoredPosition(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                    
                var startRotation = rectTransform.rotation;
                var targetRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-360f, 360f));
                var rotateTask = LMotion.Create(startRotation, targetRotation, 0.5f)
                    .WithEase(Ease.OutCubic)
                    .BindToRotation(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                    
                var scaleTask = LMotion.Create(Vector3.one, Vector3.zero, 0.5f)
                    .WithEase(Ease.InCubic)
                    .BindToLocalScale(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                    
                await UniTask.WhenAll(moveTask, rotateTask, scaleTask);
            }
            else
            {
                // 通常の削除アニメーション：上に移動しながらスケール縮小
                var currentPos = rectTransform.anchoredPosition;
                var targetPos = new Vector2(currentPos.x, currentPos.y + 100f);
                
                var moveTask = LMotion.Create(currentPos, targetPos, 0.3f)
                    .WithEase(Ease.InCubic)
                    .BindToAnchoredPosition(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                    
                var scaleTask = LMotion.Create(Vector3.one, Vector3.one * 0.5f, 0.3f)
                    .WithEase(Ease.InCubic)
                    .BindToLocalScale(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                    
                await UniTask.WhenAll(moveTask, scaleTask);
            }
        }
        
        // カードオブジェクトを破棄
        Destroy(card.gameObject);
        
        // 残りのカードを再配置
        await ArrangeCardsAsync();
        
        // 選択中のカードがあれば再度ハイライト
        if (_selectedCard.Value)
        {
            HighlightCard(_selectedCard.Value, true);
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
        _originalYPositions.Clear();
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 手札をデッキに戻す（アニメーション付き）
    /// </summary>
    public async UniTask ReturnCardsToDeck()
    {
        var cards = new List<Card>(_cards.Value);
        if (cards.Count == 0) return;
        
        // 選択状態をクリア
        _selectedCard.Value = null;
        
        // 各カードをデッキ位置にアニメーション
        var animationTasks = new List<UniTask>();
        
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (!card) continue;
            
            var rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform && deckPosition)
            {
                // デッキ位置を計算
                var deckPos = handContainer.InverseTransformPoint(deckPosition.position);
                var targetPosition = new Vector2(deckPos.x, deckPos.y);
                
                // 少しずつ時間差をつけてアニメーション
                var delay = i * 0.1f;
                animationTasks.Add(ReturnCardToDeckAsync(card, targetPosition, delay));
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
        _originalYPositions.Clear();
        _cards.ForceNotify();
    }
    
    /// <summary>
    /// 単一カードをデッキに戻すアニメーション
    /// </summary>
    private async UniTask ReturnCardToDeckAsync(Card card, Vector2 targetPosition, float delay)
    {
        // 遅延
        if (delay > 0)
            await UniTask.Delay((int)(delay * 1000));
        
        var rectTransform = card.GetComponent<RectTransform>();
        if (!rectTransform) return;
        
        var currentPos = rectTransform.anchoredPosition;
        var currentScale = rectTransform.localScale;
        var currentRotation = rectTransform.rotation;
        
        // 移動、スケール、回転のアニメーション
        var moveTask = LMotion.Create(currentPos, targetPosition, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToAnchoredPosition(rectTransform)
            .AddTo(card.gameObject)
            .ToUniTask();
            
        var scaleTask = LMotion.Create(currentScale, Vector3.one * 0.1f, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToLocalScale(rectTransform)
            .AddTo(card.gameObject)
            .ToUniTask();
            
        var rotateTask = LMotion.Create(currentRotation, Quaternion.identity, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToRotation(rectTransform)
            .AddTo(card.gameObject)
            .ToUniTask();
        
        await UniTask.WhenAll(moveTask, scaleTask, rotateTask);
    }
    
    /// <summary>
    /// カードオブジェクトを生成
    /// </summary>
    private Card CreateCardObject(CardData cardData)
    {
        var cardObject = Instantiate(cardPrefab, handContainer);
        cardObject.Init(cardData);
        
        // カードクリックイベントを登録（インタラクト可能な場合のみ）
        if (cardObject.TryGetComponent<Button>(out var button))
        {
            button.interactable = isInteractable;
            if (isInteractable)
            {
                button.onClick.AddListener(() => SelectCard(cardObject));
            }
        }
        
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
        var previousCard = _selectedCard.Value;
        
        Debug.Log($"SetSelectedCard: new={card?.name}, previous={previousCard?.name}");
        
        // 前に選択されていたカードのハイライトを解除
        if (previousCard && previousCard != card)
        {
            Debug.Log($"Removing highlight from: {previousCard.name}");
            HighlightCard(previousCard, false);
        }
        
        _selectedCard.Value = card;
        
        // 新しいカードをハイライト
        if (card)
        {
            Debug.Log($"Adding highlight to: {card.name}");
            HighlightCard(card, true);
        }
        
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
            var rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform)
            {
                // 基本のY位置は0
                var targetPosition = new Vector2(startX + i * cardSpacing, 0);
                var targetRotation = Quaternion.Euler(0, 0, -(i - (cardCount - 1) * 0.5f) * cardAngle);
                
                // 元のY位置を記憶（新しいカードの場合のみ）
                if (!_originalYPositions.ContainsKey(card))
                {
                    _originalYPositions[card] = 0;
                }
                
                // 位置のアニメーション
                var moveTask = LMotion.Create(rectTransform.anchoredPosition, targetPosition, 0.3f)
                    .WithEase(Ease.OutCubic)
                    .BindToAnchoredPosition(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                
                // 回転のアニメーション
                var rotateTask = LMotion.Create(rectTransform.rotation, targetRotation, 0.3f)
                    .WithEase(Ease.OutCubic)
                    .BindToRotation(rectTransform)
                    .AddTo(card.gameObject)
                    .ToUniTask();
                
                animationTasks.Add(moveTask);
                animationTasks.Add(rotateTask);
            }
        }
        
        // 全てのアニメーションが完了するまで待つ
        await UniTask.WhenAll(animationTasks);
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
            // 元のY位置を取得（記憶されていない場合は0）
            var originalY = _originalYPositions.TryGetValue(card, out var position) ? position : 0f;
            
            // 選択時は少し上に移動、非選択時は元の位置に戻る
            var currentPos = rectTransform.anchoredPosition;
            var targetPos = new Vector2(currentPos.x, highlight ? originalY + 30f : originalY);
            
            // 位置のアニメーション
            LMotion.Create(currentPos, targetPos, 0.2f)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(rectTransform)
                .AddTo(card.gameObject);
        }
    }
}