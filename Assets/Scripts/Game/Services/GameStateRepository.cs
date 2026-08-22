using System.Collections.Generic;
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
    public EmotionWallet PlayerWallet { get; } = new();
    public PersonaState Persona { get; } = new();
    public HashSet<string> CollapsedParticipantIds { get; } = new();
    public Observable<Unit> OnDataSaved => _onDataSaved;

    private readonly SaveDataManager _saveDataManager;

    private readonly Subject<Unit> _onDataSaved = new();

    public GameStateRepository(SaveDataManager saveDataManager)
    {
        _saveDataManager = saveDataManager;

        // 起動時に自動ロード
        LoadAll();
    }

    public bool HasSaveData() => _saveDataManager.SaveFileExists() && StoryProgress.CurrentStep > 0;

    public void SaveAll()
    {
        var saveData = CreateGameSaveData();
        _saveDataManager.SaveGameData(saveData);
        _onDataSaved.OnNext(Unit.Default);
    }

    public void ResetAll()
    {
        StoryProgress.Reset();
        NovelProgress.Reset();
        PlayerWallet.LoadCounts(new int[EmotionWallet.ALL_EMOTIONS.Length]);
        Persona.Reset();
        CollapsedParticipantIds.Clear();

        SaveAll();

        Debug.Log("[GameStateRepository] 全データを初期状態にリセット完了");
    }

    private void LoadAll()
    {
        var loadedData = _saveDataManager.LoadGameData();

        StoryProgress.CurrentStep = loadedData.CurrentStep;
        NovelProgress.NovelKitState = loadedData.NovelKitState;
        PlayerWallet.LoadCounts(loadedData.EmotionCounts);
        Persona.Load(loadedData.EmotionState, loadedData.IntegratedLotIds, loadedData.CollectionLotIds, loadedData.TotalDistortion);
        CollapsedParticipantIds.Clear();
        CollapsedParticipantIds.UnionWith(loadedData.CollapsedParticipantIds);

        var dataType = StoryProgress.CurrentStep == 0 ? "新規データ" : "既存データ";
        Debug.Log($"[GameStateRepository] {dataType}自動ロード: Step {StoryProgress.CurrentStep}");
    }

    private GameSaveData CreateGameSaveData()
    {
        var saveData = new GameSaveData { CurrentStep = StoryProgress.CurrentStep, NovelKitState = NovelProgress.NovelKitState };
        saveData.EmotionCounts.AddRange(PlayerWallet.ToCounts());
        saveData.EmotionState = Persona.EmotionState.HasValue ? (int)Persona.EmotionState.Value : -1;
        saveData.IntegratedLotIds.AddRange(Persona.IntegratedLotIds);
        saveData.CollectionLotIds.AddRange(Persona.CollectionLotIds);
        saveData.TotalDistortion = Persona.TotalDistortion;
        saveData.CollapsedParticipantIds.AddRange(CollapsedParticipantIds);
        return saveData;
    }
}
