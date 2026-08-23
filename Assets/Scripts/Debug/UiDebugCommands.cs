using System;
using System.Collections.Generic;
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

    [LiminalCommand("UI/ClickButton", Description = "GameObject 名が一致するボタンを実際のクリック経路 (レイキャスト) で押す")]
    public string ClickButton(string name)
    {
        var buttons = ActiveButtons().Where(b => b.name == name).ToArray();
        if (buttons.Length == 0) throw new ArgumentException($"押下可能なボタンが見つからない: {name} (UI/ListButtons で確認)");
        if (buttons.Length > 1) throw new ArgumentException($"同名のボタンが {buttons.Length} 個ある: {name}");
        ClickThrough(buttons[0]);
        return name;
    }

    /// <summary>
    /// 画面上のその位置を実際にクリックする。手前を別の要素に覆われていれば例外にする
    /// (onClick を直接呼ぶと「見えているのに押せない」不具合を検証がすり抜けてしまう)
    /// </summary>
    public static void ClickThrough(Button button)
    {
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas ? canvas.rootCanvas.worldCamera : null;
        var screenPoint = camera ? (Vector2)camera.WorldToScreenPoint(button.transform.position) : (Vector2)button.transform.position;
        var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);

        // Game ビューの状態によっては座標が引けない。そのときは押下だけ通す
        if (hits.Count == 0)
        {
            button.onClick.Invoke();
            return;
        }

        var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        if (handler != button.gameObject) throw new InvalidOperationException($"手前の要素がクリックを奪っている: {button.name} は {hits[0].gameObject.name} に覆われている");

        ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
    }

    [LiminalCommand("UI/SelectedObject", Description = "EventSystem が選択中の GameObject 名を返す")]
    public string SelectedObject()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null ? selected.name : "";
    }

    [LiminalCommand("UI/Screenshot", Description = "Game 画面を PNG に保存する (絶対パス)。保存はフレーム末尾に行われる")]
    public string Screenshot(string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        return path;
    }

    private static Button[] ActiveButtons()
    {
        return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(b => b.interactable && b.isActiveAndEnabled).ToArray();
    }
}
