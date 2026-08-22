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
        Description = "ノベル完了の記録で次ノードへ進み、ディスクの Step も追従する。リセットで先頭へ戻る")]
    public static IEnumerable<ScenarioStep> AdvanceMovesStepAndSaves()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");

        yield return ScenarioStep.AssertCommandReturns("Progress/AdvanceAsNovel", null, "auction0");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/NextSceneType", null, "Auction");
        yield return ScenarioStep.AssertCommandReturns("Progress/HasSaveData", null, "True");

        yield return ScenarioStep.AssertCommandReturns("Progress/Reset", null, "prologue1");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "0");
    }
}
