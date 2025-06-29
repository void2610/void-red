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

## Architecture: Simplified MVP Pattern

The project uses a pragmatic MVP pattern optimized for Unity development:

```
Presenter Layer (High-level control)
├── UIPresenter     → Manages all UI components
└── GameManager     → Controls game flow (IStartable)
    │
View Layer (Display + Self-contained logic)  
├── CardView        → Card display + animations
├── HandView        → Hand management + arrangement
└── UI Views        → Theme, Announcement, PlayButton, etc.
    │
Model Layer (Pure data)
├── CardData        → ScriptableObject card definitions
├── PlayerMove      → Turn data (CardData, PlayStyle, bet)
└── ThemeData       → ScriptableObject theme definitions
```

### Key Design Principles
- **Appropriate Granularity**: Avoid over-separation; Views contain their own animations
- **Direct References**: HandView is directly referenced by BasePlayer (no complex DI)
- **Unity-First**: Leverage MonoBehaviour patterns naturally
- **Data Purity**: Models (PlayerMove) hold data, not UI references

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