using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

public class BattlePresenter : IStartable, ISceneInitializable
{
    public ReadOnlyReactiveProperty<GameState> CurrentGameState => CurrentGameStateInternal;

    protected readonly BattleUIPresenter BattleUIPresenter;
    protected readonly Player Player;
    protected readonly Enemy Enemy;
    private readonly GameProgressService _gameProgressService;
    private readonly SceneTransitionManager _sceneTransitionManager;
    private readonly AllAuctionData _allAuctionData;

    protected IEnemyAIController EnemyAI;
    protected AuctionProcessor AuctionProcessor;
    protected CompetitionPhaseRunner CompetitionPhaseRunner;

    private readonly UniTaskCompletionSource _initializationComplete = new();

    private EnemyData _currentEnemyData;
    private ThemeData _currentTheme;
    private AuctionData _currentAuctionData;
    protected readonly List<CardModel> AuctionCards = new();

    // バトル関連
    private Dictionary<CardModel, CardNumberAssigner.CardNumberInfo> _cardNumbers;

    protected readonly ReactiveProperty<GameState> CurrentGameStateInternal = new(GameState.ThemeAnnouncement);

    public BattlePresenter(
        BattleUIPresenter battleUIPresenter,
        Player player,
        Enemy enemy,
        GameProgressService gameProgressService,
        SceneTransitionManager sceneTransitionManager,
        AllAuctionData allAuctionData)
    {
        BattleUIPresenter = battleUIPresenter;
        Player = player;
        Enemy = enemy;
        _gameProgressService = gameProgressService;
        _sceneTransitionManager = sceneTransitionManager;
        _allAuctionData = allAuctionData;

        EnemyAI = new EnemyAIController(Enemy);
        CompetitionPhaseRunner = new CompetitionPhaseRunner(Player, EnemyAI, BattleUIPresenter);
        AuctionProcessor = new AuctionProcessor(Player, Enemy, BattleUIPresenter, CompetitionPhaseRunner);

        InitializeAuctionData();
    }

    public UniTask WaitForInitializationAsync() => _initializationComplete.Task;

    protected virtual async UniTask HandleAuctionResult()
    {
        await AuctionProcessor.ProcessAuctionResultAsync(AuctionCards, _currentEnemyData, CurrentGameStateInternal);
    }

    protected virtual UniTask OnAfterCardRevealAsync()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnDeckSelectionShownAsync()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnAfterCardsDisplayed()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnAfterResourceGaugesDisplayed()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnAfterResourceRewardsAnimated()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnBeforeMemoryGrowthContinueAsync()
    {
        return UniTask.CompletedTask;
    }

    protected virtual UniTask OnAfterBattleEndAsync()
    {
        return UniTask.CompletedTask;
    }

    protected virtual void InitializeDeckSelectionView(IReadOnlyList<CardModel> wonCards)
    {
        BattleUIPresenter.InitializeDeckSelection(wonCards);
    }

    protected virtual void DecideFirstPlayer(CardBattleHandler handler)
    {
        handler.DecideFirstPlayer();
    }

    protected virtual bool CanUseBattleSkill(CardBattleHandler handler, EmotionType playerSkill)
    {
        return handler.PlayerSkillAvailable;
    }
    protected virtual bool CanUseDeckSelectionSkill(EmotionType playerSkill)
    {
        return BattleSkillExecutor.CanUseInDeckSelection(playerSkill);
    }
    protected virtual EmotionType GetDeckSelectionSkill(EmotionType defaultSkill)
    {
        return defaultSkill;
    }
    protected virtual EmotionType GetBattleSkill(EmotionType defaultSkill)
    {
        return defaultSkill;
    }
    protected virtual bool RequiresDeckSelectionSkillActivation(EmotionType playerSkill)
    {
        return false;
    }
    protected virtual bool RequiresBattleSkillActivation(CardBattleHandler handler, EmotionType playerSkill)
    {
        return false;
    }

    protected virtual VictoryCondition GetBattleVictoryCondition(VictoryCondition defaultVictoryCondition)
    {
        return defaultVictoryCondition;
    }

    protected virtual EmotionType GetEnemyBattleEmotionState(CardBattleHandler handler, EmotionType currentEmotionState)
    {
        return currentEmotionState;
    }

    protected virtual List<CardModel> BuildPlayerDeckCards(IReadOnlyList<CardModel> selectedCards, IReadOnlyList<CardModel> wonCards)
    {
        return selectedCards.ToList();
    }

    /// <summary>
    /// 対話選択肢を強制するインデックスを返す。null の場合は制限なし
    /// </summary>
    protected virtual int? GetForcedDialogueChoiceIndex()
    {
        return null;
    }

    protected virtual async UniTask<CardModel> SelectBattleCardAsync(CardBattleHandler handler, BattleDeckModel playerDeck)
    {
        BattleUIPresenter.ShowPlayerHand(playerDeck.GetAvailableCards());
        return await BattleUIPresenter.OnBattleCardSelected.FirstAsync();
    }

    // === 1. テーマ公開 ===

    protected virtual async UniTask HandleThemeAnnouncement()
    {
        // オークションデータからテーマを取得
        _currentTheme = _currentAuctionData.Theme;

        // 記憶テーマを表示
        await BattleUIPresenter.SetTheme(_currentTheme, isMainTheme: true);
    }

    // === 3. 入札フェーズ ===

    protected virtual async UniTask HandleBiddingPhase()
    {
        Debug.Log("[BattlePresenter] 入札フェーズ開始");

        // 敵AIで入札を決定
        EnemyAI.DecideBids(AuctionCards);
        Debug.Log($"[BattlePresenter] 敵の入札完了: 合計{Enemy.Bids.GetTotalBidAmount()}リソース");

        using var disposables = new CompositeDisposable();
        BattleUIPresenter.OnAuctionDialogueRequested
            .Subscribe(card => ShowAuctionDialogueAsync(card).Forget())
            .AddTo(disposables);

        // プレイヤーの入札UI表示・待機
        await BattleUIPresenter.WaitForBiddingAsync(AuctionCards, Player.Bids, EmotionType.Joy, Player.EmotionResources);

        Debug.Log($"[BattlePresenter] プレイヤーの入札完了: 合計{Player.Bids.GetTotalBidAmount()}リソース");

        // 入札対象カード公開演出
        await BattleUIPresenter.ShowBidTargetsAsync(Player.Bids, Enemy.Bids);
    }

    /// <summary>
    /// オークション中にカード対話を表示する
    /// </summary>
    /// <param name="card">対話対象のカード</param>
    protected async UniTask ShowAuctionDialogueAsync(CardModel card)
    {
        CurrentGameStateInternal.Value = GameState.DialoguePhase;
        await BattleUIPresenter.ShowAuctionCardDialogueAsync(card, _currentEnemyData, GetForcedDialogueChoiceIndex());
        CurrentGameStateInternal.Value = GameState.BiddingPhase;
    }

    private void InitializeAuctionData()
    {
        AuctionCards.Clear();

        // Player/Enemyの状態をリセット
        Player.ResetPlayerState();
        Enemy.ResetPlayerState();
    }

    private async UniTaskVoid InitializeGameAsync()
    {
        if (!await InitializeGame()) return;
        _initializationComplete.TrySetResult();
        await StartGame();
    }

    private async UniTask<bool> InitializeGame()
    {
        await UniTask.Delay(500);

        // GameProgressServiceからノード情報を取得
        var currentNode = _gameProgressService.GetCurrentNode();
        if (currentNode is not BattleNode battleNode)
        {
            Debug.LogError("[BattlePresenter] 現在のノードがBattleNodeではありません");
            await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
            return false;
        }

        // オークションデータを取得
        _currentAuctionData = _allAuctionData.GetAuctionById(battleNode.AuctionId);
        if (!_currentAuctionData)
        {
            Debug.LogError("[BattlePresenter] オークションデータが見つかりません");
            await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
            return false;
        }

        // オークションデータから敵データを取得
        _currentEnemyData = _currentAuctionData.Enemy;
        if (!_currentEnemyData)
        {
            Debug.LogError("[BattlePresenter] 敵データが見つかりません");
            await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
            return false;
        }

        // 敵を初期化して表示
        Enemy.SetEnemyData(_currentEnemyData);
        BattleUIPresenter.InitializeEnemy(_currentEnemyData);

        // 敵情報をアナウンス
        await BattleUIPresenter.ShowAnnouncement(_currentEnemyData.EnemyName, 1.5f);
        return true;
    }

    private async UniTask StartGame()
    {
        await UniTask.Delay(1000);

#if UNITY_EDITOR
        if (DebugBattleSettings.SkipAuction)
        {
            await StartDebugBattleAsync();
            return;
        }
#endif

        // ===== オークションパート =====

        // 1. テーマ公開
        CurrentGameStateInternal.Value = GameState.ThemeAnnouncement;
        await HandleThemeAnnouncement();

        // 2. カード提示（6枚を場に並べる）
        CurrentGameStateInternal.Value = GameState.CardReveal;
        await HandleCardReveal();

        // 3. 入札フェーズ（対話は入札中にオプションで実施）
        CurrentGameStateInternal.Value = GameState.BiddingPhase;
        await HandleBiddingPhase();

        // 5. 落札者判定フェーズ
        CurrentGameStateInternal.Value = GameState.AuctionResult;
        await HandleAuctionResult();

        // ===== リザルトパート =====

        // 6. リザルトフェーズ（獲得カード表示 + 感情リソース報酬 + 感情状態判定）
        CurrentGameStateInternal.Value = GameState.ResultPhase;
        var playerEmotionState = await HandleResultPhase();
        var deckSelectionSkill = GetDeckSelectionSkill(playerEmotionState);
        var battleSkill = GetBattleSkill(playerEmotionState);

        // ===== バトルパート =====

        // 7. カード数字割り当て
        _cardNumbers = CardNumberAssigner.AssignNumbers(AuctionCards, Player.Bids, Enemy.Bids);

        // 8. スキルボタン初期化 → デッキ選択
        BattleUIPresenter.InitializeSkillButton(deckSelectionSkill);
        CurrentGameStateInternal.Value = GameState.DeckSelection;
        var (playerDeck, enemyDeck, isPlayerSkillUsedInDeckSelection) = await HandleDeckSelection(deckSelectionSkill);

        // 9. カードバトル（3本勝負）
        BattleUIPresenter.InitializeSkillButton(battleSkill);
        CurrentGameStateInternal.Value = GameState.CardBattle;
        await HandleCardBattle(playerDeck, enemyDeck, battleSkill, GetBattleVictoryCondition(_currentAuctionData.VictoryCondition),
            isPlayerSkillUsedInDeckSelection);

        // ===== 終了処理 =====

        // 10. 記憶育成フェーズ
        CurrentGameStateInternal.Value = GameState.MemoryGrowth;
        await HandleMemoryGrowth();

        // 12. 終了
        CurrentGameStateInternal.Value = GameState.BattleEnd;
        await HandleBattleEnd();
    }

    // === 2. カード提示（6枚共有表示） ===

    private async UniTask HandleCardReveal()
    {
        Debug.Log("[BattlePresenter] カード提示フェーズ開始");

        // AuctionDataから6枚取得（プレイヤー/敵の区別なし）
        AuctionCards.Clear();
        foreach (var cardData in _currentAuctionData.AuctionCards)
        {
            AuctionCards.Add(new CardModel(cardData));
        }

        Debug.Log($"[BattlePresenter] オークション対象カード: {AuctionCards.Count}枚");

        // カードを表示
        BattleUIPresenter.ShowAuctionCards(AuctionCards, Player.EmotionResources);

        // トランジション：開く（黒フェードから復帰）
        await BattleUIPresenter.PlayPhaseTransitionOpenAsync();

        await OnAfterCardRevealAsync();
    }

    // === 6. リザルトフェーズ ===

    private async UniTask<EmotionType> HandleResultPhase()
    {
        Debug.Log("[BattlePresenter] リザルトフェーズ開始");

        // プレイヤーの報酬を計算
        var rewardResults = RewardCalculator.CalculateAll(Player.WonCards, Player.Bids);

        // 最大リソース値を設定（デフォルト値の3倍を仮の上限とする）
        var maxResources = new Dictionary<EmotionType, int>();
        foreach (EmotionType emotion in System.Enum.GetValues(typeof(EmotionType)))
        {
            maxResources[emotion] = GameConstants.DEFAULT_EMOTION_VALUE * 3;
        }

        await BattleUIPresenter.DisplayCardsAsync(rewardResults);
        await OnAfterCardsDisplayed();

        await BattleUIPresenter.WaitForCardAcquisitionCompleteAsync();

        BattleUIPresenter.DisplayResourceGauges(Player.EmotionResources, maxResources);
        await OnAfterResourceGaugesDisplayed();

        var rewardedAmounts = await BattleUIPresenter.AnimateResourceRewardsAsync(rewardResults);
        await OnAfterResourceRewardsAnimated();
        await BattleUIPresenter.WaitForResourceRewardNextAsync();

        // 報酬を各感情リソースに加算
        foreach (var (emotion, amount) in rewardedAmounts)
        {
            if (amount > 0)
            {
                Player.AddEmotion(emotion, amount);
                Debug.Log($"[BattlePresenter] プレイヤーに報酬付与: {emotion} +{amount}リソース");
            }
        }

        // 感情状態を判定（最も多い感情リソース = スキルの種類）
        var emotionState = MemoryEmotionCalculator.CalculateFromEmotionTotals(Player.EmotionResources);
        Debug.Log($"[BattlePresenter] プレイヤーの感情状態: {emotionState}");

        await UniTask.Delay(2000);
        return emotionState;
    }

    // === 8. デッキ選択フェーズ ===

    /// <summary>
    /// プレイヤーと敵のバトルデッキを構築し、デッキ選択中のスキル使用有無も返す
    /// </summary>
    /// <param name="playerSkill">このバトルでプレイヤーに割り当てられた感情スキル</param>
    /// <returns>プレイヤーデッキ、敵デッキ、デッキ選択中にスキルを使ったかを返す</returns>
    private async UniTask<(BattleDeckModel playerDeck, BattleDeckModel enemyDeck, bool isPlayerSkillUsed)> HandleDeckSelection(EmotionType playerSkill)
    {
        Debug.Log("[BattlePresenter] デッキ選択フェーズ開始");

        // カードにバトルデータを初期化
        foreach (var card in AuctionCards)
        {
            var info = _cardNumbers[card];
            card.InitializeBattleData(info.Number, info.TotalBid);
        }

        // プレイヤーの獲得カード
        var playerWonBattleCards = Player.WonCards
            .Where(c => AuctionCards.Contains(c))
            .ToList();

        // 不足分を補完（数字3のダミーカード）
        BattleDeckModel.FillWithDefaults(playerWonBattleCards);

        // デッキ選択中のスキル適用結果を、そのまま選択UIへ反映させるための作業用デッキ
        var previewDeck = new BattleDeckModel();
        previewDeck.SetDeck(playerWonBattleCards);
        var isPlayerSkillUsed = false;
        var canUseDeckSelectionSkill = CanUseDeckSelectionSkill(playerSkill);
        using var disposables = new CompositeDisposable();

        // デッキ選択UIとスキルボタンを表示
        InitializeDeckSelectionView(playerWonBattleCards);
        BattleUIPresenter.SetSkillButtonVisible(true);
        BattleUIPresenter.SetSkillButtonInteractable(canUseDeckSelectionSkill);
        BattleUIPresenter.SetDeckSelectionConfirmInteractable(!RequiresDeckSelectionSkillActivation(playerSkill));
        await OnDeckSelectionShownAsync();

        BattleUIPresenter.OnSkillActivated
            .Subscribe(_ =>
            {
                if (isPlayerSkillUsed || !BattleSkillExecutor.TryActivateInDeckSelection(playerSkill, previewDeck)) return;

                // デッキ選択中に使った場合は、その後のカードバトルでは再使用させない
                isPlayerSkillUsed = true;
                BattleUIPresenter.RefreshDeckSelectionCardNumbers();
                BattleUIPresenter.SetDeckSelectionConfirmInteractable(true);
                BattleUIPresenter.SetSkillButtonInteractable(false);
                Debug.Log($"[BattlePresenter] デッキ選択中にスキル発動: {playerSkill}");
            })
            .AddTo(disposables);

        await BattleUIPresenter.WaitForDeckSelectionAsync();

        BattleUIPresenter.SetSkillButtonInteractable(true);
        BattleUIPresenter.SetDeckSelectionConfirmInteractable(true);

        // プレイヤーのデッキ構築
        var playerDeck = new BattleDeckModel();
        playerDeck.SetDeck(BuildPlayerDeckCards(BattleUIPresenter.GetSelectedDeck(), playerWonBattleCards));
        BattleUIPresenter.HideDeckSelection();

        // 敵AIのデッキ選択
        var enemyWonBattleCards = Enemy.WonCards
            .Where(c => AuctionCards.Contains(c))
            .ToList();
        BattleDeckModel.FillWithDefaults(enemyWonBattleCards);

        var enemyDeck = new BattleDeckModel();
        enemyDeck.SetDeck(EnemyAI.SelectDeck(enemyWonBattleCards));

        Debug.Log($"[BattlePresenter] プレイヤーデッキ: {playerDeck.Cards.Count}枚, 敵デッキ: {enemyDeck.Cards.Count}枚");

        return (playerDeck, enemyDeck, isPlayerSkillUsed);
    }

    // === 9. カードバトルフェーズ ===

    /// <summary>
    /// 3本勝負のカードバトル全体を進行する
    /// </summary>
    /// <param name="playerDeck">プレイヤーのバトル用デッキ</param>
    /// <param name="enemyDeck">敵のバトル用デッキ</param>
    /// <param name="playerEmotionState">プレイヤーの感情状態。スキル種別としても使う</param>
    /// <param name="victoryCondition">このバトルの基本勝利条件</param>
    /// <param name="isPlayerSkillUsedInDeckSelection">デッキ選択中にスキルを使っている場合はtrue</param>
    /// <returns>バトルに勝利した場合はtrue</returns>
    private async UniTask<bool> HandleCardBattle(BattleDeckModel playerDeck, BattleDeckModel enemyDeck, EmotionType playerEmotionState,
        VictoryCondition victoryCondition, bool isPlayerSkillUsedInDeckSelection)
    {
        Debug.Log("[BattlePresenter] カードバトルフェーズ開始");

        // デッキ選択中に使用済みなら、バトル開始時点でスキル使用権を閉じる
        var handler = new CardBattleHandler(victoryCondition, !isPlayerSkillUsedInDeckSelection);

        // 敵の感情状態（ランダム）
        var enemyEmotionState = EnemyAI.DecideEmotionState();

        // バトルUI初期化
        BattleUIPresenter.InitializeBattle(victoryCondition);

        // ラウンドループ
        while (!handler.IsFinished)
        {
            Debug.Log($"[BattlePresenter] ラウンド {handler.CurrentRound + 1} 開始");
            enemyEmotionState = GetEnemyBattleEmotionState(handler, enemyEmotionState);

            // コイントス
            DecideFirstPlayer(handler);
            await BattleUIPresenter.PlayCoinFlipAsync(handler.IsPlayerFirst);

            // スキルボタン表示（カード選択中に使用可能）
            var showSkillButton = CanUseBattleSkill(handler, playerEmotionState);
            BattleUIPresenter.SetSkillButtonVisible(true);
            BattleUIPresenter.SetSkillButtonInteractable(showSkillButton);

            // カード伏せフェーズ
            bool shouldApplyDeferredPlayerSkill;
            using var playerSkillSession = new PlayerBattleSkillSession(BattleUIPresenter, handler, playerDeck, playerEmotionState);
            playerSkillSession.BeginListening();
            if (handler.IsPlayerFirst)
            {
                shouldApplyDeferredPlayerSkill = await PlayerPlaceCard(handler, playerDeck, playerSkillSession);
                EnemyAI.PlaceCard(handler, enemyDeck);
                BattleUIPresenter.PlaceEnemyCard(handler.EnemyCard);
            }
            else
            {
                EnemyAI.PlaceCard(handler, enemyDeck);
                BattleUIPresenter.PlaceEnemyCard(handler.EnemyCard);
                shouldApplyDeferredPlayerSkill = await PlayerPlaceCard(handler, playerDeck, playerSkillSession);
            }

            BattleUIPresenter.SetSkillButtonInteractable(false);

            // 敵AIのスキル発動判定
            if (EnemyAI.TryActivateSkill(handler, enemyDeck, enemyEmotionState))
            {
                BattleUIPresenter.SetBattleInstruction($"敵が{enemyEmotionState.ToJapaneseName()}スキルを発動！");
                Debug.Log($"[BattlePresenter] 敵がスキル発動: {enemyEmotionState}");
                await UniTask.Delay(1500);
            }

            if (shouldApplyDeferredPlayerSkill)
            {
                // Fearは相手カードが見える直前まで見た目の入れ替えを遅らせる
                playerSkillSession.ApplyDeferredSkill();
            }

            // カードオープン
            BattleUIPresenter.SetBattleInstruction("カードオープン！");
            BattleUIPresenter.RevealCards(handler.PlayerCard, handler.EnemyCard);
            await UniTask.Delay(1000);

            // 勝敗判定
            bool? competitionWinner = null;
            if (handler.RequiresCompetition)
                competitionWinner = await HandleBattleCompetitionAsync(handler);

            var result = handler.ResolveRound(competitionWinner);

            var resultText = result == RoundResult.PlayerWin ? "プレイヤー勝利！" : "敵の勝利...";
            BattleUIPresenter.SetBattleInstruction(resultText);
            BattleUIPresenter.UpdateDiamondIndicators(handler.PlayerWins, handler.EnemyWins);
            Debug.Log($"[BattlePresenter] ラウンド {handler.CurrentRound + 1} 結果: {result}");

            // 次へボタン待機
            await BattleUIPresenter.WaitForBattleNextAsync();

            // 次ラウンド準備
            if (!handler.IsFinished)
            {
                handler.NextRound();
                BattleUIPresenter.ClearBattleField();
            }
        }

        // バトル結果
        var isPlayerWon = handler.IsPlayerWon;
        Debug.Log($"[BattlePresenter] バトル終了: {(isPlayerWon ? "プレイヤー勝利" : "プレイヤー敗北")}");

        BattleUIPresenter.HideBattle();
        return isPlayerWon;
    }

    /// <summary>
    /// プレイヤーがカードを選んで伏せる（スキルボタンも同時に操作可能）
    /// </summary>
    /// <param name="handler">現在ラウンドの状態を管理するバトルハンドラ</param>
    /// <param name="playerDeck">プレイヤーのバトル用デッキ</param>
    /// <param name="playerSkillSession">このラウンド中のプレイヤースキル制御</param>
    /// <returns>開示直前に遅延適用するスキルが残っている場合はtrue</returns>
    private async UniTask<bool> PlayerPlaceCard(
        CardBattleHandler handler,
        BattleDeckModel playerDeck,
        PlayerBattleSkillSession playerSkillSession)
    {
        BattleUIPresenter.SetBattleInstruction("伏せるカードを選んでください");
        var selectedCard = await SelectBattleCardAsync(handler, playerDeck);
        handler.PlacePlayerCard(selectedCard);
        playerDeck.MarkAsUsed(selectedCard);
        BattleUIPresenter.PlacePlayerCard(selectedCard);
        await playerSkillSession.CompleteCardPlacementAsync();
        return playerSkillSession.ShouldApplyDeferredSkill;
    }

    /// <summary>
    /// バトル中に同数になった時の競合フェーズを実行し、勝者を返す
    /// </summary>
    /// <param name="handler">現在ラウンドのカード情報を持つバトルハンドラ</param>
    /// <returns>競合勝者。完全同数ならnull</returns>
    private async UniTask<bool?> HandleBattleCompetitionAsync(CardBattleHandler handler)
    {
        CurrentGameStateInternal.Value = GameState.CompetitionPhase;
        var competitionHandler = await CompetitionPhaseRunner.RunAsync(
            handler.PlayerCard,
            handler.PlayerCard.AuctionBidTotal,
            handler.EnemyCard.AuctionBidTotal,
            "同数のため競合発生！");
        CurrentGameStateInternal.Value = GameState.CardBattle;
        return competitionHandler.IsPlayerWon;
    }

    // === 10. 記憶育成フェーズ ===

    private async UniTask HandleMemoryGrowth()
    {
        Debug.Log("[BattlePresenter] 記憶育成フェーズ開始");

        // 全カードの獲得情報を構築（入札がないカードは除外）
        var allCardInfoList = BuildCardAcquisitionInfoList();

        // 入札されたカードがない場合はスキップ
        if (allCardInfoList.Count == 0)
        {
            Debug.Log("[BattlePresenter] 入札カードなし - 記憶育成フェーズをスキップ");
            BattleUIPresenter.HideRewardView();
            return;
        }

        // 使用した感情リソースを計算（プレイヤーの全入札から集計）
        var usedEmotions = CalculateUsedEmotions();

        // 獲得テーマを作成
        var acquiredTheme = new AcquiredTheme(_currentTheme, allCardInfoList, usedEmotions);

        Debug.Log($"[BattlePresenter] 支配的感情: {acquiredTheme.DominantEmotionResult}");
        Debug.Log($"[BattlePresenter] 獲得テーマ作成: {acquiredTheme.ThemeName}");
        Debug.Log($"[BattlePresenter] 勝利{acquiredTheme.WonCount}枚、敗北{acquiredTheme.LostCount}枚");

        // メモリに記録（ディスク保存はシーン遷移直前に行う）
        _gameProgressService.RecordAcquiredTheme(acquiredTheme);

        // 全獲得テーマを取得
        var allThemes = _gameProgressService.GetAcquiredThemes();
        Debug.Log($"[BattlePresenter] 全獲得テーマ数: {allThemes.Count}");

        BattleUIPresenter.ShowMemoryGrowthView(allThemes);
        BattleUIPresenter.HideRewardView();
        await OnBeforeMemoryGrowthContinueAsync();
        await BattleUIPresenter.WaitForMemoryGrowthCompleteAsync();
    }

    /// <summary>
    /// 全カードの獲得情報リストを構築
    /// </summary>
    private List<CardAcquisitionInfo> BuildCardAcquisitionInfoList()
    {
        var result = new List<CardAcquisitionInfo>();

        foreach (var card in AuctionCards)
        {
            var playerBids = Player.Bids.GetBidsByEmotion(card);
            var enemyBids = Enemy.Bids.GetBidsByEmotion(card);

            // どちらも入札していないカードは除外
            if (playerBids.Count == 0 && enemyBids.Count == 0) continue;

            var playerWon = Player.WonCards.Contains(card);
            var cardInfo = new CardAcquisitionInfo(card, playerBids, enemyBids, playerWon);
            result.Add(cardInfo);
        }

        return result;
    }

    /// <summary>
    /// 使用した感情リソースを計算（プレイヤーの全入札から集計）
    /// </summary>
    private Dictionary<EmotionType, int> CalculateUsedEmotions()
    {
        var result = new Dictionary<EmotionType, int>();

        foreach (var card in AuctionCards)
        {
            var bidsByEmotion = Player.Bids.GetBidsByEmotion(card);
            foreach (var kvp in bidsByEmotion)
            {
                result.TryAdd(kvp.Key, 0);
                result[kvp.Key] += kvp.Value;
            }
        }

        return result;
    }

    // === 終了 ===

    private async UniTask HandleBattleEnd()
    {
        await OnAfterBattleEndAsync();
        await UniTask.Delay(500);

        // Volumeエフェクトを全てデフォルトに戻す
        VolumeController.Instance.ResetToDefault();

        // 現在のノード情報を一旦キャッシュ
        var currentNode = _gameProgressService.GetCurrentNode();

        // バトル完了を記録（勝敗なし）
        _gameProgressService.RecordNovelResultAndSave();

        // ノード設定に基づいてシーン遷移
        await UniTask.Delay(1000);
        if (currentNode.ReturnToHome)
        {
            await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
        }
        else
        {
            var nextScene = _gameProgressService.GetNextSceneType();
            await _sceneTransitionManager.TransitionToSceneWithFade(nextScene);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// デバッグ用：オークションをスキップしてバトルフェーズから開始する
    /// </summary>
    private async UniTask StartDebugBattleAsync()
    {
        Debug.Log("[BattlePresenter] デバッグ: オークションスキップ開始");

        // デバッグ用カード（数字1〜6）を生成
        AuctionCards.Clear();
        for (var i = 1; i <= GameConstants.AUCTION_CARD_COUNT; i++)
            AuctionCards.Add(new CardModel(i));

        // カード番号マップを構築（入札0、数字は順番通り）
        _cardNumbers = new Dictionary<CardModel, CardNumberAssigner.CardNumberInfo>();
        for (var i = 0; i < AuctionCards.Count; i++)
        {
            _cardNumbers[AuctionCards[i]] = new CardNumberAssigner.CardNumberInfo
            {
                Number = i + 1,
                TotalBid = 0,
            };
        }

        // プレイヤーに全カードを付与（デッキ選択フェーズで3枚選ぶ）
        foreach (var card in AuctionCards)
            Player.AddWonCard(card);

        // スキルボタン初期化（DebugBattleSettingsで設定したスキルを使用）
        var playerEmotionState = DebugBattleSettings.PlayerSkill;
        Debug.Log($"[BattlePresenter] デバッグ: スキル={playerEmotionState}, 勝利条件={DebugBattleSettings.VictoryCondition}");

        BattleUIPresenter.InitializeSkillButton(playerEmotionState);
        CurrentGameStateInternal.Value = GameState.DeckSelection;

        // デッキ選択（通常フローと同じ）
        var (playerDeck, enemyDeck, isPlayerSkillUsedInDeckSelection) = await HandleDeckSelection(playerEmotionState);

        // カードバトル（通常フローと同じ）
        CurrentGameStateInternal.Value = GameState.CardBattle;
        await HandleCardBattle(playerDeck, enemyDeck, playerEmotionState, DebugBattleSettings.VictoryCondition,
            isPlayerSkillUsedInDeckSelection);

        // 終了（記憶育成はスキップしてHome遷移）
        CurrentGameStateInternal.Value = GameState.BattleEnd;
        VolumeController.Instance.ResetToDefault();
        await UniTask.Delay(1000);
        await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
    }
#endif

    public void Start()
    {
        BattleUIPresenter.SetBattlePresenter(this);

        InitializeGameAsync().Forget();
        BgmManager.Instance.PlayBGM("Battle");
    }
}
