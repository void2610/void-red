using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

/// <summary>
/// ゲーム状態データの保持とI/Oを担当するリポジトリ
/// データの永続化とイベント発行を管理
/// </summary>
public class GameStateRepository
{
    public StoryProgressData StoryProgress { get; } = new();
    public PlayerProgressData PlayerProgress { get; } = new();
    public NovelProgressData NovelProgress { get; } = new();
    public MemoryProgressData MemoryProgress { get; } = new();
    public Observable<Unit> OnDataSaved => _onDataSaved;

    // 依存サービス
    private readonly SaveDataManager _saveDataManager;
    private readonly CardPoolService _cardPoolService;
    private readonly AllThemeData _allThemeData;

    private readonly Subject<Unit> _onDataSaved = new();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public GameStateRepository(SaveDataManager saveDataManager, CardPoolService cardPoolService, AllThemeData allThemeData)
    {
        _saveDataManager = saveDataManager;
        _cardPoolService = cardPoolService;
        _allThemeData = allThemeData;

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
        PlayerProgress.Reset();
        NovelProgress.Reset();
        MemoryProgress.Reset();

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

        // プレイヤー進行データのロード
        PlayerProgress.ViewedCardIds.Clear();
        var viewedIds = loadedData.GetViewedCardIds();
        foreach (var id in viewedIds)
        {
            PlayerProgress.ViewedCardIds.Add(id);
        }

        // ノベル進行データのロード
        NovelProgress.LoadFrom(loadedData.GetAllChoiceResults());
        NovelProgress.NovelKitState = loadedData.NovelKitState;

        // 獲得テーマデータのロード
        MemoryProgress.Reset();
        foreach (var savedTheme in loadedData.AcquiredThemes)
        {
            var acquiredTheme = ConvertSavedThemeToAcquiredTheme(savedTheme);
            if (acquiredTheme != null)
            {
                MemoryProgress.AddAcquiredTheme(acquiredTheme);
            }
        }

        // 新規データかどうかを判定
        var isNewData = StoryProgress.CurrentStep == 0 && StoryProgress.BattleResults.Count == 0;
        var dataType = isNewData ? "新規データ" : "既存データ";

        Debug.Log($"[GameStateRepository] {dataType}自動ロード: Step {StoryProgress.CurrentStep}, Themes {MemoryProgress.AcquiredThemes.Count}");
    }

    /// <summary>
    /// 現在の状態からGameSaveDataを作成
    /// </summary>
    private GameSaveData CreateGameSaveData()
    {
        var saveData = new GameSaveData();

        // ストーリー進行データを設定
        saveData.UpdateGameProgress(StoryProgress.CurrentStep, StoryProgress.BattleResults);

        // 閲覧済みカードをセーブデータに追加
        foreach (var cardId in PlayerProgress.ViewedCardIds)
        {
            saveData.RecordCardView(cardId);
        }

        // ノベル選択結果をセーブデータに追加
        foreach (var choiceResult in NovelProgress.GetAllChoiceResults())
        {
            saveData.AddNovelChoiceResult(choiceResult);
        }
        saveData.NovelKitState = NovelProgress.NovelKitState;

        // 獲得テーマをセーブデータに追加
        var savedThemes = MemoryProgress.AcquiredThemes.Select(theme => theme.ToSavedData());
        saveData.UpdateAcquiredThemes(savedThemes);

        return saveData;
    }

    /// <summary>
    /// 保存されたテーマデータをAcquiredThemeに変換
    /// </summary>
    /// <param name="savedTheme">保存されたテーマデータ</param>
    /// <returns>変換されたAcquiredTheme、変換失敗時はnull</returns>
    private AcquiredTheme ConvertSavedThemeToAcquiredTheme(SavedAcquiredTheme savedTheme)
    {
        // テーマデータを取得
        var themeData = _allThemeData.GetThemeById(savedTheme.ThemeId);
        if (!themeData)
        {
            Debug.LogWarning($"[GameStateRepository] テーマID '{savedTheme.ThemeId}' が見つかりませんでした");
            return null;
        }

        // カード獲得情報を復元
        var allCardInfoList = new List<CardAcquisitionInfo>();
        foreach (var savedCardInfo in savedTheme.CardInfoList)
        {
            var cardData = _cardPoolService.GetCardById(savedCardInfo.CardId);
            if (cardData)
            {
                // セーブデータにはinstanceIdがないので新規生成
                var cardModel = new CardModel(cardData);
                var cardInfo = new CardAcquisitionInfo(
                    cardModel,
                    savedCardInfo.GetPlayerBidsByEmotion(),
                    savedCardInfo.GetEnemyBidsByEmotion(),
                    savedCardInfo.PlayerWon
                );
                allCardInfoList.Add(cardInfo);
            }
            else
            {
                Debug.LogWarning($"[GameStateRepository] カードID '{savedCardInfo.CardId}' が見つかりませんでした");
            }
        }

        return new AcquiredTheme(
            themeData,
            allCardInfoList,
            savedTheme.GetUsedEmotions()
        );
    }
}
