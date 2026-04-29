using System.Collections.Generic;
using Auction;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// カードバトル画面のView
/// プレイヤーはD&Dでカードを場に出し、敵カードが開示されて横並びに表示される
/// </summary>
public class CardBattleView : BasePhaseView
{
    [Header("勝利条件")]
    [SerializeField] private TextMeshProUGUI victoryConditionText;

    [Header("プレイヤー手札")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private DraggableCardView draggableCardPrefab;

    [Header("フィールド")]
    [SerializeField] private DeckSlotView playerFieldSlot;
    [SerializeField] private DeckSlotView enemyFieldSlot;

    [Header("ドラッグ演出")]
    [SerializeField] private DragLineView dragLineView;

    [Header("ダイヤモンドインジケータ")]
    [SerializeField] private DiamondIndicatorView diamondIndicatorView;

    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button nextButton;

    [Header("アニメーション")]
    [SerializeField] private StaggeredSlideInGroup cardStagger;

    /// <summary>プレイヤーがカードを場に出した</summary>
    public Observable<CardModel> OnCardSelected => _onCardSelected;

    /// <summary>次へボタンが押された</summary>
    public Observable<Unit> OnNextClicked => _onNextClicked;

    /// <summary>現在フィールドに仮置きされているカード</summary>
    public CardModel SelectedFieldCard => playerFieldSlot.IsOccupied ? playerFieldSlot.PlacedCard.CardModel : null;

    /// <summary>フィールドへ仮置きしたカードが変わった</summary>
    public Observable<CardModel> OnFieldCardChanged => _onFieldCardChanged;

    private readonly List<DraggableCardView> _handCards = new();
    private readonly Subject<CardModel> _onCardSelected = new();
    private readonly Subject<CardModel> _onFieldCardChanged = new();
    private readonly Subject<Unit> _onNextClicked = new();
    private CompositeDisposable _disposables = new();
    private RectTransform _handContainerRect;
    private DraggableCardView _enemyFieldCard;

    /// <summary>指示テキストを設定</summary>
    public void SetInstruction(string text) => instructionText.text = text;

    /// <summary>プレイヤーの手札をD&D可能カードとして表示する</summary>
    public void ShowPlayerHand(IReadOnlyList<CardModel> availableCards) => ShowPlayerHand(availableCards, null);

    /// <summary>ダイヤモンドインジケータを更新する</summary>
    public void UpdateDiamondIndicators(int playerWins, int enemyWins) => diamondIndicatorView.UpdateIndicators(playerWins, enemyWins);

    /// <summary>表示中の手札全体の操作可否を切り替える</summary>
    public void SetHandInteractable(bool interactable)
    {
        foreach (var card in _handCards)
        {
            if (card)
                card.SetInteractable(interactable);
        }

        if (!interactable)
            nextButton.gameObject.SetActive(false);
    }

    /// <summary>バトルを初期化する</summary>
    public void Initialize(VictoryCondition condition)
    {
        Show();

        victoryConditionText.text = condition == VictoryCondition.LowerWins
            ? "勝利条件: 数字が小さい方が勝利"
            : "勝利条件: 数字が大きい方が勝利";

        nextButton.gameObject.SetActive(false);

        // ダイヤモンドを未獲得状態にリセット
        diamondIndicatorView.UpdateIndicators(0, 0);

        // 敵スロットを初期状態でロックしておく
        enemyFieldSlot.SetLocked(true);
    }

    public void ShowPlayerHand(IReadOnlyList<CardModel> availableCards, int? forcedCardIndex)
    {
        ClearPlayerHand();
        handContainer.gameObject.SetActive(true);

        // 選択フェーズ中は敵スロットをロックして使用不可を示す
        enemyFieldSlot.SetLocked(true);

        for (var i = 0; i < availableCards.Count; i++)
        {
            var card = availableCards[i];
            var draggableCard = Instantiate(draggableCardPrefab, handContainer);
            draggableCard.Initialize(card, i);

            draggableCard.OnDragStarted.Subscribe(OnCardDragStarted).AddTo(_disposables);
            draggableCard.OnDragEnded.Subscribe(OnCardDragEnded).AddTo(_disposables);
            draggableCard.OnDragging.Subscribe(OnCardDragging).AddTo(_disposables);

            if (forcedCardIndex.HasValue)
                draggableCard.SetInteractable(forcedCardIndex.Value == i);

            _handCards.Add(draggableCard);
        }

        // レイアウト計算＋スライドインアニメーション
        cardStagger.Play();

        // フィールドスロットのドロップイベントを購読
        playerFieldSlot.OnCardDropped
            .Subscribe(tuple => OnCardDroppedToField(tuple.card))
            .AddTo(_disposables);

        // 確定ボタンでカード選択を確定
        nextButton.OnClickAsObservable()
            .Subscribe(_ => OnConfirmCardSelection())
            .AddTo(_disposables);
    }

    /// <summary>プレイヤーのカードを場に配置（確定後に手札をクリア）</summary>
    public void PlacePlayerCard(CardModel card)
    {
        // 配置済みカードを操作不可にする
        var placedCard = playerFieldSlot.PlacedCard;
        if (placedCard)
        {
            // 確定直前に変わった数値をスロット上の表示へ同期する
            placedCard.UpdateNumber(card.BattleNumber);
            placedCard.CanvasGroup.blocksRaycasts = false;
        }

        handContainer.gameObject.SetActive(false);
        ClearPlayerHand();
    }

    /// <summary>敵のカードを場に伏せる</summary>
    public void PlaceEnemyCard(CardModel enemyCard)
    {
        if (_enemyFieldCard)
            Destroy(_enemyFieldCard.gameObject);

        _enemyFieldCard = Instantiate(draggableCardPrefab, enemyFieldSlot.CardAnchor);
        _enemyFieldCard.Initialize(enemyCard, 0);
        _enemyFieldCard.ShowBack();
        _enemyFieldCard.CanvasGroup.blocksRaycasts = false;
    }

    /// <summary>表示中カードの数字を再描画する</summary>
    public void RefreshDisplayedCardNumbers()
    {
        // 手札・仮置き・敵札のどこに表示されていても再描画できるようにする
        foreach (var card in _handCards)
        {
            if (card)
                card.UpdateNumber(card.CardModel.BattleNumber);
        }

        if (playerFieldSlot.IsOccupied)
            playerFieldSlot.PlacedCard.UpdateNumber(playerFieldSlot.PlacedCard.CardModel.BattleNumber);

        if (_enemyFieldCard)
            _enemyFieldCard.UpdateNumber(_enemyFieldCard.CardModel.BattleNumber);
    }

    /// <summary>両者のカードをオープンする（横並びで比較表示）</summary>
    public void RevealCards(CardModel playerCard, CardModel enemyCard)
    {
        // 公開時は敵スロットのロックを解除する
        enemyFieldSlot.SetLocked(false);

        // プレイヤーカードの数字を更新（スキル効果による変更を反映）
        var placedCard = playerFieldSlot.PlacedCard;
        if (placedCard)
            placedCard.UpdateNumber(playerCard.BattleNumber);

        // 敵カードを表面に切り替え、数字を更新
        if (_enemyFieldCard)
        {
            _enemyFieldCard.ShowFront();
            _enemyFieldCard.UpdateNumber(enemyCard.BattleNumber);
        }
    }

    /// <summary>次へボタンを表示して待機</summary>
    public async UniTask WaitForNextAsync()
    {
        nextButton.gameObject.SetActive(true);
        await _onNextClicked.FirstAsync();
        nextButton.gameObject.SetActive(false);
    }

    /// <summary>場のカードをクリアする</summary>
    public void ClearField()
    {
        // プレイヤーのスロットをクリア
        var placedCard = playerFieldSlot.RemoveCard();
        if (placedCard)
            Destroy(placedCard.gameObject);

        // 敵カードをクリア
        if (_enemyFieldCard)
        {
            Destroy(_enemyFieldCard.gameObject);
            _enemyFieldCard = null;
        }
    }

    private void OnCardDragging(Vector3 cardWorldPos)
    {
        dragLineView.UpdateEndPosition(cardWorldPos);
    }

    // === D&D関連 ===

    private void OnCardDragStarted(DraggableCardView card)
    {
        var handCenterWorld = _handContainerRect.position;
        dragLineView.Show(handCenterWorld);
    }

    private void OnCardDragEnded(DraggableCardView card)
    {
        dragLineView.Hide();

        if (card.IsPlaced)
        {
            // 配置済みカードはスロットに戻す
            card.PlaySnapToSlotAsync(card.CurrentSlot.CardAnchor, Vector2.zero).Forget();
            return;
        }

        // スロット外にドロップされた場合、手札に戻す
        ReturnCardToHand(card);
    }

    private void OnCardDroppedToField(DraggableCardView droppedCard)
    {
        // 既にカードがある場合は入れ替え
        if (playerFieldSlot.IsOccupied)
        {
            var existingCard = playerFieldSlot.RemoveCard();
            ReturnCardToHand(existingCard);
        }

        playerFieldSlot.PlaceCard(droppedCard);
        droppedCard.PlaySnapToSlotAsync(playerFieldSlot.CardAnchor, Vector2.zero).Forget();
        droppedCard.UpdateNumber(droppedCard.CardModel.BattleNumber);
        _onFieldCardChanged.OnNext(droppedCard.CardModel);

        // カードを枠に配置した時のSE
        SeManager.Instance.PlaySe("SE_FRAME_LIGHT", pitch: 1f);

        // 確定ボタンを表示
        nextButton.gameObject.SetActive(true);
    }

    /// <summary>カード選択を確定する</summary>
    private void OnConfirmCardSelection()
    {
        if (!playerFieldSlot.IsOccupied) return;

        nextButton.gameObject.SetActive(false);
        _onCardSelected.OnNext(playerFieldSlot.PlacedCard.CardModel);
    }

    /// <summary>カードを手札コンテナに戻してレイアウトを再計算する</summary>
    private void ReturnCardToHand(DraggableCardView card)
    {
        var rt = card.transform as RectTransform;
        card.transform.SetParent(handContainer);
        card.transform.localScale = card.OriginalScale;
        card.transform.localEulerAngles = Vector3.zero;

        // アンカーをリセット（ドラッグ中にrootCanvasに移動しているため）
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = card.OriginalSizeDelta;

        // HandIndexの順番に並べ直す
        card.transform.SetSiblingIndex(GetSiblingIndexForHandIndex(card.HandIndex));

        // StaggeredSlideInGroupでレイアウト再計算
        cardStagger.ApplyLayout();
    }

    /// <summary>HandIndexに基づいて正しいSiblingIndexを計算する</summary>
    private int GetSiblingIndexForHandIndex(int handIndex)
    {
        for (var i = 0; i < handContainer.childCount; i++)
        {
            var child = handContainer.GetChild(i).GetComponent<DraggableCardView>();
            if (child && child.HandIndex > handIndex) return i;
        }

        return handContainer.childCount;
    }

    // === Lifecycle ===

    private void ClearPlayerHand()
    {
        _disposables.Dispose();
        _disposables = new CompositeDisposable();

        foreach (var card in _handCards)
            if (card && !card.IsPlaced)
                Destroy(card.gameObject);

        _handCards.Clear();
    }

    protected override void Awake()
    {
        base.Awake();
        _handContainerRect = handContainer as RectTransform;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        dragLineView.Initialize(canvas);

        nextButton.OnClickAsObservable()
            .Subscribe(_ => _onNextClicked.OnNext(Unit.Default))
            .AddTo(this);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
        _onCardSelected.Dispose();
        _onFieldCardChanged.Dispose();
        _onNextClicked.Dispose();
    }
}
