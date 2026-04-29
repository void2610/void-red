using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// UIのビジネスロジックとイベント処理を担当するPresenterクラス
/// VContainerで依存性注入される
/// </summary>
public class BattleUIPresenter : IStartable, System.IDisposable
{
    public Observable<Unit> OnCompetitionRaise => _competitionView.OnRaise;
    public Observable<EmotionType> OnCompetitionEmotionSelected => _competitionView.OnEmotionSelected;
    public Observable<CardModel> OnAuctionDialogueRequested => _auctionView.OnDialogueRequested;
    public Observable<EmotionType> OnAuctionEmotionSelected => _auctionView.OnEmotionSelected;
    public Observable<int> OnAuctionCardClicked => _auctionView.OnCardClickedByIndex;
    public Observable<Unit> OnAuctionBidChanged => _auctionView.OnBidChanged;
    public Observable<Unit> OnAuctionBiddingConfirmed => _auctionView.OnBiddingConfirmed;
    public Observable<CardModel> OnBattleCardSelected => _cardBattleView.OnCardSelected;
    public Observable<CardModel> OnBattleFieldCardChanged => _cardBattleView.OnFieldCardChanged;
    // 仮置き中カードを参照して、確定前でもスキル適用できるようにする
    public CardModel SelectedBattleCard => _cardBattleView.SelectedFieldCard;
    public Observable<Unit> OnSkillActivated => _skillButtonView.OnActivated;
    public Observable<Unit> OnBattleNextClicked => _cardBattleView.OnNextClicked;
    public Observable<Unit> OnHelpButtonClickedInPause => _battlePauseView.OnHelpButtonClicked;

    [Inject] private readonly CardPoolService _cardPoolService;
    [Inject] private readonly GameProgressService _gameProgressService;
    [Inject] private readonly InputActionsProvider _inputActionsProvider;

    private readonly ThemeView _themeView;
    private readonly AnnouncementView _announcementView;
    private EnemyView _enemyView;
    private readonly BlackOverlayView _blackOverlayView;

    private readonly EyeBlinkTransitionView _eyeBlinkTransitionView;
    private readonly CompositeDisposable _disposables = new();
    private readonly TutorialPresenter _tutorialPresenter;
    private ThemeData _currentTheme;
    private readonly AuctionView _auctionView;
    private readonly DialoguePhaseView _dialoguePhaseView;
    private readonly CompetitionView _competitionView;
    private readonly RewardPhaseView _rewardPhaseView;
    private readonly MemoryGrowthView _memoryGrowthView;
    private readonly SkillButtonView _skillButtonView;
    private readonly DeckSelectionView _deckSelectionView;
    private readonly CardBattleView _cardBattleView;
    private readonly TargetCardSelectionView _targetCardSelectionView;
    private readonly CoinFlipView _coinFlipView;
    private readonly BattlePauseView _battlePauseView;
    private BattlePresenter _battlePresenter;

    public BattleUIPresenter(Player player, AllTutorialData allTutorialData, InputActionsProvider inputActionsProvider)
    {
        // 初期化
        _themeView = Object.FindFirstObjectByType<ThemeView>();
        _announcementView = Object.FindFirstObjectByType<AnnouncementView>();
        _enemyView = Object.FindFirstObjectByType<EnemyView>();
        _blackOverlayView = Object.FindFirstObjectByType<BlackOverlayView>();
        _eyeBlinkTransitionView = Object.FindFirstObjectByType<EyeBlinkTransitionView>();
        _auctionView = Object.FindFirstObjectByType<AuctionView>();
        _dialoguePhaseView = Object.FindFirstObjectByType<DialoguePhaseView>(FindObjectsInactive.Include);
        _competitionView = Object.FindFirstObjectByType<CompetitionView>();
        _rewardPhaseView = Object.FindFirstObjectByType<RewardPhaseView>();
        _memoryGrowthView = Object.FindFirstObjectByType<MemoryGrowthView>();
        _skillButtonView = Object.FindFirstObjectByType<SkillButtonView>(FindObjectsInactive.Include);
        _deckSelectionView = Object.FindFirstObjectByType<DeckSelectionView>();
        _cardBattleView = Object.FindFirstObjectByType<CardBattleView>();
        _targetCardSelectionView = Object.FindFirstObjectByType<TargetCardSelectionView>(FindObjectsInactive.Include);
        _coinFlipView = Object.FindFirstObjectByType<CoinFlipView>(FindObjectsInactive.Include);
        _battlePauseView = Object.FindFirstObjectByType<BattlePauseView>();

        _tutorialPresenter = new TutorialPresenter(allTutorialData, inputActionsProvider);
    }

    public async UniTask ShowAnnouncement(string message, float duration = 2f) => await _announcementView.DisplayAnnouncement(message, duration);
    public async UniTask PlayPhaseTransitionOpenAsync() => await _eyeBlinkTransitionView.PlayOpenAsync();
    public async UniTask PlayPhaseTransitionCloseAsync() => await _eyeBlinkTransitionView.PlayCloseAsync();
    public async UniTask StartTutorial(string tutorialId, params string[] args) => await _tutorialPresenter.StartTutorial(tutorialId, args);
    public async UniTask DisplayCardsAsync(Dictionary<CardModel, RewardCalculator.RewardResult> results) => await _rewardPhaseView.DisplayCardsAsync(results);
    public async UniTask WaitForCardAcquisitionCompleteAsync() => await _rewardPhaseView.WaitForCardAcquisitionCompleteAsync();
    public void ShowAuctionView() => _auctionView.Show();
    public void StartAuctionBidding(IReadOnlyList<CardModel> auctionCards, BidModel playerBids, EmotionType initialEmotion, IReadOnlyDictionary<EmotionType, int> emotionResources) => _auctionView.StartBidding(auctionCards, playerBids, initialEmotion, emotionResources);
    public void SetAuctionEmotionInteractable(bool interactable) => _auctionView.SetEmotionInteractable(interactable);
    public void SetAuctionCardInteractable(int index, bool interactable) => _auctionView.SetCardInteractable(index, interactable);
    public void SetAuctionDialogueInteractable(int index, bool interactable) => _auctionView.SetDialogueInteractable(index, interactable);
    public void SetAuctionAllDialogueInteractable(bool interactable) => _auctionView.SetAllDialogueInteractable(interactable);
    public void SetAuctionAllCardsInteractable(bool interactable) => _auctionView.SetAllCardsInteractable(interactable);
    public void SetAuctionConfirmInteractable(bool interactable) => _auctionView.SetConfirmInteractable(interactable);
    public void SetAuctionBidIncreaseInteractable(bool interactable) => _auctionView.SetBidIncreaseInteractable(interactable);
    public async UniTask ShowAuctionResultsSequentialAsync(IReadOnlyList<AuctionJudge.AuctionResultEntry> results, Color enemyColor) => await _auctionView.ShowResultsSequentialAsync(results, enemyColor);
    public async UniTask ShowBidTargetsAsync(BidModel playerBids, BidModel enemyBids, float duration = 2f) => await _auctionView.ShowBidTargetsAsync(playerBids, enemyBids, duration);
    public void HideAuctionView() => _auctionView.Hide();
    public async UniTask ShowAuctionCardDialogueAsync(CardModel card, EnemyData enemyData, int? forcedChoiceIndex = null) => await _dialoguePhaseView.ShowCardDialogueAsync(card, enemyData, forcedChoiceIndex);
    public void HideRewardView() => _rewardPhaseView.Hide();
    public void ShowMemoryGrowthView(IReadOnlyList<AcquiredTheme> allThemes) => _memoryGrowthView.ShowMemoryGrowth(allThemes);
    public UniTask WaitForMemoryGrowthCompleteAsync() => _memoryGrowthView.WaitForContinueAsync();
    public void DisplayResourceGauges(IReadOnlyDictionary<EmotionType, int> currentResources, IReadOnlyDictionary<EmotionType, int> maxResources) => _rewardPhaseView.DisplayResourceGauges(currentResources, maxResources);
    public async UniTask WaitForResourceRewardNextAsync() => await _rewardPhaseView.WaitForResourceRewardNextAsync();

    // 競合フェーズ
    public void ShowCompetition(int playerBid, int enemyBid, IReadOnlyDictionary<EmotionType, int> resources) => _competitionView.Initialize(playerBid, enemyBid, resources);
    public void SetCompetitionEmotion(EmotionType emotion) => _competitionView.SetSelectedEmotion(emotion);
    public void SetCompetitionEmotionInteractable(bool interactable) => _competitionView.SetEmotionInteractable(interactable);
    public void SetCompetitionRaiseInteractable(bool interactable) => _competitionView.SetRaiseInteractable(interactable);
    public void UpdateCompetitionBids(int playerBid, int enemyBid) => _competitionView.UpdateBids(playerBid, enemyBid);
    public void UpdateCompetitionTimer(float remaining, float max) => _competitionView.UpdateTimer(remaining, max);
    public void UpdateCompetitionResources(IReadOnlyDictionary<EmotionType, int> resources) => _competitionView.UpdateResources(resources);
    public void HideCompetition() => _competitionView.Hide();

    // デッキ選択フェーズ
    public void InitializeDeckSelection(IReadOnlyList<CardModel> wonCards) => _deckSelectionView.Initialize(wonCards);
    public void InitializeDeckSelection(IReadOnlyList<CardModel> wonCards, IReadOnlyList<int> allowedCardIndices) => _deckSelectionView.Initialize(wonCards, allowedCardIndices);
    public async UniTask WaitForDeckSelectionAsync() => await _deckSelectionView.WaitForSelectionAsync();
    public IReadOnlyList<CardModel> GetSelectedDeck() => _deckSelectionView.SelectedCards;
    // デッキ選択中スキルの結果をViewへ反映する
    public void RefreshDeckSelectionCardNumbers() => _deckSelectionView.RefreshCardNumbers();
    public void SetDeckSelectionConfirmInteractable(bool interactable) => _deckSelectionView.SetConfirmInteractable(interactable);
    public void HideDeckSelection() => _deckSelectionView.Hide();

    // スキルボタン
    public void InitializeSkillButton(EmotionType emotion) => _skillButtonView.Initialize(emotion);
    public void SetSkillButtonVisible(bool visible) => _skillButtonView.SetVisible(visible);
    public void SetSkillButtonInteractable(bool isInteractable) => _skillButtonView.SetInteractable(isInteractable);

    // コインフリップ
    public async UniTask PlayCoinFlipAsync(bool isPlayerFirst) => await _coinFlipView.PlayCoinFlipAsync(isPlayerFirst);

    // カードバトルフェーズ
    public void InitializeBattle(VictoryCondition condition) => _cardBattleView.Initialize(condition);
    public void ShowPlayerHand(IReadOnlyList<CardModel> availableCards) => _cardBattleView.ShowPlayerHand(availableCards);
    public void ShowPlayerHand(IReadOnlyList<CardModel> availableCards, int forcedCardIndex) => _cardBattleView.ShowPlayerHand(availableCards, forcedCardIndex);
    public void SetBattleHandInteractable(bool interactable) => _cardBattleView.SetHandInteractable(interactable);
    public void PlacePlayerCard(CardModel card) => _cardBattleView.PlacePlayerCard(card);
    public void PlaceEnemyCard(CardModel card) => _cardBattleView.PlaceEnemyCard(card);
    public void RevealCards(CardModel playerCard, CardModel enemyCard) => _cardBattleView.RevealCards(playerCard, enemyCard);
    public async UniTask<CardModel> WaitForTargetCardSelectionAsync(string instruction, IReadOnlyList<CardModel> selectableCards) => await _targetCardSelectionView.WaitForSelectionAsync(instruction, selectableCards);
    // バトル中スキルの結果を、現在表示中のカードへまとめて反映する
    public void RefreshBattleCardNumbers() => _cardBattleView.RefreshDisplayedCardNumbers();
    public void SetBattleInstruction(string text) => _cardBattleView.SetInstruction(text);
    public async UniTask WaitForBattleNextAsync() => await _cardBattleView.WaitForNextAsync();
    public void ClearBattleField() => _cardBattleView.ClearField();
    public void UpdateDiamondIndicators(int playerWins, int enemyWins) => _cardBattleView.UpdateDiamondIndicators(playerWins, enemyWins);
    public void HideBattle() => _cardBattleView.Hide();

    public void SetBattlePresenter(BattlePresenter battlePresenter)
    {
        _battlePresenter = battlePresenter;
        // BattlePresenterが設定されたらキーバインドをセットアップ
        BattleKeyBindings.Setup(_inputActionsProvider, this, _themeView, _battlePresenter.CurrentGameState, _disposables);
    }

    public async UniTask SetTheme(ThemeData theme, bool isMainTheme)
    {
        _currentTheme = theme;
        await _themeView.DisplayThemeWithKeywords(theme, isMainTheme);
    }

    public void InitializeEnemy(EnemyData enemyData)
    {
        _enemyView = Object.FindFirstObjectByType<EnemyView>();
        _enemyView.Initialize(enemyData);
    }

    // カード提示（6枚共有表示）
    public void ShowAuctionCards(
        IReadOnlyList<CardModel> auctionCards,
        IReadOnlyDictionary<EmotionType, int> emotionResources)
    {
        _auctionView.Show();
        _auctionView.ShowCards(auctionCards);
        _auctionView.UpdateEmotionResources(emotionResources);
        _auctionView.SetSelectedEmotion(EmotionType.Joy);
    }

    // 入札待機（BiddingPhaseフェーズ）
    public async UniTask WaitForBiddingAsync(
        IReadOnlyList<CardModel> auctionCards,
        BidModel playerBids,
        EmotionType initialEmotion,
        IReadOnlyDictionary<EmotionType, int> emotionResources)
    {
        _auctionView.Show();
        _auctionView.StartBidding(auctionCards, playerBids, initialEmotion, emotionResources);

        await _auctionView.OnBiddingConfirmed.FirstAsync();
    }

    // AuctionViewをクリアして非表示
    public void ClearAuctionView()
    {
        _auctionView.Clear();
        _auctionView.Hide();
    }

    public async UniTask<IReadOnlyDictionary<EmotionType, int>> AnimateResourceRewardsAsync(
        Dictionary<CardModel, RewardCalculator.RewardResult> results)
    {
        await _rewardPhaseView.AnimateResourceRewardsAsync(results);
        return _rewardPhaseView.RewardedAmounts;
    }

    // ルートボタンを初期選択
    public void Start()
    {
        SafeNavigationManager.SelectRootForceSelectable().Forget();
    }

    // すべてのViewのイベントを解除
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
