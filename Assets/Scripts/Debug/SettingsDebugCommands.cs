using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Void2610.LiminalPalette;
using Void2610.SettingsSystem;

/// <summary>
/// 設定値と確認ダイアログを LiminalPalette から観測 / 操作するデバッグコマンド群
/// </summary>
public sealed class SettingsDebugCommands
{
    private readonly SettingsManager _settingsManager;
    private readonly IConfirmationDialog _confirmationDialog;

    public SettingsDebugCommands(SettingsManager settingsManager, IConfirmationDialog confirmationDialog)
    {
        _settingsManager = settingsManager;
        _confirmationDialog = confirmationDialog;
    }

    [LiminalCommand("Settings/List", Description = "全設定を key=value の改行区切りで返す (key はそのまま Settings/Get / Set に渡せる)")]
    public string List() => string.Join("\n", _settingsManager.Categories.SelectMany(c => c.Settings).Select(s => $"{s.SettingKey}={s.SerializeValue()}"));

    [LiminalCommand("Settings/Get", Description = "設定キーの現在値 (シリアライズ形式) を返す")]
    public string Get(string key) => Find(key).SerializeValue();

    [LiminalCommand("Dialog/Show", Description = "確認ダイアログを表示して結果 (OK=true) を返す (UI/ClickButton で応答できる)")]
    public async UniTask<bool> DialogShow(string message = "デバッグ確認", string confirmText = "OK", string cancelText = "キャンセル") => await _confirmationDialog.ShowDialog(message, confirmText, cancelText);

    [LiminalCommand("Settings/Set", Description = "設定キーに値 (シリアライズ形式) を書き込み適用する。Settings/Get の出力と同じ形式で渡す")]
    public string Set(string key, string value)
    {
        var setting = Find(key);
        setting.DeserializeValue(value);
        setting.ApplyCurrentValue();
        return setting.SerializeValue();
    }

    [LiminalCommand("Settings/ResetAll", Description = "全設定を初期値に戻す")]
    public string ResetAll()
    {
        _settingsManager.ResetAllSettings();
        return "reset";
    }

    [LiminalCommand("Dialog/IsShowing", Description = "確認ダイアログが表示中かを返す")]
    public bool DialogIsShowing()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<ConfirmationDialogView>(FindObjectsInactive.Include);
        return view != null && view.IsShowing;
    }

    private ISettingBase Find(string key)
    {
        return _settingsManager.Categories.SelectMany(c => c.Settings).FirstOrDefault(s => s.SettingKey == key) ?? throw new ArgumentException($"設定キーが存在しない: {key} (Settings/List で確認)");
    }
}
