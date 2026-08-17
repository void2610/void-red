#if UNITY_EDITOR || DEVELOPMENT_BUILD || LIMINAL_PALETTE_FORCE_ENABLE
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

    [LiminalCommand("Progress/CurrentNode", Description = "現在のストーリーノード ID を返す")]
    public string GetCurrentNode() => _gameProgressService.GetCurrentNode().NodeId;

    [LiminalCommand("Progress/NextNode", Description = "次に発生するストーリーノード ID を返す")]
    public string GetNextNode() => _gameProgressService.GetNextNode().NodeId;

    [LiminalCommand("Progress/NextSceneType", Description = "次に遷移するシーン種別を返す")]
    public string GetNextSceneType() => _gameProgressService.GetNextSceneType().ToString();

    [LiminalCommand("Progress/HasSaveData", Description = "有効なセーブデータが存在するか (ストーリー進行ベース) を返す")]
    public string HasSaveData() => _gameProgressService.HasSaveData().ToString();

    [LiminalCommand("Progress/AcquiredThemes", Description = "獲得済みテーマ ID をカンマ区切りで返す")]
    public string GetAcquiredThemes() => string.Join(",", _gameProgressService.GetAcquiredThemes().Select(t => t.Theme.ThemeId));

    [LiminalCommand("Progress/ViewedCardCount", Description = "閲覧済みカード数を返す")]
    public string GetViewedCardCount() => _gameProgressService.GetViewedCardIds().Count.ToString();

    [LiminalCommand("Save/FileExists", Description = "セーブファイルが存在するかを返す")]
    public string SaveFileExists() => _saveDataManager.SaveFileExists().ToString();

    [LiminalCommand("Save/Delete", Description = "セーブファイルを削除する")]
    public string DeleteSaveFile() => _saveDataManager.DeleteSaveFile().ToString();

    [LiminalCommand("Progress/Reset", Description = "全進行度を初期状態に戻す (デバッグ用)")]
    public string Reset()
    {
        _gameProgressService.ResetToDefaultData();
        return _gameProgressService.GetCurrentNode().NodeId;
    }
}
#endif
