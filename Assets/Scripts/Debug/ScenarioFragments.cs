using System.Collections.Generic;
using Void2610.LiminalPalette;

/// <summary>
/// LiminalScenario 群で共有する組み立て部品
/// </summary>
internal static class ScenarioFragments
{
    public static Dictionary<string, object> Args(string key, object value) => new() { [key] = value };

    public static Dictionary<string, object> Args(string key1, object value1, string key2, object value2) => new() { [key1] = value1, [key2] = value2 };

    /// <summary>フェード遷移の完了を待つ (遷移中に次の操作を打つと無言で no-op になるため)</summary>
    public static ScenarioStep WaitFadeDone(float timeoutSeconds = 15f) => ScenarioStep.AssertCommandEventually("Scene/IsFading", null, "False", timeoutSeconds, "フェード完了待ち");

    /// <summary>進行度とセーブ済みノベル状態を初期化し、決定的な開始状態を作る定型前文</summary>
    public static IEnumerable<ScenarioStep> ResetProgress()
    {
        yield return ScenarioStep.Run("Progress/Reset");
        yield return ScenarioStep.Run("Novel/ClearSavedState");
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "prologue1", "初期ノード確認");
    }

    /// <summary>指定シーンへ到達しフェードが明けるまで待つ</summary>
    public static IEnumerable<ScenarioStep> WaitScene(string sceneName, float timeoutSeconds = 20f)
    {
        yield return ScenarioStep.AssertCommandEventually("Scene/Current", null, sceneName, timeoutSeconds, $"{sceneName} 到達待ち");
        yield return WaitFadeDone(timeoutSeconds);
    }
}
