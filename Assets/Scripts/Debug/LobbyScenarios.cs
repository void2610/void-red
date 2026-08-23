using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// ロビー (HomeScene) の進行案内 / 記憶コレクション / 人格画面を検証する LiminalScenario
/// </summary>
public static class LobbyScenarios
{
    [LiminalScenario(
        "Lobby/Scenario/ProgressTextFollowsNextNode",
        Scene = "HomeScene",
        Description = "ホームの進行案内は次ノードに追従し、ノベル完了でオークションの案内に変わる")]
    public static IEnumerable<ScenarioStep> ProgressTextFollowsNextNode()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();
        yield return ScenarioStep.Run("Scene/Load", Args("sceneName", "HomeScene"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Lobby/ProgressText", null, "次: ストーリー (prologue1)");
        yield return ScenarioStep.Run("Progress/AdvanceAsNovel");
        yield return ScenarioStep.Run("Scene/Load", Args("sceneName", "HomeScene"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Lobby/ProgressText", null, "次: 第 0 階層の記憶オークション");
    }

    [LiminalScenario(
        "Lobby/Scenario/CollectionIsHiddenUntilWon",
        Scene = "HomeScene",
        Description = "記憶コレクションは全 25 件が並び、何も落札していなければすべて伏せ字。閉じるで戻る")]
    public static IEnumerable<ScenarioStep> CollectionIsHiddenUntilWon()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "CardLibButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/CollectionShowing", null, "True", 5f);
        yield return ScenarioStep.AssertCommandReturns("Lobby/CollectionEntryCount", null, "25");
        yield return ScenarioStep.AssertCommandReturns("Lobby/CollectionRevealedCount", null, "0");
        yield return ScenarioStep.AssertCommandReturns("Lobby/CollectionSummary", null, "収集 0 / 25");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "MemoryCollectionViewCloseButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/CollectionShowing", null, "False", 5f);
    }

    [LiminalScenario(
        "Lobby/Scenario/ClearedFloorShowsInCollectionAndPersona",
        Scene = "HomeScene",
        Description = "階層を突破して戻ると、落札した記憶がコレクションに現れ、統合した記憶が人格画面に出る")]
    public static IEnumerable<ScenarioStep> ClearedFloorShowsInCollectionAndPersona()
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();
        yield return ScenarioStep.Run("Auction/Start", new Dictionary<string, object> { ["floor"] = 0, ["seed"] = 7, ["timeout"] = 1.5f, ["speed"] = 6f });
        foreach (var step in WaitScene("AuctionScene")) yield return step;
        yield return ScenarioStep.Run("Auction/AutoPlayFloor", Args("winLots", 2));
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 60f, "洗礼待ち");
        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismReady", null, "True", 20f, "洗礼の札が並ぶまで待つ");
        yield return ScenarioStep.Run("Auction/ClickIntegrate", Args("lotIndex", 0));
        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismSelected", null, "True", 10f, "統合する記憶の選択が反映されるまで待つ");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "FinishButton"));
        foreach (var step in WaitScene("HomeScene")) yield return step;

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "CardLibButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/CollectionShowing", null, "True", 5f);
        yield return ScenarioStep.AssertCommandReturns("Lobby/CollectionRevealedCount", null, "2", "落札した 2 件だけ中身が見える");
        yield return ScenarioStep.AssertCommandReturns("Lobby/CollectionSummary", null, "収集 2 / 25");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "MemoryCollectionViewCloseButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/CollectionShowing", null, "False", 5f);

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "DeckButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/PersonaShowing", null, "True", 5f);
        yield return ScenarioStep.AssertCommandReturns("Lobby/PersonaIntegratedContains", Args("lotIndex", 0), "True", "統合した記憶の名前が並ぶ");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "PersonaViewCloseButton"));
        yield return ScenarioStep.AssertCommandEventually("Lobby/PersonaShowing", null, "False", 5f);
    }
}
