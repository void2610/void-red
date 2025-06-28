using UnityEngine;
using Void2610.UnityTemplate;
using R3;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("ゲーム設定")]
    [SerializeField] private Player player;
    [SerializeField] private Enemy enemy;
    
    // DIされるサービス
    [Inject] private readonly CardPoolService _cardPoolService;
    
    private readonly ReactiveProperty<GameState> _currentState = new (GameState.ThemeAnnouncement);
    private readonly ReactiveProperty<CardStatus> _currentTheme = new (null);
    
    // プレイヤーとNPCの手
    private PlayerMove _playerMove;
    private PlayerMove _npcMove;
    
    // 実行中フラグ（重複実行防止）
    private bool _isProcessing = false;
    
    // プロパティ
    public ReadOnlyReactiveProperty<GameState> CurrentState => _currentState;
    
    private void Start()
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
        
        player.InitializeDeck(playerDeck);
        enemy.InitializeDeck(npcDeck);
        
        // 手札を配る
        player.DrawCard(5);
        await UniTask.Delay(200);
        enemy.DrawCard(5);
        
        // ゲーム開始
        ChangeState(GameState.ThemeAnnouncement);
    }
    
    /// <summary>
    /// ステートを変更
    /// </summary>
    public void ChangeState(GameState newState)
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
        _currentTheme.Value = new CardStatus(
            forgiveness: Random.Range(0f, 1f),
            rejection: Random.Range(0f, 1f),
            blank: Random.Range(0f, 1f)
        );
        UIManager.Instance.SetTheme(_currentTheme.Value);
        
        DelayedStateChangeAsync(GameState.PlayerCardSelection, 0.3f).Forget();
    }
    
    /// <summary>
    /// プレイヤーカード選択フェーズ
    /// </summary>
    private void HandlePlayerCardSelection()
    {
        UIManager.Instance.ShowAnnouncement("プレイヤーのカード選択を待機中...", 0.5f).Forget();
        
        // プレイヤーの選択を監視
        player.SelectedCard.Subscribe(card =>
        {
            if (card && _currentState.Value == GameState.PlayerCardSelection)
            {
                // プレイボタンを表示してプレイヤーの確定を待つ
                UIManager.Instance.ShowPlayButton();
                WaitForPlayButtonAsync(card).Forget();
            }
        }).AddTo(this);
    }
    
    /// <summary>
    /// プレイボタンが押されるのを待つ
    /// </summary>
    private async UniTask WaitForPlayButtonAsync(Card selectedCard)
    {
        // プレイボタンが押されるのを待つ
        await UIManager.Instance.PlayButtonClicked.FirstAsync();
        
        // プレイヤーの手を作成
        var playStyle = UIManager.Instance.GetSelectedPlayStyle();
        var mentalBet = UIManager.Instance.GetMentalBetValue();
        
        // 精神力を消費
        player.ConsumeMentalPower(mentalBet);
        _playerMove = new PlayerMove(selectedCard, playStyle, mentalBet);
        
        // プレイヤーの選択を表示
        await UIManager.Instance.ShowAnnouncement($"プレイヤーが {_playerMove.SelectedCard.CardData.CardName} を{_playerMove.PlayStyle.ToJapaneseString()}で選択（精神ベット: {_playerMove.MentalBet}）", 1.0f);
        
        // デバッグログで詳細を表示
        Debug.Log($"プレイヤーの選択 - {_playerMove}");
        
        // 少し間を置いてから敵フェーズに移行
        await UniTask.Delay(500);
        ChangeState(GameState.EnemyCardSelection);
    }
    
    /// <summary>
    /// 敵カード選択フェーズ
    /// </summary>
    private void HandleEnemyCardSelection()
    {
        UIManager.Instance.ShowAnnouncement("NPCがカードを選択中...", 0.5f).Forget();
        NpcThinkAndSelectAsync().Forget();
    }
    
    /// <summary>
    /// NPCの思考と選択
    /// </summary>
    private async UniTask NpcThinkAndSelectAsync()
    {
        // 思考時間を待つ（NPCが考えているように見せる）
        await UniTask.Delay(2000);
        
        // AIでカードを選択
        var npcCard = enemy.SelectCardByAI();
        // NPCの手を作成（NPCもランダムなプレイスタイルと精神ベットを選択）
        var npcPlayStyle = (PlayStyle)Random.Range(0, 3);
        var npcMentalBet = Random.Range(1, Mathf.Min(6, enemy.MentalPower.CurrentValue + 1)); // NPCの精神力範囲内でベット
        
        // NPCの精神力を消費
        enemy.ConsumeMentalPower(npcMentalBet);
        _npcMove = new PlayerMove(npcCard, npcPlayStyle, npcMentalBet);
        
        // NPCの選択を表示
        await UIManager.Instance.ShowAnnouncement($"NPCが {_npcMove.SelectedCard.CardData.CardName} を{_npcMove.PlayStyle.ToJapaneseString()}で選択（精神ベット: {_npcMove.MentalBet}）", 1.0f);
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
        
        // 評価中のアナウンス
        await UIManager.Instance.ShowAnnouncement("カードを評価中...", 0.5f);
        
        // スコアを計算（テーマとの一致度 × 精神ベット）
        var playerScore = playerMove.GetScore(_currentTheme.CurrentValue);
        var npcScore = npcMove.GetScore(_currentTheme.CurrentValue);
        
        // 評価結果を順次表示
        await UIManager.Instance.ShowAnnouncement($"プレイヤーのスコア: {playerScore:F2}", 1f);
        await UniTask.Delay(300);
        await UIManager.Instance.ShowAnnouncement($"NPCのスコア: {npcScore:F2}", 1f);
        
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
        var playerScore = _playerMove.GetScore(_currentTheme.CurrentValue);
        var npcScore = _npcMove.GetScore(_currentTheme.CurrentValue);
        
        string result;
        if (playerScore > npcScore)
            result = "プレイヤーの勝利!";
        else if (npcScore > playerScore)
            result = "NPCの勝利!";
        else
            result = "引き分け!";
        
        // 結果を表示
        await UIManager.Instance.ShowAnnouncement(result, 2f);
        
        // 使用したカードをプレイ
        player.PlaySelectedCard();
        enemy.PlaySelectedCard();
        
        // 新しいラウンドの準備時間
        await UniTask.Delay(2000);
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
