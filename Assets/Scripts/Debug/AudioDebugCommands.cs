using System.Linq;
using Void2610.LiminalPalette;
using Void2610.UnityTemplate;

/// <summary>
/// BGM / SE を LiminalPalette から観測 / 操作するデバッグコマンド群
/// </summary>
public sealed class AudioDebugCommands
{
    [LiminalCommand("Audio/CurrentBgm", Description = "再生中の BGM 名を返す (未再生は空文字)")]
    public string CurrentBgm() => BgmManager.Instance.CurrentBgmName ?? "";

    [LiminalCommand("Audio/IsBgmPlaying", Description = "BGM が再生中かを返す")]
    public bool IsBgmPlaying() => BgmManager.Instance.IsPlaying;

    [LiminalCommand("Audio/ListBgm", Description = "登録済み BGM 名をカンマ区切りで返す")]
    public string ListBgm() => string.Join(",", BgmManager.Instance.EnumerateBgmEntries().Select(e => e.Item1));

    [LiminalCommand("Audio/ListSe", Description = "登録済み SE 名をカンマ区切りで返す")]
    public string ListSe() => string.Join(",", SeManager.Instance.EnumerateSeEntries().Select(e => e.Item1));

    [LiminalCommand("Audio/PlaySe", Description = "指定 SE を鳴らす")]
    public int PlaySe(string name) => SeManager.Instance.PlaySe(name);

    [LiminalCommand("Audio/BgmVolume", Description = "BGM 音量 (0-1) を返す")]
    public float BgmVolume() => BgmManager.Instance.BgmVolume;

    [LiminalCommand("Audio/SeVolume", Description = "SE 音量 (0-1) を返す")]
    public float SeVolume() => SeManager.Instance.SeVolume;

    [LiminalCommand("Audio/PlayBgm", Description = "指定 BGM を再生する")]
    public string PlayBgm(string name)
    {
        BgmManager.Instance.PlayBGM(name);
        return BgmManager.Instance.CurrentBgmName ?? "";
    }

    [LiminalCommand("Audio/StopBgm", Description = "BGM を停止する")]
    public async Cysharp.Threading.Tasks.UniTask<string> StopBgm()
    {
        await BgmManager.Instance.Stop();
        return "stopped";
    }
}
