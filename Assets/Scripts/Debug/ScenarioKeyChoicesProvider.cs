using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Void2610.LiminalPalette;

/// <summary>
/// Resources/Scenarios 配下のシナリオキーを LiminalPalette の入力補完に出す
/// </summary>
public sealed class ScenarioKeyChoicesProvider : IChoicesProvider
{
    public const string RESOURCES_ROOT = "Scenarios";

    // novel-kit が .rb から生成する .mrb サブアセットは再生キーではないので除く
    public static IReadOnlyList<string> ListKeys() => Resources.LoadAll<TextAsset>(RESOURCES_ROOT).Select(t => t.name).Where(n => !n.Contains('.')).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<ChoiceItem> GetChoices() => ListKeys().Select(k => new ChoiceItem(k)).ToArray();
}
