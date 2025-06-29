using R3;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using UnityEngine;

public class GameManager: IStartable
{
    public ReadOnlyReactiveProperty<GameState> CurrentState => _currentState;
    
    private readonly CardPoolService _cardPoolService;
    private readonly ThemeService _themeService;
    private readonly UIPresenter _uiPresenter;
    private readonly Player _player;
    private readonly Enemy _enemy;
    private readonly ReactiveProperty<GameState> _currentState = new (GameState.ThemeAnnouncement);
    private readonly ReactiveProperty<ThemeData> _currentTheme = new (null);
    private PlayerMove _playerMove;
    private PlayerMove _npcMove;
    private bool _isProcessing = false;
    
    /// <summary>
    /// コンストラクタ（依存性注入）
    /// </summary>
    public GameManager(
        CardPoolService cardPoolService,
        ThemeService themeService,
        UIPresenter uiPresenter,
        Player player,
        Enemy enemy)
    {
        _cardPoolService = cardPoolService;
        _themeService = themeService;
        _uiPresenter = uiPresenter;
        _player = player;
        _enemy = enemy;
    }
    
    public void Start()
    {
        InitializeGame().Forget();
    }
    
    /// <summary>
    /// ゲームを初期化
    /// </summary>
    private async UniTaskVoid InitializeGame()
    {
        await UniTask.Delay(500);
        
        // カードデッキを初期化
        var playerDeck = _cardPoolService.GetRandomCards(10);
        var npcDeck = _cardPoolService.GetRandomCards(10);
        
        _player.InitializeDeck(playerDeck);
        _enemy.InitializeDeck(npcDeck);
        
        // 手札を配る
        _player.DrawCard(3);
        await UniTask.Delay(200);
        _enemy.DrawCard(3);
        
        // エネミーのカードを非インタラクティブに設定
        _enemy.SetHandInteractable(false);
        
        // ゲーム開始
        ChangeState(GameState.ThemeAnnouncement);
    }
    
    /// <summary>
    /// ステートを変更
    /// </summary>
    private void ChangeState(GameState newState)
    {
        _currentState.Value = newState;
        switch (newState)
        {
            case GameState.ThemeAnnouncement:
                _isProcessing = false; // フラグリセット
                HandleThemeAnnouncement();
                break;
            case GameState.PlayerCardSelection:
                _isProcessing = false; // フラグリセット
                HandlePlayerCardSelection();
                break;
            case GameState.EnemyCardSelection:
                _isProcessing = false; // フラグリセット
                HandleEnemyCardSelection();
                break;
            case GameState.Evaluation:
                HandleEvaluation();
                break;
            case GameState.ResultDisplay:
                HandleResultDisplay();
                break;
        }
    }
    
    /// <summary>
    /// お題発表フェーズ
    /// </summary>
    private void HandleThemeAnnouncement()
    {
        
        // ランダムなお題を選択
        _currentTheme.Value = _themeService.GetRandomTheme();
        _uiPresenter.SetTheme(_currentTheme.Value);
        
        DelayedStateChangeAsync(GameState.PlayerCardSelection, 0.3f).Forget();
    }
    
    /// <summary>
    /// プレイヤーカード選択フェーズ
    /// </summary>
    private void HandlePlayerCardSelection()
    {
        _uiPresenter.ShowAnnouncement("プレイヤーのカード選択を待機中...", 0.5f).Forget();
        
        // プレイヤーの操作を待つ（カード選択とプレイボタン）
        WaitForPlayerActionAsync().Forget();
    }
    
    /// <summary>
    /// プレイヤーのアクションを待つ
    /// </summary>
    private async UniTask WaitForPlayerActionAsync()
    {
        // カード選択を待つ
        while (true)
        {
            await UniTask.Yield();
            
            // ゲーム状態が変わったら終了
            if (_currentState.Value != GameState.PlayerCardSelection)
                return;
            
            var selectedCard = _player.SelectedCard.CurrentValue;
            if (selectedCard != null)
            {
                // カードが選択されたらプレイボタンを表示
                _uiPresenter.ShowPlayButton();
                break;
            }
        }
        
        // プレイボタンが押されるのを待つ
        await _uiPresenter.PlayButtonClicked.FirstAsync();
        
        _uiPresenter.HidePlayButton();
        // 選択されたカードを再取得
        var finalSelectedCard = _player.SelectedCard.CurrentValue;
        if (finalSelectedCard == null)
            return;
        
        // プレイヤーの手を作成
        var playStyle = _uiPresenter.GetSelectedPlayStyle();
        var mentalBet = _uiPresenter.GetMentalBetValue();
        
        // 精神力を消費
        _player.ConsumeMentalPower(mentalBet);
        _playerMove = new PlayerMove(finalSelectedCard.CardData, playStyle, mentalBet);
        
        // プレイヤーの選択を表示
        await _uiPresenter.ShowAnnouncement($"プレイヤーが {_playerMove.SelectedCard.CardName} を「{_playerMove.PlayStyle.ToJapaneseString()}」で選択（精神ベット: {_playerMove.MentalBet}）", 1.0f);
        
        // 少し間を置いてから敵フェーズに移行
        await UniTask.Delay(500);
        ChangeState(GameState.EnemyCardSelection);
    }
    
    /// <summary>
    /// 敵カード選択フェーズ
    /// </summary>
    private void HandleEnemyCardSelection()
    {
        NpcThinkAndSelectAsync().Forget();
    }
    
    /// <summary>
    /// NPCの思考と選択
    /// </summary>
    private async UniTask NpcThinkAndSelectAsync()
    {
        // 思考時間を待つ（NPCが考えているように見せる）
        await UniTask.Delay(1000);
        
        // AIでカードを選択
        var npcCard = _enemy.SelectCardByAI();
        // NPCの手を作成（NPCもランダムなプレイスタイルと精神ベットを選択）
        var npcPlayStyle = (PlayStyle)UnityEngine.Random.Range(0, 3);
        var npcMentalBet = UnityEngine.Random.Range(1, Mathf.Min(6, _enemy.MentalPower.CurrentValue + 1)); // NPCの精神力範囲内でベット
        
        // NPCの精神力を消費
        _enemy.ConsumeMentalPower(npcMentalBet);
        _npcMove = new PlayerMove(npcCard.CardData, npcPlayStyle, npcMentalBet);
        
        // NPCの選択を表示
        await _uiPresenter.ShowAnnouncement($"NPCが {_npcMove.SelectedCard.CardName} を「{_npcMove.PlayStyle.ToJapaneseString()}」で選択（精神ベット: {_npcMove.MentalBet}）", 1.0f);
        // 少し間を置いてから評価フェーズに移行
        await UniTask.Delay(500);
        
        // 評価フェーズへ
        ChangeState(GameState.Evaluation);
    }
    
    /// <summary>
    /// 評価フェーズ
    /// </summary>
    private void HandleEvaluation()
    {
        if (_isProcessing) return; // 既に処理中の場合はスキップ
        EvaluationAsync(_playerMove, _npcMove).Forget();
    }
    
    /// <summary>
    /// 評価処理
    /// </summary>
    private async UniTask EvaluationAsync(PlayerMove playerMove, PlayerMove npcMove)
    {
        _isProcessing = true; // 処理開始フラグ
        
        // スコアを計算（テーマとの一致度 × 精神ベット）
        var currentThemeStatus = _currentTheme.CurrentValue?.CardStatus;
        if (currentThemeStatus == null) return;
        
        var playerScore = playerMove.GetScore(currentThemeStatus);
        var npcScore = npcMove.GetScore(currentThemeStatus);
        
        // 評価結果を順次表示
        await _uiPresenter.ShowAnnouncement($"プレイヤーのスコア: {playerScore:F2}", 1f);
        await UniTask.Delay(300);
        await _uiPresenter.ShowAnnouncement($"NPCのスコア: {npcScore:F2}", 1f);
        
        // 結果表示フェーズに移行
        await UniTask.Delay(500);
        _isProcessing = false; // フラグリセット
        ChangeState(GameState.ResultDisplay);
    }
    
    /// <summary>
    /// 勝敗表示フェーズ
    /// </summary>
    private void HandleResultDisplay()
    {
        if (_isProcessing) return; // 既に処理中の場合はスキップ
        ResultDisplayAsync().Forget();
    }
    
    /// <summary>
    /// 結果表示処理
    /// </summary>
    private async UniTask ResultDisplayAsync()
    {
        _isProcessing = true; // 処理開始フラグ
        
        // スコアで勝敗判定（スコアが高い方が勝利）
        var currentThemeStatus = _currentTheme.CurrentValue?.CardStatus;
        if (currentThemeStatus == null) return;
        
        var playerScore = _playerMove.GetScore(currentThemeStatus);
        var npcScore = _npcMove.GetScore(currentThemeStatus);
        
        string result;
        if (playerScore > npcScore)
            result = "プレイヤーの勝利!";
        else if (npcScore > playerScore)
            result = "NPCの勝利!";
        else
            result = "引き分け!";
        
        // 結果を表示
        await _uiPresenter.ShowAnnouncement(result, 2f);
        
        // カード崩壊判定
        var playerCollapse = _playerMove.ShouldCollapse();
        var npcCollapse = _npcMove.ShouldCollapse();
        
        // 崩壊結果を表示
        if (playerCollapse || npcCollapse)
        {
            var collapseMessage = "";
            if (playerCollapse && npcCollapse)
                collapseMessage = "プレイヤーとNPCのカードが崩壊した！";
            else if (playerCollapse)
                collapseMessage = "プレイヤーのカードが崩壊した！";
            else
                collapseMessage = "NPCのカードが崩壊した！";
                
            await _uiPresenter.ShowAnnouncement(collapseMessage, 1.0f);
        }
        
        // 使用したカードをプレイ（崩壊判定を含む）
        if (playerCollapse)
            _player.CollapseSelectedCard();
        else
            _player.PlaySelectedCard(false);
            
        if (npcCollapse)
            _enemy.CollapseSelectedCard();
        else
            _enemy.PlaySelectedCard(false);
        
        // カード使用後の処理完了を待つ
        await UniTask.Delay(1000);
        
        // 両プレイヤーの手札をデッキに戻す
        var returnTasks = new UniTask[2];
        returnTasks[0] = _player.ReturnHandToDeck();
        returnTasks[1] = _enemy.ReturnHandToDeck();
        
        await UniTask.WhenAll(returnTasks);
        
        // 手札を3枚ずつ配る
        _player.DrawCard(3);
        await UniTask.Delay(500);
        _enemy.DrawCard(3);
        
        // 新しいラウンドの準備時間
        await UniTask.Delay(1000);
        _isProcessing = false; // フラグリセット
        ChangeState(GameState.ThemeAnnouncement);
    }
    
    /// <summary>
    /// 遅延してステート変更
    /// </summary>
    private async UniTask DelayedStateChangeAsync(GameState newState, float delay)
    {
        await UniTask.Delay((int)(delay * 1000));
        ChangeState(newState);
    }
}
