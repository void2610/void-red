# CLAUDE.md

Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンス。

## プロジェクト概要

void-red は Unity 6000.3 で開発している人格構築・心理戦オークション ADV。記憶オークション (インゲーム) とノベルパート (novel-kit / MRuby シナリオ) をホーム (ロビー) から行き来する構成。ゲーム仕様は `Docs/voidred/` (入口は `README.md`、実装対応は `12-implementation.md`)。VContainer (DI)、R3 (リアクティブ)、UniTask (非同期)、LitMotion (アニメーション) を使用。

## 開発ワークフロー

1. コードを変更する
2. `uloop-compile` (`uloop compile --force-recompile true`) でコンパイル
3. `uloop-get-logs` (`uloop get-logs --log-type Error`) でコンパイルエラーがないことを確認
4. ロジック変更があれば `uloop-run-tests` (PlayMode) か `liminal run "<prefix>/*"` で該当シナリオを回す
5. コードを編集した場合は、必ず `./unity-coding-standards/scripts/run-format.sh` を実行し、push 前に `--verify-no-changes` (CI と同じ判定) を通す
6. **フォーマッタ / アナライザ / コンパイル / テストの出力は、末尾サマリ (`tail` や "Formatted N of M" 行) だけで判断せず必ず全文を確認する。**
    `warning` / `error` / `IDE` / `VUA` / `Unable to fix` 等の行が 1 件でも残っていないか確認し、残っていれば対処してから次に進む。出力が長い場合は `grep -iE "warning|error|IDE[0-9]|VUA[0-9]|Unable to fix"` で取りこぼしを防ぐ
7. 結果をユーザーに報告する

**YAGNI / KISS:** 将来の必要を先回りしない、今の要求を満たす最小限をシンプルに書く。過度な抽象化や汎用化を避け、1 クラス・1 メソッドに 1 責務。

## レスポンスガイドライン

- 日本語で応答し、プログラム内のコメントもすべて日本語で書く。過剰なコメントは避け、変更履歴や削除箇所への言及は書かない
- SerializeField で設定されるコンポーネントの null チェックは書かない (アナライザ `VUA1001` が警告する)。設定ミスは即座にクラッシュさせる方針
- リファクタリング時は後方互換性を維持せず、クリーンな置き換えを実装する
- フォーマッタやアナライザーが warning を出した場合は、`.editorconfig` や既存規約を自分で確認して自力で直す。自動修正不能という出力を理由に放置しない
- 新規シンボル (特に定数) は既存規約と照合する。定数は `ALL_UPPER` (例: `RESOURCES_ROOT`)
- 検証都合の状態・抽象をプロダクションコードに持ち込まない。観測点が要るなら Debug コマンド層 (`VoidRed.Debug`) に置く。既存の読み取り専用プロパティを View に足す程度は可
- 新しい仕組みを入れたら、判断基準と配置方針を `Docs/` かこのファイルへ残す

## 主要コマンド

```bash
# フォーマット自動修正 (unity-coding-standards の標準手順)
./unity-coding-standards/scripts/run-format.sh

# フォーマット確認のみ (CI と同じ)
./unity-coding-standards/scripts/run-format.sh --verify-no-changes

# LiminalScenario を CLI から回す (Play Mode 中)
liminal --mode runtime run "Novel/Scenario/*"
```

Unity 側の操作は **uloop / LiminalPalette skill を第一選択** とする。手動操作を依頼する前にまず skill で完結できないか検討すること。

## Unity 自動操作ツール (uloop / LiminalPalette)

uloop (Editor 操作) と LiminalPalette (ランタイム検証) の使い分け・運用方針・注意点は、`unity-standards` プラグインの skill に集約している。該当場面で参照すること:

- `unity-standards:uloop-guide` — Editor 操作 (コンパイル / テスト / シーン確認 / SerializeField 配線 / スクショ / メニュー)。ランタイム動作確認には使わない
- `unity-standards:liminal-palette-guide` — ランタイム動作確認・回帰テスト。検証は `[LiminalScenario]` として資産化する。LiminalPalette は void2610 自作ライブラリなので、不足は client 側 workaround より本体修正 / API 追加を検討する
- `unity-standards:unity-automation-unblock` — 自動操作中の Unity ダイアログ / 中断 (未保存シーン等) をコードで解消して自律進行する
- `unity-standards:submodule-workflow` — 自作ライブラリ submodule (my-unity-utils / my-unity-settings) を修正するときの Git 運用方針
- 実体の `liminal-*` / `uloop-*` skill は `.claude/skills/` に同梱 (LiminalPalette 更新後は `Tools/LiminalPalette/Install AI Skills...`、uloop 更新後は `uloop skills install --claude` で同期)

Play Mode はエディタが非フォーカスでも進む (`Run In Background` 有効)。検証のために `uloop-focus-window` でウィンドウを前面に出さない。時間のかかる Play Mode 検証はバックグラウンド実行してログで確認する。

## テスト方針

EditMode / シナリオ / CI の使い分けと決定性パターンは [`Docs/test-strategy.md`](./Docs/test-strategy.md) を参照。要点:

- **ルールの判定は EditMode テスト (`Assets/Tests/EditMode/*Tests.cs`) に書く**。ドメイン層は純 C# なので Play Mode 不要で数秒で回る
- **UI を通す検証は `[LiminalScenario]` (`Assets/Scripts/Debug/*Scenarios.cs`) として残す**。手で `liminal exec` を連打して終わらせない
- **E2E がフリーズしたらテストではなく製品を直す**。進行が止まらないことは `CompetitionRunner` / `PhaseLoopGuard` で保証し、EditMode で検証する
- `Assets/Tests/PlayMode/*E2ETests.cs` は prefix (`Novel/Scenario/` 等) でシナリオを列挙して Test Runner に流す薄いランナー。シナリオを追加すれば自動で乗る
- CI (`.github/workflows/test.yml`) が PR ごとに PlayMode テストを回す (self-hosted)
- 固定待ちではなく `AssertCommandEventually` / `WaitScene` / `WaitFadeDone` (`ScenarioFragments`) で状態を待つ。フレーム順に依存する箇所だけ `WaitFrames` を理由コメント付きで使う

## LiminalPalette (ランタイムデバッグ / 検証)

- `[LiminalCommand]` 付きのデバッグコマンドは `Assets/Scripts/Debug/*DebugCommands.cs` に置き、`DebugLifetimeScope` に登録する (`DebugBootstrap` が初回シーン読込み時に生成し、DontDestroyOnLoad で常駐、Root を親に構築される)
- asmdef 構成: `VoidRed` (Assets/Scripts 全体) / `VoidRed.Editor` (Assets/Scripts/Editor) / `VoidRed.Debug` (Assets/Scripts/Debug、`defineConstraints` で本番除外可能) / `VoidRed.Tests.PlayMode` (Assets/Tests/PlayMode)。新規の Debug 系コードは `VoidRed.Debug` に置く。例外: シーン / プレハブから参照される既存のデバッグ MonoBehaviour (`Assets/Scripts/DebugComponents/`) は本番ビルドで missing script になるため `VoidRed` に残し、実行時ガードで無効化する
- リリース前の現在は `LIMINAL_PALETTE_FORCE_ENABLE` (Scripting Define) と `Resources/PaletteRuntimeSettings.asset` (`DisableInProductionBuilds=false`) で非 Development ビルドにもデバッグ機能を含めている。リリース時はこの 2 点を戻す
- 生成される `InputSystem_Actions.cs` は `Assets/Scripts/Game/Core/` に出力する (VoidRed asmdef から参照するため)
- ランタイムの動作確認・状態観測は `.claude/skills/liminal-*` (HTTP API 経由) を第一選択にする。入口は `liminal-overview` スキル
- シーンローカルな LifetimeScope (NovelKitLifetimeScope 等) の依存はコンストラクタ注入できないため、コマンド呼び出し時に `LifetimeScope.Find<T>()` でスコープを引いて `Container.Resolve` する (`NovelDebugCommands` 参照)
- ボタン操作は `UI/ClickButton` (onClick 発火) で実経路を通す。Presenter に検証用 API を生やさない
- 任意シナリオの再生は `Novel/PlayScenario` (Root の `NovelPlaybackRequest` を `NovelKitStarter` が消費する。回想再生にも使う想定)
- パレットは Editor / Play Mode とも `Cmd/Ctrl + K` で開閉。LP 組み込みコマンド (`Scene/Current`, `Scene/Load` 等) と同じパスを自前で定義しない

## 設計原則

### DI 設計原則

- VContainer を使用する。シーンと LifetimeScope を 1:1 で対応させる (Title / Home / Auction / NovelKit / Thanks)。シーン横断サービスは `RootLifetimeScope` (VContainerSettings の Root プレハブ) に登録する
- View クラスのみ MonoBehaviour を継承し、Presenter / Service はピュア C# クラスでコンストラクタ注入する
- Presenter は View の Observable (`Button.OnClickAsObservable` 等) を購読して動く。View に業務ロジックを書かない

### 自作ライブラリ第一原則

- **汎用機能を書く前に必ず既存実装を探す**: `Assets/Scripts/Utils` (my-unity-utils の symlink) にオーディオ (SeManager / BgmManager)・展示モード・Steam / Discord 連携・Extensions 等が既にある。`ls Assets/Scripts/Utils/` と `grep -ril <キーワード> Assets/Scripts/Utils` を実装前に必ず実行する
- **既存 API の引数で足りないときはライブラリ側を拡張する**: クライアント側の手書きループや回避コードで済ませない

### シーン構成と遷移

- TitleScene → HomeScene → AuctionScene / NovelKitScene → HomeScene (オークション / ノベル後に戻る)。ThanksScene は展示モード用
- 遷移は `SceneTransitionManager.TransitionToSceneWithFade` に一本化 (フェード中は no-op になるため、連続遷移は `IsFading` を待つ)
- ストーリー進行は `GameProgressService` (現在ノード / 次ノード / セーブ / 持ち越しリソース / 人格) が持つ。ノベル完了・洗礼 (`RecordAuctionClearAndSave`) で次ノードへ進む。ゲームオーバーは進行度を動かさない

## オークション UI / データの再生成

プレハブ・シーン・初期データは `Assets/Scripts/Editor/AuctionUiBuilder.cs` / `AuctionDataBuilder.cs` の静的メソッドで生成する。`uloop execute-menu-item` はセキュリティ設定でブロックされるため、`uloop-execute-dynamic-code` で `AuctionUiBuilder.BuildPrefabs(); return "ok";` のように直接呼ぶ。レイアウト調整はビルダー側の数値を直して再実行する。`*LifetimeScope.cs` を新規作成すると VContainer のテンプレートが本文を上書きするので、作成後に中身を確認する。

## 依存パッケージ

- Utils (void2610/my-unity-utils) - 再利用可能な Unity Util スクリプト群 (submodule)
- SettingsSystem (void2610/my-unity-settings) - ゲーム内設定システム (submodule)
- novel-kit (void2610/novel-kit) - MRuby シナリオのノベルランタイム
- LiminalPalette (void2610/liminal-palette) - デバッグコマンドパレット / HTTP API / シナリオ実行基盤
- uloop (hatayama/uLoopMCP) - AI Agent が Unity Editor を操作するための基盤
- VContainer (hadashiA/VContainer) - 依存性注入
- R3 (Cysharp/R3) - リアクティブプログラミング
- UniTask (Cysharp/UniTask) - 非同期処理
- LitMotion (AnnulusGames/LitMotion) - アニメーション
- Unity Template (void2610/my-unity-template) - プロジェクトテンプレート
- Unity-SerializeReferenceExtensions (mackysoft/Unity-SerializeReferenceExtensions) - SubclassSelector
