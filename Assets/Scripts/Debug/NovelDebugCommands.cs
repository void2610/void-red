using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Void2610.LiminalPalette;

/// <summary>
/// ノベルパートを LiminalPalette から観測 / 操作するデバッグコマンド群
/// ノベルの View / Runner は NovelKitScene ローカルの LifetimeScope に登録されるため、
/// 常駐する DebugLifetimeScope からは呼び出し時にシーン側スコープを引いて解決する
/// </summary>
public sealed class NovelDebugCommands
{
    private readonly GameProgressService _gameProgressService;

    public NovelDebugCommands(GameProgressService gameProgressService)
    {
        _gameProgressService = gameProgressService;
    }

    // --- 再生中の観測 ---

    [LiminalCommand("Novel/IsActive", Description = "ノベルシーンが構築済みか (NovelKitLifetimeScope が存在するか) を返す")]
    public bool IsActive() => LifetimeScope.Find<NovelKitLifetimeScope>() is { Container: not null };

    private static NovelKitMessageView View => FindScope().Container.Resolve<NovelKitMessageView>();
    private static INovelScenarioRunner Runner => FindScope().Container.Resolve<INovelScenarioRunner>();

    [LiminalCommand("Novel/SayNumber", Description = "再生中シナリオの現在の Say 番号 (再生ごとに 0 起点) を返す")]
    public int SayNumber() => Runner.CurrentSayNumber;

    [LiminalCommand("Novel/Speaker", Description = "表示中の話者名を返す")]
    public string Speaker() => View.CurrentSpeaker;

    [LiminalCommand("Novel/Message", Description = "表示中の本文 (TMP タグ込み) を返す")]
    public string Message() => View.CurrentMessage;

    [LiminalCommand("Novel/IsWaitingForAdvance", Description = "全文表示済みで送り待ちか (次行へ進める状態か) を返す")]
    public bool IsWaitingForAdvance() => View.IsWaitingForAdvance;

    [LiminalCommand("Novel/IsAutoMode", Description = "オート再生中かを返す")]
    public bool IsAutoMode() => View.IsAutoMode;

    [LiminalCommand("Novel/IsSkipping", Description = "スキップ中かを返す")]
    public bool IsSkipping() => View.IsSkipping;

    [LiminalCommand("Novel/ChoiceCount", Description = "表示中の選択肢数を返す (0 なら選択待ちではない)")]
    public int ChoiceCount() => View.ChoiceCount;

    // --- 永続化されたノベル状態 (フラグ / 既読) ---

    [LiminalCommand("Novel/SavedState", Description = "セーブ済みの novel-kit 状態 (フラグ / 既読) の JSON を返す")]
    public string SavedState() => _gameProgressService.GetNovelKitState() ?? "";

    // --- 再生中の操作 (実入力と同じ View 経路) ---

    [LiminalCommand("Novel/Advance", Description = "セリフを送る (文字送り中なら全文表示、送り待ちなら次行へ)")]
    public string Advance()
    {
        View.Advance();
        return View.CurrentMessage;
    }

    [LiminalCommand("Novel/AdvanceToNextLine", Description = "次の行が送り待ちになるまで送り、その話者を返す (行を追い越さない)")]
    public async UniTask<string> AdvanceToNextLine(int timeoutSeconds = 10)
    {
        var view = View;
        // 送り待ちでなければまず全文表示させ、待ちに入るのを待つ
        if (!view.IsWaitingForAdvance)
        {
            view.Advance();
            await WaitUntil(() => view.IsWaitingForAdvance || view.ChoiceCount > 0, timeoutSeconds);
            return view.CurrentSpeaker;
        }
        var before = view.CurrentMessage;
        view.Advance();
        // 次行の表示が始まり (本文が変わり)、かつ送り待ち or 選択肢待ちになるまで待つ
        await WaitUntil(() => (view.CurrentMessage != before && view.IsWaitingForAdvance) || view.ChoiceCount > 0 || !IsActive(), timeoutSeconds);
        return IsActive() ? view.CurrentSpeaker : "(scene ended)";
    }

    [LiminalCommand("Novel/Choose", Description = "表示中の選択肢を index (0 始まり) で選ぶ")]
    public bool Choose(int index)
    {
        if (!View.SelectChoice(index)) throw new ArgumentOutOfRangeException(nameof(index), $"選択肢 {index} は存在しない (count={View.ChoiceCount})");
        return true;
    }

    [LiminalCommand("Novel/ToggleAuto", Description = "オート再生を切り替え、切替後の状態を返す")]
    public bool ToggleAuto()
    {
        View.ToggleAutoMode();
        return View.IsAutoMode;
    }

    [LiminalCommand("Novel/BeginSkip", Description = "確認ダイアログを経由せず即スキップを開始する (次の選択肢まで飛ばす)")]
    public string BeginSkip()
    {
        View.BeginSkip();
        return "skipping";
    }

    [LiminalCommand("Novel/Flag", Description = "セーブ済みのシナリオフラグの整数値を返す (未設定は 0)")]
    public int Flag(string key)
    {
        NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot);
        return snapshot.Values != null && snapshot.Values.TryGetValue(key, out var v) ? v : 0;
    }

    [LiminalCommand("Novel/Flags", Description = "セーブ済みのシナリオフラグを key=value のカンマ区切りで返す")]
    public string Flags()
    {
        NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot);
        return snapshot.Values == null ? "" : string.Join(",", snapshot.Values.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    [LiminalCommand("Novel/ReadCount", Description = "セーブ済みの既読テキスト数を返す")]
    public int ReadCount()
    {
        NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot);
        return snapshot.ReadTextIds?.Count ?? 0;
    }

    [LiminalCommand("Novel/SetFlag", Description = "セーブ済みのシナリオフラグを書き換えて保存する (次回のノベル再生から反映)")]
    public int SetFlag(string key, int value)
    {
        NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot);
        var values = snapshot.Values?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new();
        values[key] = value;
        var next = new NovelStateSnapshot(values, snapshot.ReadTextIds ?? Array.Empty<string>());
        _gameProgressService.SaveNovelKitState(NovelSaveSerializer.Serialize(next));
        return value;
    }

    [LiminalCommand("Novel/ClearSavedState", Description = "セーブ済みの novel-kit 状態 (フラグ / 既読) を空にして保存する")]
    public string ClearSavedState()
    {
        _gameProgressService.SaveNovelKitState(NovelSaveSerializer.Serialize(NovelSaveSerializer.Empty));
        return "cleared";
    }

    private static NovelKitLifetimeScope FindScope()
    {
        var scope = LifetimeScope.Find<NovelKitLifetimeScope>() as NovelKitLifetimeScope;
        if (scope == null || scope.Container == null) throw new InvalidOperationException("NovelKitScene が読み込まれていない");
        return scope;
    }

    private static async UniTask WaitUntil(Func<bool> predicate, int timeoutSeconds)
    {
        var deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException($"{timeoutSeconds} 秒以内に条件を満たさなかった");
            await UniTask.Yield();
        }
    }
}
