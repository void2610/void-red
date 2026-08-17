# CLAUDE.md

Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンス。

## プロジェクト概要

void-red は Unity 6000.0.50f1 で開発されているカードゲームプロジェクト。VContainer（依存性注入）、R3（リアクティブプログラミング）、LitMotion（アニメーション）、UniTask（非同期処理）、Unity-SerializeReferenceExtensions（エディタカスタマイズ）を使用。

## 開発ワークフロー

1. コードを変更する
2. `mcp__uLoopMCP__compile` (ForceRecompile=false) でコンパイル
3. `mcp__uLoopMCP__get-logs` (LogType=Error) でコンパイルエラーがないことを確認
4. フォーマット自動修正を実行:
   - `./unity-coding-standards/scripts/run-format.sh`
5. 結果をユーザーに報告する

**YAGNI (You Aren't Gonna Need It) 原則:**
- 将来必要になるかもしれない機能を先回りして実装しない
- 現在の要求を満たす最小限の実装に留める
- 過度な抽象化や汎用化を避ける
- 実際に必要になった時点で機能を追加する

**KISS (Keep It Simple, Stupid) 原則:**
- 可能な限りシンプルで理解しやすいコードを書く
- 複雑な設計パターンは本当に必要な場合のみ使用
- 1つのクラス・メソッドには1つの責任のみを持たせる
- 誰でも理解できる明快な実装を優先する

## レスポンスガイドライン

- SerializeFieldで設定されるべきコンポーネントのnullチェックは不要。コンポーネントが正しく設定されていることを前提としたコードを記述する
- プログラム内の全てのコメントは日本語で記述する
- 日本語で応答し、過剰なコメントは避ける
- リファクタリング時は後方互換性を維持せず、クリーンな置き換えを実装する
- YAGNI・KISS原則に従い、今必要なものだけを実装し、シンプルに保つ

## 主要コマンド

```bash
# フォーマット自動修正
./unity-coding-standards/scripts/run-format.sh

# フォーマット確認のみ
./unity-coding-standards/scripts/run-format.sh --verify-no-changes
```

コンパイルは `mcp__uLoopMCP__compile` (ForceRecompile=false) を使用し、その後 `mcp__uLoopMCP__get-logs` (LogType=Error) でエラーを確認する。

## 設計原則
- **明確なファイル構造**: 機能別ディレクトリで保守性向上
- **責任の明確化**: Stats, Services, Logic, Coreの分離
- **Unity-First**: MonoBehaviourパターンの自然な活用
- **リアクティブ設計**: R3による状態変更の伝播

## 実装詳細

### VContainer設定
```csharp
// RootLifetimeScope.cs - シーン横断サービス
builder.Register<SaveDataManager>(Lifetime.Singleton);
builder.Register<SceneTransitionService>(Lifetime.Singleton);
builder.Register<GameProgressService>(Lifetime.Singleton);

// BattleLifetimeScope.cs - バトル固有の依存関係
builder.RegisterInstance(player);
builder.RegisterInstance(enemy);
builder.Register<CardPoolService>(Lifetime.Singleton);
builder.Register<ThemeService>(Lifetime.Singleton);
builder.RegisterEntryPoint<GameManager>();
builder.RegisterComponentInHierarchy<UIPresenter>();

// HomeLifetimeScope.cs - ホームシーンの依存関係
builder.RegisterComponentInHierarchy<HomeUIPresenter>();
```

### シーン構成
- **TitleScene**: TitleUIPresenterによるエントリーポイント
- **HomeScene**: HomeUIPresenterによるナビゲーションハブ
- **BattleScene**: Player, Enemy, UIPresenterコンポーネントによるメインゲームシーン
- **NovelScene**: NovelUIPresenterによるストーリーシーン

### シーン遷移フロー
- Title → Home → Battle/Novel → Home（バトル/ストーリー後に戻る）
- 全シーンでSceneTransitionServiceを使用してナビゲーション
- シーン間のデータ永続化はトランジションデータオブジェクト経由

## LiminalPalette (ランタイムデバッグ / 検証)

- `[LiminalCommand]` 付きのデバッグコマンドは `Assets/Scripts/Debug/*DebugCommands.cs` に置き、`DebugLifetimeScope` に登録する (`DebugBootstrap` が各シーンへ自動生成し、Root を親に構築される)
- このプロジェクトは asmdef を持たないため、Debug 系ファイルは全体を `#if UNITY_EDITOR || DEVELOPMENT_BUILD || LIMINAL_PALETTE_FORCE_ENABLE` で囲んで本番ビルドから除外する
- ランタイムの動作確認・状態観測は `.claude/skills/liminal-*` (HTTP API 経由) を第一選択にする。運用方針は `unity-standards:liminal-palette-guide` を参照
- パレットは Editor / Play Mode とも `Cmd/Ctrl + K` で開閉。LP 組み込みコマンド (`Scene/Current`, `Scene/Load` 等) と同じパスを自前で定義しない

## 依存パッケージ

- VContainer (hadashiA/VContainer) - 依存性注入
- R3 (Cysharp/R3) - リアクティブプログラミング
- UniTask (Cysharp/UniTask) - 非同期処理
- LitMotion (AnnulusGames/LitMotion) - アニメーション
- Unity Template (void2610/my-unity-template) - プロジェクトテンプレート
- Unity-SerializeReferenceExtensions (mackysoft/Unity-SerializeReferenceExtensions) - SubclassSelector
- LiminalPalette (void2610/liminal-palette) - デバッグコマンドパレット / HTTP API (開発時専用)
