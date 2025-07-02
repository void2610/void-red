using System.Collections.Generic;
using R3;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// プレイヤーとNPCの基底プレゼンタークラス（Presenter Layer）
/// カード管理・UI制御・ゲームロジックを統合
/// </summary>
public abstract class PlayerPresenter : IDisposable
{
    // 公開プロパティ
    public ReadOnlyReactiveProperty<CardData> SelectedCard => _handModel.SelectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _playerModel.MentalPower;
    public ReadOnlyReactiveProperty<int> SelectedIndex => _handModel.SelectedIndex;
    public static int MaxMentalPower => PlayerModel.MaxMentalPower;
    public int HandCount => _handModel.Count;
    public int MaxHandSize => _handModel.MaxHandSize;
    
    // プライベートフィールド
    private readonly PlayerModel _playerModel;
    private readonly HandModel _handModel;
    private DeckModel _deckModel;
    private readonly HandView _handView;
    private readonly CompositeDisposable _disposables = new();
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="handView">手札ビュー</param>
    /// <param name="maxHandSize">最大手札数</param>
    protected PlayerPresenter(HandView handView, int maxHandSize = 3)
    {
        _playerModel = new PlayerModel();
        _handModel = new HandModel(maxHandSize);
        _handView = handView;
        
        // UI制御の設定
        _handView.BindModel(_handModel);
        
        // UIイベントを購読
        _handView.OnCardClicked
            .Subscribe(_handModel.SelectCardAt)
            .AddTo(_disposables);
    }
    
    // === デッキ・手札操作 ===
    
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deckModel = new DeckModel(cardDataList);
    }
    
    /// <summary>
    /// デッキが空かどうか
    /// </summary>
    public bool IsDeckEmpty => _deckModel?.IsEmpty ?? true;
    
    /// <summary>
    /// カードを引く
    /// </summary>
    public void DrawCard(int count = 1) => DrawCardAsync(count).Forget();
    
    /// <summary>
    /// カードを引く（待機可能）
    /// </summary>
    private async UniTask DrawCardAsync(int count = 1)
    {
        // 複数枚を一度に引く（1回のUpdateCardViewsで配置が決定される）
        var drawnCount = DrawCards(count);
        
        // 引いたカードがある場合はアニメーション完了まで待機
        if (drawnCount > 0)
        {
            // HandView側のドローアニメーション間隔: drawnCount * 150ms + 最終カードアニメーション500ms
            var animationTime = drawnCount * 150 + 500;
            await UniTask.Delay(animationTime);
        }
    }
    
    /// <summary>
    /// カードを引く（複数枚）
    /// </summary>
    /// <param name="count">引く枚数</param>
    /// <returns>実際に引けた枚数</returns>
    private int DrawCards(int count = 1)
    {
        var drawnCount = 0;
        
        for (var i = 0; i < count; i++)
        {
            if (IsDeckEmpty || HandCount >= MaxHandSize)
                break;
            
            var cardData = _deckModel?.DrawCard();
            if (cardData && _handModel.TryAddCard(cardData))
            {
                drawnCount++;
            }
            else
            {
                // 手札に追加できなかった場合はデッキに戻す
                if (cardData)
                    _deckModel?.ReturnCard(cardData);
                break;
            }
        }
        
        return drawnCount;
    }
    
    /// <summary>
    /// カードが引けるかどうかチェック
    /// </summary>
    /// <param name="count">引こうとする枚数</param>
    /// <returns>引けるかどうか</returns>
    public bool CanDrawCards(int count = 1)
    {
        return !IsDeckEmpty && HandCount + count <= MaxHandSize;
    }
    
    // === カード選択・操作 ===
    
    /// <summary>
    /// ランダムなカードを手札から取得 (敵の行動用)
    /// </summary>
    public CardData GetRandomCardDataFromHand() => _handModel.GetRandomCard();
    
    /// <summary>
    /// カードを選択
    /// </summary>
    public void SelectCard(CardData cardData) => _handModel.SelectCard(cardData);
    
    /// <summary>
    /// インデックスでカードを選択
    /// </summary>
    public void SelectCardAt(int index) => _handModel.SelectCardAt(index);
    
    /// <summary>
    /// カード選択を解除
    /// </summary>
    public void DeselectCard() => _handModel.DeselectCard();
    
    /// <summary>
    /// ランダムなカードを選択
    /// </summary>
    /// <returns>選択に成功したかどうか</returns>
    public bool SelectRandomCard()
    {
        var randomCard = _handModel.GetRandomCard();
        if (!randomCard) return false;
        
        _handModel.SelectCard(randomCard);
        return true;
    }
    
    // === カードプレイ ===
    
    /// <summary>
    /// 選択されたカードをプレイ
    /// </summary>
    /// <param name="shouldCollapse">崩壊させるかどうか</param>
    /// <returns>プレイに成功したかどうか</returns>
    public bool PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = SelectedCard.CurrentValue;
        if (!selectedCard) return false;
        
        // 手札から削除
        if (!_handModel.TryRemoveCard(selectedCard)) return false;
        
        if (!shouldCollapse)
        {
            // 崩壊しない場合はデッキに戻す
            _deckModel?.ReturnCard(selectedCard);
        }
        
        return true;
    }
    
    /// <summary>
    /// 指定したカードをプレイ
    /// </summary>
    /// <param name="card">プレイするカード</param>
    /// <param name="shouldCollapse">崩壊させるかどうか</param>
    /// <returns>プレイに成功したかどうか</returns>
    public bool PlayCard(CardData card, bool shouldCollapse)
    {
        if (!_handModel.HasCard(card)) return false;
        
        // カードを選択してからプレイ
        _handModel.SelectCard(card);
        return PlaySelectedCard(shouldCollapse);
    }
    
    /// <summary>
    /// カードがプレイ可能かどうかチェック
    /// </summary>
    /// <param name="card">チェックするカード</param>
    /// <returns>プレイ可能かどうか</returns>
    public bool CanPlayCard(CardData card) => _handModel.HasCard(card);
    
    /// <summary>
    /// 選択したカードを崩壊させる（UI制御付き）
    /// </summary>
    public void CollapseSelectedCard()
    {
        var selectedIndex = SelectedIndex.CurrentValue;
        
        // ビジネスロジック実行
        if (PlaySelectedCard(true))
        {
            // 崩壊アニメーションを再生
            if (selectedIndex >= 0)
            {
                _handView.PlayCollapseAnimation(selectedIndex).Forget();
            }
        }
    }
    
    // === 精神力管理 ===
    
    /// <summary>
    /// 精神力を消費
    /// </summary>
    /// <param name="amount">消費量</param>
    /// <returns>消費に成功したかどうか</returns>
    public bool ConsumeMentalPower(int amount) => _playerModel.TryConsumeMentalPower(amount);
    
    /// <summary>
    /// 精神力を回復
    /// </summary>
    /// <param name="amount">回復量</param>
    public void RestoreMentalPower(int amount) => _playerModel.RestoreMentalPower(amount);
    
    /// <summary>
    /// 精神力が足りるかチェック
    /// </summary>
    /// <param name="requiredAmount">必要な精神力</param>
    /// <returns>足りるかどうか</returns>
    public bool HasEnoughMentalPower(int requiredAmount) => _playerModel.MentalPower.CurrentValue >= requiredAmount;
    
    // === 手札・デッキリセット ===
    
    /// <summary>
    /// 手札をデッキに戻す（UI制御付き）
    /// </summary>
    public async UniTask ReturnHandToDeck()
    {
        // デッキに戻すアニメーション
        await _handView.ReturnCardsToDeck();
        
        // ビジネスロジック実行
        ReturnAllHandToDeck();
    }
    
    /// <summary>
    /// 手札を全てデッキに戻す
    /// </summary>
    /// <returns>戻したカードのリスト</returns>
    private List<CardData> ReturnAllHandToDeck()
    {
        var handCards = _handModel.GetAllCards();
        _handModel.Clear();
        _deckModel?.ReturnCards(handCards);
        return handCards;
    }
    
    /// <summary>
    /// プレイヤーの状態をリセット
    /// </summary>
    public void ResetPlayerState()
    {
        _handModel.DeselectCard();
        _handModel.Clear();
        _playerModel.SetMentalPower(PlayerModel.MaxMentalPower);
    }
    
    // === UI制御 ===
    
    /// <summary>
    /// 手札のインタラクション状態を設定
    /// </summary>
    public void SetHandInteractable(bool interactable) => _handView.SetInteractable(interactable);
    
    /// <summary>
    /// リソースの解放
    /// </summary>
    public virtual void Dispose()
    {
        _disposables?.Dispose();
        _playerModel?.Dispose();
    }
}