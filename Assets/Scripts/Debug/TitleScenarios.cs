using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// タイトル画面のボタン導線 (実クリック経路) を検証する LiminalScenario
/// </summary>
public static class TitleScenarios
{
    [LiminalScenario(
        "Title/Scenario/StartWithoutSaveEntersFirstNovel",
        Scene = "TitleScene",
        Description = "セーブ無しで「はじめから」→ 確認なしで最初のノベル (prologue1) へ遷移する")]
    public static IEnumerable<ScenarioStep> StartWithoutSaveEntersFirstNovel()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "StartButton"));
        yield return ScenarioStep.AssertCommandReturns("Dialog/IsShowing", null, "False", "セーブ無しでは確認ダイアログを出さない");
        foreach (var step in WaitScene("NovelKitScene")) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Novel/IsActive", null, "True", 10f, "ノベル構築待ち");
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "prologue1");
    }

    [LiminalScenario(
        "Title/Scenario/StartWithSaveAsksConfirmationAndResets",
        Scene = "TitleScene",
        Description = "進行済みセーブがある状態で「はじめから」→ 確認ダイアログ → OK で進行度がリセットされ最初のノベルへ")]
    public static IEnumerable<ScenarioStep> StartWithSaveAsksConfirmationAndResets()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/AdvanceAsNovel", null, "alv");
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "StartButton"));
        yield return ScenarioStep.AssertCommandEventually("Dialog/IsShowing", null, "True", 5f, "上書き確認ダイアログ待ち");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        foreach (var step in WaitScene("NovelKitScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "prologue1");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");
    }

    [LiminalScenario(
        "Title/Scenario/ContinueGoesHome",
        Scene = "TitleScene",
        Description = "進行済みセーブがある状態で「つづきから」→ ホームへ遷移し進行度は保たれる")]
    public static IEnumerable<ScenarioStep> ContinueGoesHome()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/AdvanceAsNovel", null, "alv");
        yield return WaitFadeDone();

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ContinueButton"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "alv");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "1");
    }
}
