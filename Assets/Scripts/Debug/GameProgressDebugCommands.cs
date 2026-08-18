using System;
using System.Linq;
using Void2610.LiminalPalette;

/// <summary>
/// ゲーム進行度 / セーブデータを LiminalPalette から観測 / 操作するデバッグコマンド群
/// </summary>
public sealed class GameProgressDebugCommands
{
    private readonly GameProgressService _gameProgressService;
    private readonly SaveDataManager _saveDataManager;

    public GameProgressDebugCommands(GameProgressService gameProgressService, SaveDataManager saveDataManager)
    {
        _gameProgressService = gameProgressService;
        _saveDataManager = saveDataManager;
    }

    [LiminalCommand("Progress/CurrentNode", Description = "次に発生するストーリーノード ID を返す (進行度が進むまで変わらない)")]
    public string GetCurrentNode() => _gameProgressService.GetCurrentNode().NodeId;

    [LiminalCommand("Progress/NextSceneType", Description = "次に遷移するシーン種別を返す")]
    public SceneType GetNextSceneType() => _gameProgressService.GetNextSceneType();

    [LiminalCommand("Progress/HasSaveData", Description = "有効なセーブデータが存在するか (ストーリー進行ベース) を返す")]
    public bool HasSaveData() => _gameProgressService.HasSaveData();

    [LiminalCommand("Progress/AcquiredThemes", Description = "獲得済みテーマ ID をカンマ区切りで返す")]
    public string GetAcquiredThemes() => string.Join(",", _gameProgressService.GetAcquiredThemes().Select(t => t.Theme.ThemeId));

    [LiminalCommand("Progress/ViewedCardCount", Description = "閲覧済みカード数を返す")]
    public int GetViewedCardCount() => _gameProgressService.GetViewedCardIds().Count;

    [LiminalCommand("Save/FileExists", Description = "セーブファイルが存在するかを返す")]
    public bool SaveFileExists() => _saveDataManager.SaveFileExists();

    [LiminalCommand("Save/Delete", Description = "セーブファイルを削除して空データを再生成する。メモリ上の進行度は残るため、完全リセットは Progress/Reset を使う")]
    public bool DeleteSaveFile()
    {
        // 失敗を success + "False" として返さず、コマンドの失敗にする
        if (!_saveDataManager.DeleteSaveFile()) throw new InvalidOperationException("セーブファイルの削除に失敗した");
        return true;
    }

    [LiminalCommand("Progress/Reset", Description = "全進行度を初期状態に戻す (デバッグ用)")]
    public string Reset()
    {
        _gameProgressService.ResetToDefaultData();
        return _gameProgressService.GetCurrentNode().NodeId;
    }
}
