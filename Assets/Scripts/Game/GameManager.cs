using UnityEngine;
using Void2610.UnityTemplate;
using R3;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("ゲーム設定")]
    [SerializeField] private AllCardDataList allCardDataList;
    [SerializeField] private Player player;
    [SerializeField] private Enemy enemy;
    
    
    private readonly ReactiveProperty<GameState> _currentState = new (GameState.ThemeAnnouncement);
    private readonly ReactiveProperty<CardStatus> _currentTheme = new (null);
    
    // 選択されたカード、プレイスタイル、精神ベット
    private Card _playerSelectedCard;
    private Card _npcSelectedCard;
    private PlayStyle _playerPlayStyle;
    private int _playerMentalBet;
    
    // プロパティ
    public ReadOnlyReactiveProperty<GameState> CurrentState => _currentState;
    
    protected override void Awake()
    {
        base.Awake();
        allCardDataList.RegisterAllCards();
    }
    
    private void Start()
    {
        InitializeGame();
    }
    
    /// <summary>
    /// ゲームを初期化
    /// </summary>
    private void InitializeGame()
    {
        // カードデッキを初期化
        if (allCardDataList && allCardDataList.Count > 0)
        {
            var playerDeck = allCardDataList.GetRandomCards(10);
            var npcDeck = allCardDataList.GetRandomCards(10);
            
            player?.InitializeDeck(playerDeck);
            enemy?.InitializeDeck(npcDeck);
            
            // 手札を配る
            player?.DrawCard(5);
            enemy?.DrawCard(5);
        }
        
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
                HandleThemeAnnouncement();
                break;
            case GameState.PlayerCardSelection:
                HandlePlayerCardSelection();
                break;
            case GameState.EnemyCardSelection:
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
        
        DelayedStateChangeAsync(GameState.PlayerCardSelection, 2f, this.GetCancellationTokenOnDestroy()).Forget();
    }
    
    /// <summary>
    /// プレイヤーカード選択フェーズ
    /// </summary>
    private void HandlePlayerCardSelection()
    {
        UIManager.Instance.ShowAnnouncement("プレイヤーのカード選択を待機中...", 1f).Forget();
        
        // プレイヤーの選択を監視
        if (player)
        {
            player.SelectedCard.Subscribe(card =>
            {
                if (card && _currentState.Value == GameState.PlayerCardSelection)
                {
                    _playerSelectedCard = card;
                    // プレイボタンを表示してプレイヤーの確定を待つ
                    UIManager.Instance.ShowPlayButton(card.CardData.CardName);
                    WaitForPlayButtonAsync().Forget();
                }
            }).AddTo(this);
        }
    }
    
    /// <summary>
    /// プレイボタンが押されるのを待つ
    /// </summary>
    private async UniTask WaitForPlayButtonAsync()
    {
        // プレイボタンが押されるのを待つ
        await UIManager.Instance.PlayButtonClicked.FirstAsync();
        
        // 選択されたプレイスタイルと精神ベットを取得
        _playerPlayStyle = UIManager.Instance.GetSelectedPlayStyle();
        _playerMentalBet = UIManager.Instance.GetMentalBetValue();
        
        // プレイヤーの選択を表示（プレイスタイルと精神ベットを含めて）
        var playStyleText = _playerPlayStyle.ToJapaneseString();
        await UIManager.Instance.ShowAnnouncement($"プレイヤーが {_playerSelectedCard.CardData.CardName} を{playStyleText}で選択（精神ベット: {_playerMentalBet}）", 2.5f);
        
        // デバッグログで詳細を表示
        Debug.Log($"プレイヤーの選択 - カード: {_playerSelectedCard.CardData.CardName}, スタイル: {playStyleText} ({_playerPlayStyle.GetDescription()}), 精神ベット: {_playerMentalBet}");
        
        // 少し間を置いてから敵フェーズに移行
        await UniTask.Delay(500);
        ChangeState(GameState.EnemyCardSelection);
    }
    
    /// <summary>
    /// 敵カード選択フェーズ
    /// </summary>
    private void HandleEnemyCardSelection()
    {
        UIManager.Instance.ShowAnnouncement("NPCがカードを選択中...", 1f).Forget();
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
        _npcSelectedCard = enemy.SelectCardByAI();
        
        if (_npcSelectedCard)
        {
            // NPCの選択を表示
            await UIManager.Instance.ShowAnnouncement($"NPCが {_npcSelectedCard.CardData.CardName} を選択", 2f);
            // 少し間を置いてから評価フェーズに移行
            await UniTask.Delay(500);
        }
        
        // 評価フェーズへ
        ChangeState(GameState.Evaluation);
    }
    
    /// <summary>
    /// 評価フェーズ
    /// </summary>
    private void HandleEvaluation()
    {
        EvaluationAsync().Forget();
    }
    
    /// <summary>
    /// 評価処理
    /// </summary>
    private async UniTask EvaluationAsync()
    {
        // 評価中のアナウンス
        await UIManager.Instance.ShowAnnouncement("カードを評価中...", 1.5f);
        
        // テーマとの距離を計算
        var playerDistance = _playerSelectedCard?.CardData.Effect?.GetDistanceTo(_currentTheme.CurrentValue) ?? float.MaxValue;
        var npcDistance = _npcSelectedCard?.CardData.Effect?.GetDistanceTo(_currentTheme.CurrentValue) ?? float.MaxValue;
        
        // 評価結果を順次表示
        await UIManager.Instance.ShowAnnouncement($"プレイヤーカードのテーマとの距離: {playerDistance:F2}", 2f);
        await UniTask.Delay(300);
        await UIManager.Instance.ShowAnnouncement($"NPCカードのテーマとの距離: {npcDistance:F2}", 2f);
        
        // 結果表示フェーズに移行
        await UniTask.Delay(500);
        ChangeState(GameState.ResultDisplay);
    }
    
    /// <summary>
    /// 勝敗表示フェーズ
    /// </summary>
    private void HandleResultDisplay()
    {
        ResultDisplayAsync().Forget();
    }
    
    /// <summary>
    /// 結果表示処理
    /// </summary>
    private async UniTask ResultDisplayAsync()
    {
        // テーマとの距離で勝敗判定（距離が近い方が勝利）
        var playerDistance = _playerSelectedCard?.CardData.Effect?.GetDistanceTo(_currentTheme.CurrentValue) ?? float.MaxValue;
        var npcDistance = _npcSelectedCard?.CardData.Effect?.GetDistanceTo(_currentTheme.CurrentValue) ?? float.MaxValue;
        
        string result;
        if (playerDistance < npcDistance)
        {
            result = "プレイヤーの勝利!";
        }
        else if (npcDistance < playerDistance)
        {
            result = "NPCの勝利!";
        }
        else
        {
            result = "引き分け!";
        }
        
        // 結果を表示
        await UIManager.Instance.ShowAnnouncement(result, 3f);
        
        // 使用したカードをプレイ
        player?.PlaySelectedCard();
        enemy?.PlaySelectedCard();
        
        // 新しいラウンドの準備時間
        await UniTask.Delay(2000);
        ChangeState(GameState.ThemeAnnouncement);
    }
    
    /// <summary>
    /// 遅延してステート変更
    /// </summary>
    private async UniTask DelayedStateChangeAsync(GameState newState, float delay, CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay((int)(delay * 1000), cancellationToken: cancellationToken);
            ChangeState(newState);
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルされた場合の処理
            UIManager.Instance.ShowAnnouncement($"ステート変更({newState})がキャンセルされました", 1.5f).Forget();
        }
    }
}
