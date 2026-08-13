using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

/// <summary>
/// ゲーム全体の進行度を管理するサービス（ファサード）
/// GameStateRepositoryに委譲し、ビジネスロジックを提供
/// </summary>
public class GameProgressService
{
    /// <summary>
    /// データセーブ時のイベント
    /// </summary>
    public Observable<Unit> OnDataSaved => _repository.OnDataSaved;

    private readonly GameStateRepository _repository;

    public GameProgressService(SaveDataManager saveDataManager, CardPoolService cardPoolService, AllThemeData allThemeData)
    {
        _repository = new GameStateRepository(saveDataManager, cardPoolService, allThemeData);

        // 現在のノードを初期化
        _repository.StoryProgress.CurrentNode = GetNextNode();
    }

    /// <summary>
    /// 有効なセーブデータが存在するかチェック（ストーリー進行ベース）
    /// </summary>
    public bool HasSaveData() => _repository.HasSaveData();

    /// <summary>
    /// 現在のストーリーノードを取得
    /// </summary>
    public StoryNode GetCurrentNode() => _repository.StoryProgress.CurrentNode;

    public SceneType GetNextSceneType() => GetSceneTypeForNode(GetNextNode());

    /// <summary>
    /// カード閲覧をリストで記録
    /// </summary>
    public void RecordCardViews(List<CardData> cardDataList) => cardDataList.ForEach(RecordCardView);

    /// <summary>
    /// 閲覧済みカードIDリストを取得
    /// </summary>
    public HashSet<string> GetViewedCardIds() => new HashSet<string>(_repository.PlayerProgress.ViewedCardIds);

    /// <summary>
    /// novel-kit のフラグ / 既読スナップショットを取得
    /// </summary>
    public string GetNovelKitState() => _repository.NovelProgress.NovelKitState;

    /// <summary>
    /// 獲得済みテーマリストを取得
    /// </summary>
    public IReadOnlyList<AcquiredTheme> GetAcquiredThemes() => _repository.MemoryProgress.AcquiredThemes;

    /// <summary>
    /// novel-kit のフラグ / 既読スナップショットを記録してセーブ
    /// </summary>
    public void SaveNovelKitState(string state)
    {
        _repository.NovelProgress.NovelKitState = state;
        _repository.SaveAll();
    }

    /// <summary>
    /// 全データを初期状態にリセット（デバッグ用）
    /// </summary>
    public void ResetToDefaultData()
    {
        _repository.ResetAll();
        _repository.StoryProgress.CurrentNode = GetNextNode();
    }

    /// <summary>
    /// 次に発生するストーリーノードを決定（結果辞書による分岐対応）
    /// </summary>
    public StoryNode GetNextNode()
    {
        StoryNode nextNode;
        var currentStep = _repository.StoryProgress.CurrentStep;

        switch (currentStep)
        {
            // プロローグ1 - 次のバトルへ直接遷移
            case 0:
                nextNode = new NovelNode("prologue1", false);
                break;
            // アルヴ - バトル後は次のノベルへ直接遷移
            case 1:
                nextNode = new BattleNode("alv", false);
                break;
            // プロローグ2 - ノベル後はホームに戻る
            case 2:
                nextNode = new NovelNode("prologue2");
                break;
            // セリカ1 - セリカと出会う
            case 3:
                nextNode = new NovelNode("cerica1", false);
                break;
            // セリカ2 - 商品提示
            case 4:
                nextNode = new NovelNode("cerica2", false);
                break;
            // セリカバトル
            case 5:
                nextNode = new DemoEnding();
                // nextNode = new BattleNode("cerica", false);
                break;
            // この先は未定
            default:
                nextNode = new DemoEnding();
                break;
        }

        return nextNode;
    }

    /// <summary>
    /// 現在のバトル結果を記録
    /// </summary>
    public void RecordBattleResultAndSave(bool isPlayerWin)
    {
        var nodeId = _repository.StoryProgress.CurrentNode.NodeId;
        _repository.StoryProgress.RecordBattleResult(nodeId, isPlayerWin);
        _repository.StoryProgress.AdvanceStep();
        _repository.StoryProgress.CurrentNode = GetNextNode();
        _repository.SaveAll();
    }

    /// <summary>
    /// ノベル完了（複数選択記録 + 進行 + セーブを統合）
    /// </summary>
    public void RecordNovelResultAndSave()
    {
        _repository.StoryProgress.AdvanceStep();
        _repository.StoryProgress.CurrentNode = GetNextNode();
        _repository.SaveAll();
    }

    /// <summary>
    /// カード閲覧を記録
    /// </summary>
    public void RecordCardView(CardData cardData)
    {
        if (!cardData || string.IsNullOrEmpty(cardData.CardId)) return;
        _repository.PlayerProgress.RecordCardView(cardData.CardId);
    }

    /// <summary>
    /// 獲得テーマをメモリに記録（ディスク保存は行わない）
    /// </summary>
    /// <param name="theme">獲得したテーマ</param>
    public void RecordAcquiredTheme(AcquiredTheme theme)
    {
        _repository.MemoryProgress.AddAcquiredTheme(theme);
        Debug.Log($"[GameProgressService] 獲得テーマを記録: {theme.ThemeName} ({theme.AcquiredCards.Count}枚, 感情: {theme.DominantEmotionResult})");
    }

    /// <summary>
    /// StoryNodeから対応するSceneTypeを取得
    /// </summary>
    private SceneType GetSceneTypeForNode(StoryNode node)
    {
        return node switch
        {
            BattleNode => SceneType.Battle,
            NovelNode => SceneType.Novel,
            DemoEnding => SceneType.Thanks,
            _ => SceneType.Home
        };
    }
}
