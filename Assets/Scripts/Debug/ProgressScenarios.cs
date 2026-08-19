using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// ストーリー進行度とセーブの整合を検証する LiminalScenario
/// </summary>
public static class ProgressScenarios
{
    [LiminalScenario(
        "Progress/Scenario/AdvanceMovesStepAndSaves",
        Scene = "HomeScene",
        Description = "ノベル / バトル完了の記録で次ノードへ進み、ディスクの Step も追従する。リセットで先頭へ戻る")]
    public static IEnumerable<ScenarioStep> AdvanceMovesStepAndSaves()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");

        yield return ScenarioStep.AssertCommandReturns("Progress/AdvanceAsNovel", null, "alv");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/NextSceneType", null, "Battle");

        yield return ScenarioStep.AssertCommandReturns("Progress/AdvanceAsBattle", Args("isPlayerWin", true), "prologue2");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "2");
        yield return ScenarioStep.AssertCommandReturns("Progress/HasSaveData", null, "True");

        yield return ScenarioStep.AssertCommandReturns("Progress/Reset", null, "prologue1");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");
    }
}
