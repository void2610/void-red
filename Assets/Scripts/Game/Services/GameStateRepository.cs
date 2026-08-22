using R3;
using UnityEngine;

/// <summary>
/// ゲーム状態データの保持とI/Oを担当するリポジトリ
/// データの永続化とイベント発行を管理
/// </summary>
public class GameStateRepository
{
    public StoryProgressData StoryProgress { get; } = new();
    public NovelProgressData NovelProgress { get; } = new();
    public Observable<Unit> OnDataSaved => _onDataSaved;

    private readonly SaveDataManager _saveDataManager;

    private readonly Subject<Unit> _onDataSaved = new();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public GameStateRepository(SaveDataManager saveDataManager)
    {
        _saveDataManager = saveDataManager;

        // 起動時に自動ロード
        LoadAll();
    }

    /// <summary>
    /// 有効なセーブデータが存在するかチェック
    /// </summary>
    public bool HasSaveData() => _saveDataManager.SaveFileExists() && (StoryProgress.CurrentStep > 0 || StoryProgress.BattleResults.Count > 0);

    /// <summary>
    /// 全データをセーブ
    /// </summary>
    public void SaveAll()
    {
        var saveData = CreateGameSaveData();
        _saveDataManager.SaveGameData(saveData);
        _onDataSaved.OnNext(Unit.Default);
    }

    /// <summary>
    /// 全データをリセット
    /// </summary>
    public void ResetAll()
    {
        StoryProgress.Reset();
        NovelProgress.Reset();

        SaveAll();

        Debug.Log("[GameStateRepository] 全データを初期状態にリセット完了");
    }

    /// <summary>
    /// 全データをロード
    /// </summary>
    private void LoadAll()
    {
        var loadedData = _saveDataManager.LoadGameData();

        // ストーリー進行データのロード
        StoryProgress.CurrentStep = loadedData.CurrentStep;
        StoryProgress.BattleResults.Clear();
        var loadedResults = loadedData.GetBattleResults();
        foreach (var result in loadedResults)
        {
            StoryProgress.BattleResults[result.Key] = result.Value;
        }

        // ノベル進行データのロード
        NovelProgress.NovelKitState = loadedData.NovelKitState;

        // 新規データかどうかを判定
        var isNewData = StoryProgress.CurrentStep == 0 && StoryProgress.BattleResults.Count == 0;
        var dataType = isNewData ? "新規データ" : "既存データ";

        Debug.Log($"[GameStateRepository] {dataType}自動ロード: Step {StoryProgress.CurrentStep}");
    }

    /// <summary>
    /// 現在の状態からGameSaveDataを作成
    /// </summary>
    private GameSaveData CreateGameSaveData()
    {
        var saveData = new GameSaveData();

        // ストーリー進行データを設定
        saveData.UpdateGameProgress(StoryProgress.CurrentStep, StoryProgress.BattleResults);

        saveData.NovelKitState = NovelProgress.NovelKitState;

        return saveData;
    }
}
