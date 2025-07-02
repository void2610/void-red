# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Respond in Japanese and avoid excessive comments. When refactoring, implement clean replacements rather than maintaining backward compatibility.

## Unity Project Overview

void-red is a Unity card game project using Unity 6000.0.37f1 with VContainer for dependency injection, R3 for reactive programming, LitMotion for animations, and UniTask for async operations.

## Core Commands

```bash
# Compile and check errors
./unity-tools/unity-compile.sh trigger . && sleep 3 && ./unity-tools/unity-compile.sh check .

# Check compilation errors only
./unity-tools/unity-compile.sh check .
```

## Architecture: 2層構造MVP Pattern

The project uses a practical 2-layer MVP pattern optimized for Unity development:

```
Presenter Layer (統合制御)
├── PlayerPresenter → カード管理 + UI制御 + ゲームロジック統合
├── UIPresenter     → UI Views統合管理 + イベント処理
└── GameManager     → ゲーム進行制御 (IStartable)
    │
Model Layer (データ管理)
├── PlayerModel     → 精神力などプレイヤー属性のみ
├── HandModel       → 手札データ (R3 Reactive)
├── DeckModel       → デッキデータ
└── Data Objects    → CardData, ThemeData, PlayerMove等
    │
View Layer (UI表示・アニメーション)
├── HandView        → 手札表示 + カードアニメーション
├── CardView        → 個別カード表示 + アニメーション
└── UI Views        → Theme, Announcement, PlayButton等
    │
Services/Logic Layer (サービス・ロジック)
├── CardPoolService → カードプール管理
├── ThemeService    → テーマデータ管理
├── ScoreCalculator → スコア計算ロジック (static)
└── CollapseJudge   → カード崩壊判定ロジック (static)
```

### 責任分離の詳細

**PlayerPresenter (統合プレゼンター)**
- HandModel, DeckModel, PlayerModelの直接管理
- HandViewとの連携によるUI制御
- カード操作ロジック（ドロー、プレイ、選択）
- 精神力管理との統合

**PlayerModel (プレイヤー属性)**
- 精神力のみの管理
- 将来の拡張用（スコア、レベル等）
- ReactivePropertyによるリアクティブ性

**UI系 (UIPresenter + Views)**
- UIPresenterが各Viewを統合管理
- R3 Observableによるイベント通信
- MonoBehaviourパターンの活用

### Key Design Principles
- **適切な粒度**: 過度な分離を避け、実用的なバランス
- **統合プレゼンター**: PlayerPresenterがカード関連を一元管理
- **責任の明確化**: カード管理とプレイヤー属性の分離
- **Unity-First**: MonoBehaviourパターンの自然な活用
- **リアクティブ設計**: R3による状態変更の伝播

## Critical Implementation Details

### VContainer Setup
```csharp
// MainLifetimeScope.cs pattern
builder.RegisterInstance(player);
builder.RegisterInstance(enemy);
builder.Register<CardPoolService>(Lifetime.Singleton);
builder.Register<ThemeService>(Lifetime.Singleton);
builder.RegisterEntryPoint<GameManager>();
builder.RegisterComponentInHierarchy<UIPresenter>();
```

### Game Flow State Machine
GameManager uses GameState enum: ThemeAnnouncement → PlayerCardSelection → EnemyCardSelection → Evaluation → ResultDisplay

### Card Animation System
CardView contains all animations using LitMotion:
- PlayDrawAnimation: Deck to hand
- PlayRemoveAnimation: Normal removal or collapse effect
- PlayArrangeAnimation: Hand positioning
- PlayReturnToDeckAnimation: Hand to deck
- SetHighlight: Selection state

### Reactive Patterns
```csharp
// R3 pattern for card selection
public ReadOnlyReactiveProperty<CardView> SelectedCard => handView?.SelectedCard;

// Event subscription
handView.OnCardSelected.Subscribe(OnCardSelected).AddTo(this);
```

### Score Calculation
```csharp
Score = MatchRate × MentalBet × CardMultiplier
MatchRate = 1.0 + (1.0 - (Distance / √3)) × 0.5
```

## Unity-Specific Guidelines

### Null Checking
```csharp
// ❌ Avoid for Unity objects (causes Rider warnings)
if (cardButton != null)

// ✅ Correct Unity pattern
if (cardButton)
if (!_cardData) return;
```

### Inspector Dependencies
Don't null-check SerializeField components that should be set in Inspector. Let NullReferenceException indicate configuration errors.

### Async Operations
Use UniTask for all async operations. Prefer `.Forget()` for fire-and-forget operations.

## Scene Structure

- **TitleScene**: Entry point with TitleMenu
- **MainScene**: Game scene with Player, Enemy, UIPresenter components

## Dependencies

Key packages from manifest.json:
- VContainer (hadashiA/VContainer)
- R3 (Cysharp/R3)
- UniTask (Cysharp/UniTask)
- LitMotion (AnnulusGames/LitMotion)
- Unity Template (void2610/my-unity-template)

## Development Workflow

1. Make code changes
2. Unity auto-compiles on focus
3. Use unity-compile.sh to verify compilation
4. Test in Unity Editor
5. Build for WebGL deployment

## Common Patterns

### Adding New UI Components
1. Create View class extending MonoBehaviour
2. Add to UIPresenter with [SerializeField]
3. Implement display logic in View
4. Control from UIPresenter

### Modifying Game Flow
1. Update GameState enum if needed
2. Modify state transitions in GameManager.ChangeState()
3. Handle new states appropriately

## Coding Guidelines

### Class Member Declaration Order
Follow this order when declaring class members (based on CardView.cs):

1. **SerializeField** - Inspector設定可能なフィールド（[Header]でグループ化）
2. **public プロパティ** - 外部アクセス可能なプロパティ
3. **定数** - const, static readonly等の定数定義
4. **private フィールド** - 内部状態管理用フィールド
5. **public メソッド** - 外部から呼び出されるメソッド（Initialize, Play~Animation, Set~等）
6. **private メソッド** - 内部処理用メソッド（UpdateDisplay, OnCardClicked等）
7. **Unity イベント関数** - Awake, Start, Update等（呼び出し順序で記述）
8. **クリーンアップ関数** - OnDestroy, Dispose等

```csharp
public class ExampleView : MonoBehaviour
{
    // 1. SerializeField
    [Header("UIコンポーネント")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI text;
    
    // 2. public プロパティ
    public bool IsEnabled { get; private set; }
    public Observable<Unit> OnClicked => _onClicked;
    
    // 3. 定数
    private const float ANIMATION_DURATION = 0.5f;
    private static readonly Vector3 DEFAULT_SCALE = Vector3.one;
    
    // 4. private フィールド
    private readonly Subject<Unit> _onClicked = new();
    private bool _isInitialized;
    
    // 5. public メソッド
    public void Initialize() { }
    public void SetEnabled(bool enabled) { }
    
    // 6. private メソッド
    private void UpdateDisplay() { }
    private void OnButtonClicked() { }
    
    // 7. Unity イベント関数
    private void Awake() { }
    private void Start() { }
    
    // 8. クリーンアップ関数
    private void OnDestroy() { }
}
```