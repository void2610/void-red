using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using Void2610.UnityTemplate;

/// <summary>
/// 手札の表示のみを担当するViewクラス
/// HandModelとバインドしてリアクティブに表示を更新
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("手札設定")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform deckPosition;
    
    [Header("配置設定")]
    [SerializeField] private float cardSpacing = 100f;
    [SerializeField] private float cardAngle = 5f;
    
    // イベント
    public Observable<int> OnCardClicked => _onCardClicked;
    
    private readonly Subject<int> _onCardClicked = new();
    private readonly List<CardView> _cardViews = new();
    private readonly CompositeDisposable _modelBindings = new();
    private CardViewFactory _cardViewFactory;
    private bool _isInteractable = true;
    
    /// <summary>
    /// HandModelをバインド
    /// </summary>
    public void BindModel(HandModel model)
    {
        // 既存のバインディングをクリア
        _modelBindings.Clear();
        
        // カードリストの変更を監視
        model.Cards
            .Subscribe(cards => UpdateCardViews(cards).Forget())
            .AddTo(_modelBindings);
            
        // 選択状態の変更を監視
        model.SelectedIndex
            .Subscribe(UpdateSelection)
            .AddTo(_modelBindings);
    }
    
    /// <summary>
    /// インタラクト可能状態を設定 (敵手札用)
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        
        // 既存のカードにも適用
        foreach (var cardView in _cardViews)
        {
            cardView?.SetInteractable(interactable);
        }
    }
    
    /// <summary>
    /// カード表示を更新（差分更新）
    /// </summary>
    private async UniTask UpdateCardViews(List<CardData> cards)
    {
        // 削除処理：余分なCardViewを削除
        while (_cardViews.Count > cards.Count)
        {
            var lastIndex = _cardViews.Count - 1;
            var cardView = _cardViews[lastIndex];
            _cardViews.RemoveAt(lastIndex);
            
            if (cardView)
            {
                await RemoveCardViewAsync(cardView);
            }
        }
        
        var newCardStartIndex = _cardViews.Count;
        
        // 追加処理：新しいカードを作成（アニメーションなし）
        for (var i = _cardViews.Count; i < cards.Count; i++)
        {
            var cardView = CreateCardView(cards[i]);
            _cardViews.Add(cardView);
        }
        
        // 既存のCardViewのデータを更新
        for (var i = 0; i < newCardStartIndex && i < cards.Count; i++)
        {
            if (_cardViews[i] && _cardViews[i].CardData != cards[i])
            {
                _cardViews[i].Initialize(cards[i]);
            }
        }
        
        // 全カードの最終位置を計算
        CalculateCardPositions();
        
        // 新しいカードのドローアニメーション + 既存カードのアレンジアニメーション
        if (newCardStartIndex < cards.Count)
        {
            await PlayDrawAndArrangeAnimation(newCardStartIndex);
        }
        else
        {
            // 既存カードのみの場合はアレンジのみ
            await ArrangeExistingCardsAsync();
        }
    }
    
    /// <summary>
    /// CardViewを作成（アニメーションなし）
    /// </summary>
    private CardView CreateCardView(CardData cardData)
    {
        var cardView = _cardViewFactory.CreateHandCard(
            cardData, 
            handContainer, 
            clickedCardView => {
                var currentIndex = _cardViews.IndexOf(clickedCardView);
                if (currentIndex >= 0)
                    _onCardClicked.OnNext(currentIndex);
            },
            gameObject,
            _isInteractable
        );
        
        return cardView;
    }
    
    /// <summary>
    /// 全カードの最終位置を計算
    /// </summary>
    private void CalculateCardPositions()
    {
        var cardCount = _cardViews.Count;
        if (cardCount == 0) return;
        
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        
        for (int i = 0; i < cardCount; i++)
        {
            var cardView = _cardViews[i];
            if (!cardView) continue;
            
            var targetPosition = new Vector2(startX + i * cardSpacing, 0);
            cardView.UpdateOriginalPosition(targetPosition);
        }
    }
    
    /// <summary>
    /// 新しいカードのドローと既存カードのアレンジを同時実行
    /// </summary>
    private async UniTask PlayDrawAndArrangeAnimation(int newCardStartIndex)
    {
        var animationTasks = new List<UniTask>();
        
        for (var i = newCardStartIndex; i < _cardViews.Count; i++)
        {
            var cardView = _cardViews[i];
            if (cardView)
            {
                var delay = (i - newCardStartIndex) * 150; // 150ms間隔で順次ドロー
                animationTasks.Add(DelayedDrawAnimation(cardView, delay));
            }
        }
        
        // 既存カードのアレンジアニメーション（少し遅れて開始）
        if (newCardStartIndex > 0)
        {
            animationTasks.Add(DelayedArrangeExistingCards(200)); // 200ms後にアレンジ開始
        }
        
        await UniTask.WhenAll(animationTasks);
    }
    
    /// <summary>
    /// 遅延付きドローアニメーション
    /// </summary>
    private async UniTask DelayedDrawAnimation(CardView cardView, int delayMs)
    {
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await PlayDrawAnimation(cardView);
    }
    
    /// <summary>
    /// 遅延付き既存カードアレンジ
    /// </summary>
    private async UniTask DelayedArrangeExistingCards(int delayMs)
    {
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await ArrangeExistingCardsAsync();
    }
    
    /// <summary>
    /// 既存カードのアレンジアニメーション
    /// </summary>
    private async UniTask ArrangeExistingCardsAsync()
    {
        var cardCount = _cardViews.Count;
        
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        var animationTasks = new List<UniTask>();
        
        for (var i = 0; i < cardCount; i++)
        {
            var cardView = _cardViews[i];
            if (!cardView) continue;
            
            var targetPosition = new Vector2(startX + i * cardSpacing, 0);
            var targetRotation = Quaternion.Euler(0, 0, -(i - (cardCount - 1) * 0.5f) * cardAngle);
            
            cardView.UpdateOriginalPosition(targetPosition);
            animationTasks.Add(cardView.PlayArrangeAnimation(targetPosition, targetRotation));
        }
        
        await UniTask.WhenAll(animationTasks);
    }
    
    /// <summary>
    /// CardViewを削除
    /// </summary>
    private async UniTask RemoveCardViewAsync(CardView cardView)
    {
        cardView.PlayRemoveAndDestroy(false);
        // アニメーション完了まで待機（通常削除は0.3秒）
        await UniTask.Delay(300);
    }
    
    /// <summary>
    /// 選択状態を更新
    /// </summary>
    private void UpdateSelection(int selectedIndex)
    {
        for (var i = 0; i < _cardViews.Count; i++)
        {
            _cardViews[i]?.SetHighlight(i == selectedIndex);
        }
    }
    
    /// <summary>
    /// ドローアニメーション
    /// </summary>
    private async UniTask PlayDrawAnimation(CardView cardView)
    {
        SeManager.Instance.PlaySe("Card");
        
        // デッキ位置をhandContainer基準の座標に変換
        var deckLocalPos = GetDeckLocalPosition();
        
        await cardView.PlayDrawAnimation(deckLocalPos);
    }
    
    
    /// <summary>
    /// 手札をデッキに戻す（アニメーション付き）
    /// </summary>
    public async UniTask ReturnCardsToDeck()
    {
        if (_cardViews.Count == 0 || !deckPosition) return;
        
        // デッキ位置をhandContainer基準の座標に変換
        var deckLocalPos = GetDeckLocalPosition();
        
        // カード数を保存（待機時間計算用）
        var cardCount = _cardViews.Count;
        
        // 各カードにデッキ戻りアニメーションを指示（自己削除付き）
        for (var i = 0; i < _cardViews.Count; i++)
        {
            var cardView = _cardViews[i];
            if (cardView)
            {
                var delay = i * 0.1f; // 0.1秒間隔で順次開始
                cardView.PlayReturnToDeckAndDestroy(deckLocalPos, delay);
            }
        }
        
        // リストをクリア（CardViewは自己削除するので参照のみクリア）
        _cardViews.Clear();
        
        // 最後のカードのアニメーション完了まで待機
        var totalAnimationTime = (cardCount - 1) * 0.1f + 0.4f; // 最後のカードの遅延 + アニメーション時間
        await UniTask.Delay((int)(totalAnimationTime * 1000));
    }
    
    /// <summary>
    /// 崩壊アニメーション付きでカードを削除
    /// </summary>
    public async UniTask PlayCollapseAnimation(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;
        
        var cardView = _cardViews[cardIndex];
        if (!cardView) return;
        
        // リストから削除
        _cardViews.RemoveAt(cardIndex);
        
        // 崩壊アニメーションを開始（自己削除付き）
        cardView.PlayRemoveAndDestroy(true);
        
        // アニメーション完了まで待機（崩壊は0.5秒）
        await UniTask.Delay(500);
    }
    
    /// <summary>
    /// デッキ位置をhandContainer基準のローカル座標に変換
    /// </summary>
    private Vector2 GetDeckLocalPosition()
    {
        var deckWorldPos = deckPosition.position;
        var handContainerRect = handContainer as RectTransform;
        
        Vector2 deckLocalPos;
        if (handContainerRect)
        {
            // ワールド座標をhandContainerのローカル座標に変換
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handContainerRect, 
                RectTransformUtility.WorldToScreenPoint(null, deckWorldPos), 
                null, 
                out deckLocalPos
            );
        }
        else
        {
            // フォールバック：従来の方法
            deckLocalPos = handContainer.InverseTransformPoint(deckWorldPos);
        }
        
        return deckLocalPos;
    }
    
    private void Awake()
    {
        _cardViewFactory = new CardViewFactory(cardPrefab);
    }
    
    private void OnDestroy()
    {
        _onCardClicked?.Dispose();
        _modelBindings?.Dispose();
    }
}