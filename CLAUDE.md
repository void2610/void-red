# void-red プロジェクト固有の開発ガイドライン

## Unity開発における注意点

### Unityオブジェクトのnullチェック
- RiderでUnityオブジェクトのnullチェック時に「Unity オブジェクトの有効期間を暗黙的にチェックしています」という警告が出る
- `if (obj != null)` ではなく `if (obj)` を使用する
- `if (obj == null)` ではなく `if (!obj)` を使用する
- 例：
  ```csharp
  // ❌ 警告が出る
  if (cardButton != null)
  if (_cardData == null) return;
  
  // ✅ 推奨
  if (cardButton)
  if (!_cardData) return;
  ```

## プロジェクト構成

### VContainer + R3 + LitMotion + UniTask 構成
- VContainer: 依存性注入（Singleton管理）
- R3: リアクティブプログラミング（ReactiveProperty）
- LitMotion: 高性能アニメーション（文字列アニメーション含む）
- UniTask: 非同期処理

### 簡略化MVPパターン

#### アーキテクチャ概要
```
Presenter層 (上位管理)
├── UIPresenter (UI統合管理)
└── GameManager (ゲームフロー制御)
    │
    ▼
View層 (表示+基本ロジック)
├── CardView (カード表示+アニメーション)
├── HandView (手札管理+配置)
└── 各種UIView (Theme, Announcement等)
    │
    ▼
Model層 (データ)
├── CardData, PlayerMove
└── ThemeData, CardStatus等
```

#### 設計方針
- **適切な粒度**: 過度な分離を避け、実用的なレベルで責務分離
- **自己完結性**: 各Viewが必要な機能（アニメーション等）を内包
- **シンプルな依存関係**: 複雑な依存性注入を排除
- **Unity親和性**: MonoBehaviourベースの自然な構造

### カードゲーム設計

#### 基本要素
- **CardData**: ScriptableObjectでカード情報を定義
  - CardStatus: 許し、拒絶、空白の3つのfloatパラメータ
  - ScoreMultiplier: カード固有の倍率
  - CollapseThreshold: 崩壊閾値
- **CardView**: カードの表示とアニメーション（簡略化MVP）
  - LitMotionによる自己完結的なアニメーション
  - R3によるリアクティブイベント処理
- **HandView**: 手札管理とカード配置（簡略化MVP）
  - 扇状配置とアニメーション制御
  - カード選択状態の一元管理
- **PlayerMove**: プレイヤーの手（CardData、プレイスタイル、精神ベット）
  - UIに依存しない純粋なデータクラス

#### 精神力システム
- **BasePlayer**: プレイヤーとエネミー共通の基底クラス
  - HandViewを直接参照（シンプルな構造）
  - 最大精神力20からスタート
  - 精神ベットで消費、ゲーム進行で自動回復
  - 精神力不足時のベット制限
- **精神ベット**: 1-7の範囲でベット可能
- **UI表示**: "現在精神力 / 最大精神力" 形式

#### テーマシステム
- **ThemeData**: ScriptableObjectでテーマ情報を定義
  - CardStatus: テーマの効果値
  - Title: プレイヤーに表示されるタイトル（数値は非表示）
- **ThemeService**: テーマ選択を管理するサービス
- **AllThemeData**: テーマのコレクション管理

#### カード崩壊システム
- **閾値システム**: カードごとに崩壊閾値を設定
- **崩壊確率**: 閾値未満では崩壊しない、閾値を超えると崩壊確率が上昇
- **永続削除**: 崩壊したカードはデッキから永久に削除

#### スコア計算
```csharp
スコア = テーマ一致度 × 精神ベット × カード倍率
一致度 = 1.0 + (1.0 - (距離 / √3)) × 0.5
```

#### ゲームフロー
1. **テーマアナウンス**: ランダムテーマ選択、文字列アニメーション表示
2. **プレイヤーフェーズ**: カード選択→プレイボタン表示→確定
3. **エネミーフェーズ**: AI自動選択
4. **評価フェーズ**: スコア計算と表示
5. **結果表示**: 勝敗判定、崩壊判定、カード処理
6. **新ターン**: カードをデッキに戻して再配布

### Service層設計
- **CardPoolService**: カードプール管理
- **ThemeService**: テーマ選択管理
- 両サービスともVContainerでSingleton登録
- ❌ CardAnimationService（削除済み - CardView内蔵）

### UI/UX設計
- **段階的操作**: カード選択→ボタン表示→確定の段階的フロー
- **アニメーション**: LitMotionによるテーマテキストの徐々表示
- **状態管理**: プレイボタンの適切な表示/非表示制御
- **音響処理**: ButtonSeでGameObject非アクティブ時のエラー対策

### nullチェックのガイドライン
- SerializeFieldやInspectorで事前に設定すべき要素（GameManager、Player、Enemy、UIManager等）に対するnullチェックは避ける
- これらの要素がnullの場合は設定ミスであり、NullReferenceExceptionが発生して然るべき
- nullチェックを行うのは、ランタイム中に動的に設定される要素のみ
  - 例：動的に生成/破棄されるオブジェクト、ネットワークから取得するデータ、ユーザー入力によって変化する値など
- `?.` や `??` 演算子の過剰な使用は避け、コードの可読性を重視する

### 非同期処理のガイドライン
- **UniTask使用**: 軽量で高性能な非同期処理
- **Subscription管理**: 複雑なイベント購読は避け、シンプルなポーリングを優先
- **状態監視**: `UniTask.Yield()`による軽量なポーリングパターン
- **キャンセレーション**: 適切なCancellationToken管理

## Unity開発ツール

### unity-compile.sh
Unityのコンパイルエラーをチェックし、コンパイルをトリガーするシンプルなツール。

#### 使用方法
```bash
# コンパイルエラーのチェック
./unity-tools/unity-compile.sh check .

# コンパイルをトリガー（Unity Editorでcmd+Rを実行）
./unity-tools/unity-compile.sh trigger .

# コンパイル後にエラーチェック
./unity-tools/unity-compile.sh trigger . && sleep 3 && ./unity-tools/unity-compile.sh check .
```

#### 出力例
```bash
# エラーなしの場合
📋 Checking Unity log: /Users/user/Library/Logs/Unity/Editor.log
✅ No recent compilation errors detected
📝 Last compile status: CompileScripts: 1.603ms

# エラーありの場合
📋 Checking Unity log: /Users/user/Library/Logs/Unity/Editor.log
❌ Recent compilation errors found:
Assets/Scripts/Example.cs(11,9): error CS0103: The name 'NonExistentMethod' does not exist in the current context
```

#### 注意事項
- Unity Editorが起動している必要がある
- macOS専用（AppleScriptを使用）
- triggerコマンドはUnityエディターを最前面に移動させる

## 実装完了機能

### 簡略化MVPアーキテクチャ
- ✅ CardView（表示+アニメーション統合）
- ✅ HandView（手札管理+配置）
- ✅ UIPresenter（UI統合管理）
- ✅ BasePlayer簡素化（HandView直接参照）
- ✅ PlayerMove純粋データ化（CardData保持）

### 精神力システム
- ✅ MP管理（消費/回復）
- ✅ UI表示（現在/最大）
- ✅ ベット制限
- ✅ デバッグログ

### テーマシステム  
- ✅ ThemeData ScriptableObject
- ✅ ThemeService
- ✅ 文字列アニメーション
- ✅ 数値非表示UI

### カード拡張
- ✅ スコア倍率
- ✅ 崩壊閾値
- ✅ 崩壊システム
- ✅ 自己完結アニメーション

### UI/UX改善
- ✅ 段階的ボタン表示
- ✅ 適切な非表示制御
- ✅ AudioSource エラー対策

### パフォーマンス最適化
- ✅ Subscription管理簡素化
- ✅ メモリリーク対策
- ✅ 重複実行防止
- ✅ 複雑な依存性注入の排除