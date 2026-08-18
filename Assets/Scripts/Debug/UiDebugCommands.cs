using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Void2610.LiminalPalette;

/// <summary>
/// uGUI のボタンを LiminalPalette から列挙 / 押下するデバッグコマンド群
/// Presenter はボタンの Observable を購読して動くため、onClick を発火させれば実操作と同じ経路を通る
/// </summary>
public sealed class UiDebugCommands
{
    [LiminalCommand("UI/ListButtons", Description = "押下可能なボタンの GameObject 名をカンマ区切りで返す (同名は重複して並ぶ)")]
    public string ListButtons() => string.Join(",", ActiveButtons().Select(b => b.name));

    [LiminalCommand("UI/ClickButton", Description = "GameObject 名が一致する押下可能なボタンの onClick を発火する")]
    public string ClickButton(string name)
    {
        var buttons = ActiveButtons().Where(b => b.name == name).ToArray();
        if (buttons.Length == 0) throw new ArgumentException($"押下可能なボタンが見つからない: {name} (UI/ListButtons で確認)");
        if (buttons.Length > 1) throw new ArgumentException($"同名のボタンが {buttons.Length} 個ある: {name}");
        buttons[0].onClick.Invoke();
        return name;
    }

    [LiminalCommand("UI/SelectedObject", Description = "EventSystem が選択中の GameObject 名を返す")]
    public string SelectedObject()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null ? selected.name : "";
    }
    private static Button[] ActiveButtons()
    {
        return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(b => b.interactable && b.isActiveAndEnabled).ToArray();
    }
}
