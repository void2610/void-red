using Void2610.LiminalPalette;
using Void2610.UnityTemplate;
using Void2610.UnityTemplate.Steam;

/// <summary>
/// 展示モードと Steam 連携を LiminalPalette から観測 / 操作するデバッグコマンド群
/// </summary>
public sealed class ExhibitDebugCommands
{
    private readonly ExhibitSettings _exhibitSettings;
    private readonly IdleDetector _idleDetector;
    private readonly ExhibitSessionTimerService _sessionTimer;
    private readonly SteamService _steamService;

    public ExhibitDebugCommands(ExhibitSettings exhibitSettings, IdleDetector idleDetector,
        ExhibitSessionTimerService sessionTimer, SteamService steamService)
    {
        _exhibitSettings = exhibitSettings;
        _idleDetector = idleDetector;
        _sessionTimer = sessionTimer;
        _steamService = steamService;
    }

    [LiminalCommand("Exhibit/Enabled", Description = "展示モードが有効かを返す")]
    public bool Enabled() => _exhibitSettings.EnableExhibitMode;

    [LiminalCommand("Exhibit/IdleSeconds", Description = "最後の入力からの経過秒数を返す")]
    public float IdleSeconds() => _idleDetector.IdleSeconds;

    [LiminalCommand("Steam/IsInitialized", Description = "Steam API が初期化済みかを返す")]
    public bool SteamIsInitialized() => _steamService.IsInitialized;

    [LiminalCommand("Steam/ResetAllStats", Description = "Steam の統計と実績をすべてリセットする")]
    public bool SteamResetAllStats() => _steamService.ResetAllStats();

    [LiminalCommand("Exhibit/ResetIdle", Description = "無操作タイマーをリセットする")]
    public string ResetIdle()
    {
        _idleDetector.ResetIdleTimer();
        return "reset";
    }

    [LiminalCommand("Exhibit/ResetSession", Description = "セッション制限タイマーをリセットする")]
    public string ResetSession()
    {
        _sessionTimer.ResetSession();
        return "reset";
    }

    [LiminalCommand("Steam/GetStat", Description = "整数 stat の値を返す (取得失敗は例外)")]
    public int SteamGetStat(string statName)
    {
        if (!_steamService.GetStat(statName, out int value)) throw new System.InvalidOperationException($"stat を取得できない: {statName}");
        return value;
    }
}
