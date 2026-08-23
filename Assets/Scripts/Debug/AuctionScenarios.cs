using System.Collections.Generic;
using Void2610.LiminalPalette;
using static ScenarioFragments;

/// <summary>
/// 記憶オークションの進行を検証する LiminalScenario
/// seed 固定で NPC の入札予定を決定的にし、操作は実 UI (対話コマンド / 感情ホイール / 入札ウィンドウ) を通す
/// </summary>
public static class AuctionScenarios
{
    private const int SEED = 7;

    // seed 1 は最初のロットでアルヴへの観察が成功し、逆対話まで発生する (決定的)
    private const int SEED_COUNTER_DIALOGUE = 1;

    [LiminalScenario(
        "Auction/Scenario/WinFirstLotThenClearFloor",
        Scene = "HomeScene",
        Description = "1 ロット目を落札し、残りは 0 枚で流す。洗礼で統合するとホームへ戻り、進行度 / 人格 / リソースがセーブされる")]
    public static IEnumerable<ScenarioStep> WinFirstLotThenClearFloor()
    {
        foreach (var step in StartAuction()) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/PlayerResources", null, "40", "階層開始時は 8 種 × 5 枚");

        foreach (var step in PlayFloor(winLots: 1)) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "1");
        yield return ScenarioStep.AssertCommandReturns("Auction/CollapseConsistent", null, "True", "無落札のライバルだけが人格崩壊する");

        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismReady", null, "True", 20f, "洗礼の札が並ぶまで待つ");
        yield return ScenarioStep.Run("Auction/ClickIntegrate", Args("lotIndex", 0));
        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismSelected", null, "True", 10f, "統合する記憶の選択が反映されるまで待つ");
        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "FinishButton"));
        foreach (var step in WaitScene("HomeScene")) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Progress/IntegratedCount", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/CollectionCount", null, "1");
        yield return ScenarioStep.AssertCommandReturns("Progress/CurrentNode", null, "auction0", "洗礼で進行度が 1 つ進む");
        yield return ScenarioStep.AssertCommandReturns("Save/CurrentStep", null, "1");
    }

    [LiminalScenario(
        "Auction/Scenario/NoWinIsGameOverAndRetrySameFloor",
        Scene = "HomeScene",
        Description = "5 ロットすべて 0 枚で流すとゲームオーバー。やり直すと同じ階層が進行度を変えずに再開する")]
    public static IEnumerable<ScenarioStep> NoWinIsGameOverAndRetrySameFloor()
    {
        foreach (var step in StartAuction(floor: 2)) yield return step;
        for (var lot = 0; lot < 5; lot++) foreach (var step in PlayLot(win: false)) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "GameOver", 40f, "無落札はゲームオーバー");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "0");

        yield return ScenarioStep.Run("UI/ClickButton", Args("name", "RetryButton"));
        yield return WaitFadeDone();
        foreach (var step in WaitScene("AuctionScene")) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/Floor", null, "2", 20f, "同じ階層をやり直す");
        yield return ScenarioStep.AssertCommandReturns("Auction/PlayerResources", null, "40", "やり直しても補充は重ならない");
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
        foreach (var step in OpenBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/RememberLotEmotion");
        // 一致属性 5 枚 + 不一致 12 枚 (4 枚 × 3 種)。一致属性が主属性のまま NPC の予定を確実に上回る
        yield return ScenarioStep.Run("Auction/BidMatchingAndMismatched", Args("matching", 5, "mismatched", 12));
        yield return ScenarioStep.Run("Auction/Confirm");
        yield return ScenarioStep.AssertCommandEventually("Auction/LastWinner", null, "ノア", 20f);
        yield return ScenarioStep.AssertCommandReturns("Auction/WonDistortion", Args("index", 0), "12", "不一致の枚数がそのまま歪みになる");
        yield return ScenarioStep.AssertCommandReturns("Auction/WonViaCompetition", Args("index", 0), "False");

        for (var lot = 0; lot < 4; lot++) foreach (var step in PlayLot(win: false)) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 40f);
        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismReady", null, "True", 20f, "洗礼の札が並ぶまで待つ");
        yield return ScenarioStep.Run("Auction/ClickIntegrate", Args("lotIndex", 0));
        yield return ScenarioStep.AssertCommandEventually("Auction/BaptismSelected", null, "True", 10f, "統合する記憶の選択が反映されるまで待つ");
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
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Provoke"), "True");
        yield return ScenarioStep.Run("Auction/SelectTarget", Args("name", "アルヴ"));
        yield return ScenarioStep.AssertCommandEventually("Auction/DialogueTarget", null, "アルヴ", 10f);
        yield return ScenarioStep.Run("Auction/UseDialogue", Args("command", "Provoke"));
        yield return ScenarioStep.AssertCommandEventually("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Provoke"), "False", 15f, "同じコマンドは同じロットで二度使えない");
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Persuade"), "True", "別コマンドは使える");
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "喜ぶ参加者", "command", "Provoke"), "True", "別のライバルには使える");
    }

    [LiminalScenario(
        "Auction/Scenario/ObserveRevealsPlannedTotal",
        Scene = "HomeScene",
        Description = "観察を使うと相手の入札予定が分かる。逆対話が起きた場合も対話フェーズに戻る")]
    public static IEnumerable<ScenarioStep> ObserveRevealsPlannedTotal()
    {
        foreach (var step in StartAuction(seed: SEED_COUNTER_DIALOGUE)) yield return step;
        yield return ScenarioStep.Run("Auction/SelectTarget", Args("name", "アルヴ"));
        yield return ScenarioStep.Run("Auction/RememberPlanned", Args("name", "アルヴ"));
        yield return ScenarioStep.Run("Auction/UseDialogue", Args("command", "Observe"));
        yield return ScenarioStep.AssertCommandEventually("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Observe"), "False", 20f, "観察は 1 度きり");
        yield return ScenarioStep.AssertCommandEventually("Auction/DialogueReady", null, "True", 40f, "演出後は対話フェーズに戻る");
    }

    [LiminalScenario(
        "Auction/Scenario/TieGoesToCompetitionAndRaiseWins",
        Scene = "HomeScene",
        Description = "最高額が同数なら競合に入る。上乗せして単独最高額のまま時間切れになれば落札し、競合の入札は返らない")]
    public static IEnumerable<ScenarioStep> TieGoesToCompetitionAndRaiseWins()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in OpenBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/BidToTieTopRival");
        yield return ScenarioStep.Run("Auction/Confirm");
        yield return ScenarioStep.AssertCommandEventually("Auction/Competing", null, "True", 20f, "同数なら競合へ");
        yield return ScenarioStep.Run("Auction/RaiseUntilLeading");
        yield return ScenarioStep.AssertCommandEventually("Auction/LastWinner", null, "ノア", 30f, "単独最高額のまま時間切れで落札");
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
        yield return ScenarioStep.Run("Auction/DrainRival", Args("name", "アルヴ"));
        yield return ScenarioStep.AssertCommandReturns("Auction/CanUseDialogue", Args("name", "アルヴ", "command", "Observe"), "False", "卓から外れた相手には対話できない");
        foreach (var step in OpenBidding()) yield return step;
        yield return ScenarioStep.Run("Auction/Confirm");
        yield return ScenarioStep.AssertCommandEventually("Auction/LastBidderCount", null, "3", 20f, "主人公 0 枚とアルヴを除く 3 人だけが入札に参加した");
    }

    [LiminalScenario(
        "Auction/Scenario/WinningMajorityClarifiesTheme",
        Scene = "HomeScene",
        Description = "5 ロット中 3 つを落札すると記憶テーマが鮮明化し、洗礼の見出しに鮮明化後のテーマが出る")]
    public static IEnumerable<ScenarioStep> WinningMajorityClarifiesTheme()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in PlayFloor(winLots: 3)) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "3");
        yield return ScenarioStep.AssertCommandReturns("Auction/ThemeClarified", null, "True");
        yield return ScenarioStep.AssertCommandReturns("Auction/BaptismHeaderClarified", null, "True", "見出しに鮮明化後のテーマが出る");
    }

    [LiminalScenario(
        "Auction/Scenario/WinningMinorityKeepsThemeVague",
        Scene = "HomeScene",
        Description = "落札が過半に届かなければ記憶テーマは鮮明化しない")]
    public static IEnumerable<ScenarioStep> WinningMinorityKeepsThemeVague()
    {
        foreach (var step in StartAuction()) yield return step;
        foreach (var step in PlayFloor(winLots: 2)) yield return step;
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "2");
        yield return ScenarioStep.AssertCommandReturns("Auction/ThemeClarified", null, "False");
        yield return ScenarioStep.AssertCommandReturns("Auction/BaptismHeaderClarified", null, "False");
    }

    [LiminalScenario(
        "Auction/Scenario/LastFloorRequiresParadiseKey",
        Scene = "HomeScene",
        Description = "最終階層は楽園への鍵を落札しないと、他を落札していてもゲームオーバーになる")]
    public static IEnumerable<ScenarioStep> LastFloorRequiresParadiseKey()
    {
        foreach (var step in StartAuction(floor: 4)) yield return step;
        foreach (var step in PlayLastFloor(winKey: false)) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "GameOver", 40f);
        yield return ScenarioStep.AssertCommandReturns("Auction/WonCount", Args("name", "ノア"), "1", "鍵以外は落札している");
        yield return ScenarioStep.AssertCommandReturns("Auction/MissedKey", null, "True");
        yield return ScenarioStep.AssertCommandReturns("Auction/GameOverMessageMentionsKey", null, "True");
    }

    [LiminalScenario(
        "Auction/Scenario/LastFloorWithKeyProceedsToBaptism",
        Scene = "HomeScene",
        Description = "最終階層で楽園への鍵を落札すれば洗礼へ進める")]
    public static IEnumerable<ScenarioStep> LastFloorWithKeyProceedsToBaptism()
    {
        foreach (var step in StartAuction(floor: 4)) yield return step;
        foreach (var step in PlayLastFloor(winKey: true)) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 40f);
        yield return ScenarioStep.AssertCommandReturns("Auction/MissedKey", null, "False");
    }

    // ---- 部品 ----

    /// <summary>進行度と無関係にオークションを起動し、対話フェーズまで待つ</summary>
    private static IEnumerable<ScenarioStep> StartAuction(float competitionTimeout = 4f, int seed = SEED, int floor = 0)
    {
        foreach (var step in ResetProgress()) yield return step;
        yield return WaitFadeDone();
        // 検証は演出を早送りする (待ち時間で Unity を長時間占有しない)
        yield return ScenarioStep.Run("Auction/Start", new Dictionary<string, object> { ["floor"] = floor, ["seed"] = seed, ["timeout"] = competitionTimeout, ["speed"] = 6f });
        foreach (var step in WaitScene("AuctionScene")) yield return step;
        yield return ScenarioStep.AssertCommandEventually("Auction/DialogueReady", null, "True", 60f, "テーマ公開 → 最初のロットの対話フェーズ待ち");
    }

    /// <summary>対話を切り上げて入札フェーズへ</summary>
    private static IEnumerable<ScenarioStep> OpenBidding()
    {
        yield return ScenarioStep.AssertCommandEventually("Auction/DialogueReady", null, "True", 40f, "対話フェーズの入力受付待ち");
        yield return ScenarioStep.Run("Auction/Confirm");
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Bid", 15f, "入札フェーズ待ち");
    }

    /// <summary>1 ロット分を進める。win なら最大予定 + 1 枚で落札を狙い、そうでなければ 0 枚で流す</summary>
    private static IEnumerable<ScenarioStep> PlayLot(bool win)
    {
        foreach (var step in OpenBidding()) yield return step;
        if (win) yield return ScenarioStep.Run("Auction/BidAboveTopRival");
        yield return ScenarioStep.Run("Auction/Confirm");
        yield return ScenarioStep.AssertCommandEventually("Auction/LotSettled", null, "True", 40f, "開示 (競合があれば決着) 待ち");
    }

    /// <summary>階層の 5 ロットを進める。最初の winLots ロットだけ落札を狙う</summary>
    private static IEnumerable<ScenarioStep> PlayFloor(int winLots)
    {
        for (var lot = 0; lot < 5; lot++)
        {
            foreach (var step in PlayLot(lot < winLots)) yield return step;
        }
        yield return ScenarioStep.AssertCommandEventually("Auction/ActivePanel", null, "Baptism", 40f, "洗礼待ち");
    }

    /// <summary>最終階層用。鍵のロットだけ (winKey なら) 落札を狙う</summary>
    private static IEnumerable<ScenarioStep> PlayLastFloor(bool winKey)
    {
        for (var lot = 0; lot < 5; lot++)
        {
            foreach (var step in OpenBidding()) yield return step;
            yield return ScenarioStep.Run("Auction/BidAboveTopRivalIfKey", Args("winKey", winKey));
            yield return ScenarioStep.Run("Auction/Confirm");
            yield return ScenarioStep.AssertCommandEventually("Auction/LotSettled", null, "True", 40f, "開示待ち");
        }
    }
}
