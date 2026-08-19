using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// ノベルパートの操作 / 分岐 / スキップ導線を検証する LiminalScenario
/// </summary>
public static class NovelScenarios
{
    [LiminalScenario(
        "Novel/Scenario/PlayScenarioChooseAndReturnHome",
        Scene = "HomeScene",
        Description = "任意シナリオ (prologue2) を直接再生 → 選択肢まで飛ばして 2 択目を選ぶ → 完走でホームへ戻り、進行度は変わらずフラグだけ保存される")]
    public static IEnumerable<ScenarioStep> PlayScenarioChooseAndReturnHome()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("Novel/PlayScenario", Args("scenarioKey", "prologue2"));
        yield return ScenarioStep.AssertCommandReturns("Novel/IsActive", null, "True");

        // 選択肢まで飛ばす (スキップは選択肢で止まる)
        yield return ScenarioStep.Run("Novel/BeginSkip");
        yield return ScenarioStep.AssertCommandEventually("Novel/ChoiceCount", null, "2", 20f, "選択肢表示待ち");
        yield return ScenarioStep.Run("Novel/Choose", Args("index", 1));
        yield return ScenarioStep.AssertCommandEventually("Novel/ChoiceCount", null, "0", 5f, "選択肢が閉じるのを待つ");

        // 残りを飛ばして完走 → ホームへ戻る
        yield return ScenarioStep.Run("Novel/BeginSkip");
        foreach (var step in WaitScene("HomeScene", 40f)) yield return step;

        // 進行度は動かず、選んだ札のフラグだけが保存されている
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "prologue1");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");
        yield return ScenarioStep.AssertCommandReturns("Novel/Flag", Args("key", "prologue_fate_card"), "1");
    }

    [LiminalScenario(
        "Novel/Scenario/AdvanceToNextLineFollowsScript",
        Scene = "HomeScene",
        Description = "prologue1 を直接再生し、行送りコマンドが台本どおりに 1 行ずつ進む (追い越さない)")]
    public static IEnumerable<ScenarioStep> AdvanceToNextLineFollowsScript()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("Novel/PlayScenario", Args("scenarioKey", "prologue1"));
        // 1 行目 (ナレーション) が送り待ちになるまで送る
        yield return ScenarioStep.Run("Novel/AdvanceToNextLine");
        yield return ScenarioStep.AssertCommandReturns("Novel/Message", null, "<noparse>(エレベーターの音)</noparse>");
        yield return ScenarioStep.Run("Novel/AdvanceToNextLine");
        yield return ScenarioStep.AssertCommandReturns("Novel/Message", null, "<noparse>(エレベーターの扉が開く)</noparse>");
        yield return ScenarioStep.Run("Novel/AdvanceToNextLine");
        yield return ScenarioStep.AssertCommandReturns("Novel/Message", null, "<noparse>………。ん……。</noparse>");
        yield return ScenarioStep.AssertCommandReturns("Novel/SayNumber", null, "3");
    }

    [LiminalScenario(
        "Novel/Scenario/SkipButtonAsksConfirmation",
        Scene = "NovelKitScene",
        Description = "スキップボタン → 確認ダイアログ → キャンセルでは飛ばず、OK でスキップが始まる")]
    public static IEnumerable<ScenarioStep> SkipButtonAsksConfirmation()
    {
        yield return ScenarioStep.AssertCommandEventually("Novel/IsActive", null, "True", 10f, "ノベルシーン構築待ち");
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "SkipButton"));
        yield return ScenarioStep.AssertCommandEventually("Dialog/IsShowing", null, "True", 5f, "確認ダイアログ表示待ち");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "CancelButton"));
        yield return ScenarioStep.AssertCommandEventually("Dialog/IsShowing", null, "False", 5f, "ダイアログが閉じるのを待つ");
        yield return ScenarioStep.AssertCommandReturns("Novel/IsSkipping", null, "False", "キャンセルではスキップしない");
        // ダイアログが閉じた同フレームの末尾で「確認中」ガードが解除されるため、次の要求はフレームを跨いでから出す
        yield return ScenarioStep.WaitFrames(2, "確認処理の完了待ち");

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "SkipButton"));
        yield return ScenarioStep.AssertCommandEventually("Dialog/IsShowing", null, "True", 5f, "確認ダイアログ再表示待ち");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        yield return ScenarioStep.AssertCommandEventually("Novel/IsSkipping", null, "True", 5f, "OK でスキップ開始");
    }
}
