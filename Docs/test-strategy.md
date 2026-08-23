# テスト方針 (EditMode / LiminalScenario / CI)

**EditMode テストと E2E シナリオの両方を回す。役割が違うので、どちらも減らさない。**

- **EditMode**: ルールの判定と「進行が止まらないこと」。純 C# なので数秒で全件回る
- **E2E (LiminalScenario)**: 実 UI を通した導線。ボタンの配線や演出の順序はここでしか壊れが分からない

E2E がフリーズしたら、それは**テストの都合ではなく製品の不具合**として直す。
実際に競合フェーズが決着しない不具合で Editor が応答しなくなったことがあり、次の 3 点を製品側に入れた。

| 仕組み | 目的 |
| --- | --- |
| `CompetitionRunner` + `CompetitionState` の打ち切り | 競合が必ず決着する。時刻を外から渡すので EditMode で総当たり検証できる |
| `PhaseLoopGuard` | 同じフェーズが同じ状態で回り続けたら例外にする |
| `AuctionPresenter` の復帰処理 | 進行が壊れてもロビーへ戻し、操作できない状態で放置しない |

UI の押下は Observable の購読タイミングで取りこぼすと**押しても進まない詰み**になる。
確定 / 洗礼 / ゲームオーバーのボタンは View 側がフラグで受け、Presenter はそれを待つ。

## 層と置き場所

| 層 | 場所 | 役割 | 実行 |
| --- | --- | --- | --- |
| EditMode テスト | `Assets/Tests/EditMode/*Tests.cs` | ルールの判定 (勝敗 / 競合 / 歪み / 崩壊 / 対話の制限)。**まずここに書く** | `uloop run-tests --test-mode EditMode` |
| LiminalCommand | `Assets/Scripts/Debug/*DebugCommands.cs` | シナリオ / 手動デバッグ両用の操作・観測 API | `liminal exec` / パレット |
| LiminalScenario | `Assets/Scripts/Debug/*Scenarios.cs` | end-to-end の挙動検証 (シーン込み) | `liminal run "<prefix>/*"` / Test Runner |
| PlayMode テスト | `Assets/Tests/PlayMode/*E2ETests.cs` | prefix でシナリオを列挙して Test Runner に流す薄いランナー | `uloop-run-tests` / CI |
| CI | `.github/workflows/test.yml` | PR ごとに PlayMode テスト (self-hosted) | 自動 |

`Assets/Scripts/Debug` (`VoidRed.Debug` asmdef) と `Assets/Tests/PlayMode` (`VoidRed.Tests.PlayMode`) は本番コードから分離されている。

## シナリオの書き方

```csharp
[LiminalScenario("Novel/Scenario/PlayScenarioChooseAndReturnHome", Scene = "HomeScene", Description = "...")]
public static IEnumerable<ScenarioStep> PlayScenarioChooseAndReturnHome()
{
    foreach (var step in ResetProgress()) yield return step;   // 決定的な初期状態
    yield return WaitFadeDone();
    yield return ScenarioStep.Run("Novel/PlayScenario", Args("scenarioKey", "prologue2"));
    yield return ScenarioStep.AssertCommandEventually("Novel/ChoiceCount", null, "2", 20f, "選択肢表示待ち");
    ...
}
```

- `Scene = "..."` で開始シーンを Single ロードする。Root スコープ (VContainerSettings) と常駐の `DebugLifetimeScope` は自動で立つ
- prefix は `<領域>/Scenario/<名前>`。ランナー (`*E2ETests.cs`) は prefix で拾うので、新しい領域を作ったらランナーも 1 ファイル足す
- 前文は `ScenarioFragments.ResetProgress()` (進行度 + ノベル保存状態の初期化)。テストは実セーブを書き換えるので、決定性のために毎回リセットする
- 操作は実経路を通す: ボタンは `UI/ClickButton`、選択肢は `Novel/Choose`、送りは `Novel/AdvanceToNextLine`。同名ボタンが複数ある UI は属性などで引く専用コマンドを足す (`Auction/ClickPlus`)
- 見た目の確認は `UI/Screenshot path=<絶対パス>` で PNG を書き出して見る

## 決定性のパターン

- **固定待ち禁止**: `WaitSeconds` ではなく `AssertCommandEventually` / `AssertEventually` で状態を待つ
- **フェード**: `SceneTransitionManager` はフェード中の遷移要求を無言で捨てる。遷移や再生の前後は `WaitFadeDone()` / `WaitScene(name)` を挟む
- **同フレーム内の順序依存**: UniTask 継続と LP のステップ実行の順序に依存する箇所 (例: 確認ダイアログを閉じた直後の再要求) だけ `WaitFrames(2, "理由")` を使い、理由をコメントに残す
- **1 シナリオ 1 確認**: 細かく多く。glob (`"Novel/Scenario/*"`) で束ねて回す
- **判定の網羅は EditMode に寄せる**: 同じ確認を E2E で重複させない。E2E は導線を見る

## 実行方法

```bash
# Play Mode に入ってから (Editor 非フォーカスでも進む)
liminal --mode runtime scenarios                 # 一覧
liminal --mode runtime run "Novel/Scenario/*"    # glob 実行
liminal --mode runtime run "Title/Scenario/*" --report outputs/junit.xml
```

Test Runner からは `uloop-run-tests` (PlayMode)。CI は `.github/workflows/test.yml` が self-hosted ランナーで同じものを回す。

## 現在のシナリオ

| prefix | 内容 |
| --- | --- |
| `Progress/Scenario/` | ノベル / バトル完了記録で Step が進みセーブに追従、リセットで戻る |
| `Novel/Scenario/` | 任意シナリオ再生 → 選択 → 完走でホーム復帰 (進行度不変・フラグ保存)、行送りが台本どおり、スキップの確認ダイアログ |
| `Title/Scenario/` | はじめから (セーブ無し / 有り + 確認ダイアログ)、つづきから |
| `Auction/Scenario/` | 記憶オークション (11 本): 落札 → 洗礼 → セーブ、無落札のゲームオーバーとやり直し、歪み、対話の回数制限、観察と逆対話、競合、破産、テーマ鮮明化、楽園への鍵 |
| `Lobby/Scenario/` | ホームの進行案内、記憶コレクションの伏せ字 / 開示、人格画面 |

オークションは `Auction/Start` で階層・seed・競合の確定秒数を指定して起動する。seed を固定すると NPC の入札予定と対話の成否が決定的になる (`Docs/voidred/12-implementation.md`)。
