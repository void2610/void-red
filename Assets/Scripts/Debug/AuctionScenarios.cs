using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// 記憶オークションの進行を検証する LiminalScenario
/// seed 固定で NPC の入札予定を決定的にし、主人公の入札は実 UI を通して行う
/// </summary>
public static class AuctionScenarios
{
    private const int SEED = 7;

    // seed 1 は最初のロットでアルヴへの観察が成功し、逆対話まで発生する (決定的)
    private const int SEED_COUNTER_DIALOGUE = 1;

    [LiminalScenario(
        "Auction/Scenario/WinFirstLotThenClearFloor",
        Scene = "HomeScene",
        Description = "全額で 1 ロット目を落札し、残りは 0 枚で流す。洗礼で統合するとホームへ戻り、進行度 / 人格 / リソースがセーブされる")]
    public static IEnumerable<ScenarioStep> WinFirstLotThenClearFloor()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/PlayerResources", null, "40", "階層開始時は 8 種 × 5 枚");

        foreach (var step in BidAll()) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/LastWinner", null, "ノア", 10f, "全額入札なら単独最高額で落札");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "1");
        yield return ScenarioStep.AssertCommandReturns("Auction/PlayerResources", null, "0", "落札分は消費される");
        foreach (var step in NextLot()) yield return step;

        for (var i = 0; i < 4; i++) foreach (var step in PassLot()) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 10f, "5 ロット終了で洗礼へ");
        yield return ScenarioStep.AssertCommandReturns("Auction/CollapseConsistent", null, "True", "無落札のライバルだけが人格崩壊する");

        yield return ScenarioStep.Run("Auction/ClickIntegrate", Args("lotIndex", 0));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "FinishButton"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/IntegratedCount", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/CollectionCount", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/WalletTotal", null, "0", "残りリソースが持ち越される");
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "auction0", "洗礼で進行度が 1 つ進む");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "1");
    }

    [LiminalScenario(
        "Auction/Scenario/NoWinIsGameOverAndRetrySameFloor",
        Scene = "HomeScene",
        Description = "5 ロットすべて 0 枚で流すとゲームオーバー。やり直すと同じ階層が進行度を変えずに再開する")]
    public static IEnumerable<ScenarioStep> NoWinIsGameOverAndRetrySameFloor()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        for (var i = 0; i < 5; i++) foreach (var step in PassLot()) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "GameOver", 10f, "無落札はゲームオーバー");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "0");

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "RetryButton"));
        yield return WaitFadeDone();
        foreach (var step in WaitScene("AuctionScene")) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/Phase", null, "ThemeAnnounce", 10f, "同じ階層が最初から再開する");
        yield return ScenarioStep.AssertCommandReturns("Auction/PlayerResources", null, "40", "手持ちは補充し直される");
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "prologue1", "進行度は動かない");
        yield return ScenarioStep.AssertCommandReturns("Progress/IntegratedCount", null, "0");
    }

    [LiminalScenario(
        "Auction/Scenario/DistortionCountsMismatchedEmotion",
        Scene = "HomeScene",
        Description = "ロットの属性と一致しない枚数だけ歪みになり、統合すると入札の主属性が感情状態になる")]
    public static IEnumerable<ScenarioStep> DistortionCountsMismatchedEmotion()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        // 一致属性 5 枚 + 不一致 12 枚 (4 枚 × 3 種)。一致属性が主属性のまま NPC の予定を確実に上回る
        yield return ScenarioStep.Run("Auction/RememberLotEmotion");
        foreach (var step in ToBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/BidMatchingAndMismatched", Args("matching", 5, "mismatched", 12));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/LastWinner", null, "ノア", 10f);
        yield return ScenarioStep.AssertCommandReturns("Auction/WonDistortion", Args("index", 0), "12", "不一致の枚数がそのまま歪みになる");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonViaCompetition", Args("index", 0), "False");
        foreach (var step in NextLot()) yield return step;

        for (var i = 0; i < 4; i++) foreach (var step in PassLot()) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 10f);
        yield return ScenarioStep.Run("Auction/ClickIntegrate", Args("lotIndex", 0));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "FinishButton"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/TotalDistortion", null, "12");
        yield return ScenarioStep.AssertCommandReturns("Progress/PersonaEmotionIsRemembered", null, "True", "入札の主属性 (一致属性) が感情状態になる");
    }

    [LiminalScenario(
        "Auction/Scenario/DialogueCommandsAreOncePerRivalPerLot",
        Scene = "HomeScene",
        Description = "対話コマンドは各ライバルに各 1 回。使うと同じコマンドは押せなくなり、別のライバルには使える")]
    public static IEnumerable<ScenarioStep> DialogueCommandsAreOncePerRivalPerLot()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Provoke"), "True");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "Slot_アルヴ"));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ProvokeButton"));
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Provoke"), "False", "同じコマンドは同じロットで二度使えない");
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Persuade"), "True", "別コマンドは使える");
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "喜ぶ参加者", "command", "Provoke"), "True", "別のライバルには使える");
        yield return ScenarioStep.AssertCommandReturns("Auction/DialogueResultShown", null, "True", "失敗してもセリフが返る");
    }

    [LiminalScenario(
        "Auction/Scenario/ObserveRevealsPlannedTotalAndCounterDialogueShifts",
        Scene = "HomeScene",
        Description = "観察が成功すると入札予定枚数が見え、逆対話が起きたら二択の返答で相手の予定が動く")]
    public static IEnumerable<ScenarioStep> ObserveRevealsPlannedTotalAndCounterDialogueShifts()
    {
        foreach (var step in StartAuction(seed: SEED_COUNTER_DIALOGUE)) yield return step;
        foreach (var step in OpenLot()) yield return step;
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "Slot_アルヴ"));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ObserveButton"));
        yield return ScenarioStep.AssertCommandReturns("Auction/ObservedMatchesPlanned", Args("name", "アルヴ"), "True", "観察結果は予定枚数そのもの");
        yield return ScenarioStep.AssertCommandReturns("Auction/ActivePanel", null, "Counter", "逆対話の二択が出る");
        yield return ScenarioStep.Run("Auction/RememberPlanned", Args("name", "アルヴ"));
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ChoiceAButton"));
        yield return ScenarioStep.AssertCommandReturns("Auction/ActivePanel", null, "Dialogue", "返答すると対話に戻る");
        yield return ScenarioStep.AssertCommandReturns("Auction/PlannedDelta", Args("name", "アルヴ"), "3", "選択肢 A (大幅増加) で +3");
    }

    [LiminalScenario(
        "Auction/Scenario/TieGoesToCompetitionAndRaiseWins",
        Scene = "HomeScene",
        Description = "最高額が同数なら競合に入る。上乗せして単独最高額のまま時間切れになれば落札し、競合の入札は返らない")]
    public static IEnumerable<ScenarioStep> TieGoesToCompetitionAndRaiseWins()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        foreach (var step in ToBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/BidToTieTopRival");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/Competing", null, "True", 5f, "同数なら競合へ");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "NextButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Competition", 5f);
        yield return ScenarioStep.Run("Auction/RaiseUntilLeading");
        yield return ScenarioStep.AssertCommandEventually("Auction/LastWinner", null, "ノア", 20f, "単独最高額のまま時間切れで落札");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonViaCompetition", Args("index", 0), "True");
        yield return ScenarioStep.AssertCommandReturns("Auction/CompetitionLosersRefunded", null, "False", "競合に入った入札は敗者にも返らない");
    }

    [LiminalScenario(
        "Auction/Scenario/BankruptRivalLeavesTable",
        Scene = "HomeScene",
        Description = "リソースが尽きたライバルは以降のロットの入札に参加しない")]
    public static IEnumerable<ScenarioStep> BankruptRivalLeavesTable()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenLot()) yield return step;
        yield return ScenarioStep.Run("Auction/DrainRival", Args("name", "アルヴ"));
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Observe"), "False", "卓から外れた相手には対話できない");
        foreach (var step in ToBidding()) yield return step;
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/RevealReached", null, "True", 10f, "開示待ち");
        yield return ScenarioStep.AssertCommandReturns("Auction/LastBidderCount", null, "3", "主人公 0 枚とアルヴを除く 3 人だけが入札に参加した");
    }

    // ---- 部品 ----

    private static IEnumerable<ScenarioStep> StartAuction(float competitionTimeout = 1.5f, int seed = SEED)
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();
        yield return ScenarioStep.Run("Auction/Start", new Dictionary<string, object> { ["floor"] = 0, ["seed"] = seed, ["timeout"] = competitionTimeout });
        foreach (var step in WaitScene("AuctionScene")) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/WaitingFor", null, "Theme", 10f, "テーマ公開待ち");
    }

    /// <summary>テーマ公開 → ロット提示の「次へ」を押して対話フェーズへ</summary>
    private static IEnumerable<ScenarioStep> OpenLot()
    {
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "NextButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/WaitingFor", null, "LotIntro", 10f, "ロット提示待ち");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "NextButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Dialogue", 10f, "対話フェーズ待ち");
    }

    private static IEnumerable<ScenarioStep> ToBidding()
    {
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ToBiddingButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Bid", 10f, "入札パネル待ち");
    }

    /// <summary>手持ち全部を入札して確定する</summary>
    private static IEnumerable<ScenarioStep> BidAll()
    {
        foreach (var step in ToBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/BidAll");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
    }

    /// <summary>開示後、NPC 同士の競合があれば見届けてから落札表示の「次へ」を押し、次ロットの提示も押して対話フェーズへ</summary>
    private static IEnumerable<ScenarioStep> NextLot()
    {
        yield return ScenarioStep.AssertCommandEventually("Auction/RevealReached", null, "True", 10f, "開示待ち");
        yield return ScenarioStep.Run("Auction/ClickNextIfTie");
        yield return ScenarioStep.AssertCommandEventually("Auction/WaitingFor", null, "LotResult", 20f, "落札表示待ち (競合なら確定まで)");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "NextButton"));
        yield return ScenarioStep.AssertCommandEventually("Auction/LotIntroOrEndReached", null, "True", 10f);
        yield return ScenarioStep.Run("Auction/ClickNextIfWaiting");
    }

    /// <summary>0 枚で入札して流し、次のロットの対話フェーズ (または終了画面) まで進める</summary>
    private static IEnumerable<ScenarioStep> PassLot()
    {
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Dialogue", 10f, "対話フェーズ待ち");
        foreach (var step in ToBidding()) yield return step;
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "ConfirmButton"));
        foreach (var step in NextLot()) yield return step;
    }
}
