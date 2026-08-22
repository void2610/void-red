# 11. 旧 UI 資産の目録 (アーカイブ)

新オークションシステムへ作り直すにあたり、**旧ルール (カードバトル + 旧オークション) の実装を全削除**した。
本ファイルはそこに何があったかの記録であり、必要になったら復元するための索引。

削除後の状態で **コンパイルは通っている (Error 0 / Warning 0)**。

## 復元方法

削除直前の全ファイルはタグ **`legacy/card-battle`** に残してある。

```bash
# 目録を見る
git ls-tree -r --name-only legacy/card-battle -- Assets/Scripts/UI Assets/Prefabs

# 1 ファイルだけ中身を見る (作業ツリーを汚さない)
git show legacy/card-battle:Assets/Scripts/UI/Auction/CompetitionView.cs

# 1 ファイルを復元する (.meta も忘れずに)
git checkout legacy/card-battle -- Assets/Scripts/UI/Auction/CompetitionView.cs{,.meta}

# ディレクトリごと復元する
git checkout legacy/card-battle -- Assets/Prefabs/NewBattleSceneView
```

> プレハブを復元する場合、参照している C# スクリプトも一緒に復元しないと missing script になる。
> `.meta` の GUID はタグ側と同一なので、スクリプトとプレハブを揃えて戻せば参照は復旧する。

## 削除範囲

| 分類 | 内容 |
| --- | --- |
| ロジック | `Game/Logic/` `Game/Models/` (ディレクトリごと)、`Game/Presenters/` の Player / Enemy 系 |
| サービス | `AuctionJudge` `CardPoolService` `CardViewFactory` `EnemyProgressService` `MemoryEmotionCalculator` `RewardCalculator` |
| データ | カード / 敵 / 旧オークション / チュートリアル / 記憶テーマ の ScriptableObject 定義 |
| セーブ | `AcquiredTheme` `SavedAcquiredTheme` `MemoryProgressData` `PlayerProgressData` と、`GameSaveData` のカード閲覧履歴 / 獲得テーマ欄 |
| UI | `UI/Auction/` `UI/Battle/` (ディレクトリごと)、`UI/Views/` のカード・デッキ・敵・テーマ・チュートリアル系 |
| DI | `BattleLifetimeScope`、`RootLifetimeScope` / `HomeLifetimeScope` の該当登録 |
| シーン | `Assets/Scenes/BattleScene.unity` |
| プレハブ | `Prefabs/BattleSceneView/` `Prefabs/NewBattleSceneView/` (ディレクトリごと)、`Prefabs/HomeSceneView/` のカード系 4 件、`Prefabs/Button/AuctionNormalButton.prefab` |
| エフェクト | `MentalPowerEffectController` (精神力 = 旧バトル依存) |

## 削除に伴う既知の未解決事項

新オークションシステムを実装する際に必ず対処が必要なもの。

1. **`GameProgressService.GetNextNode()` が `BattleNode("alv")` を返す**。`SceneType.Battle` に対応する
   `BattleScene` は削除済みのため、ストーリーを step 1 まで進めるとシーン読込みに失敗する。
   新しいオークションシーンを作った時点で差し替える。
2. **`HomeScene` にカード図鑑 / デッキ / チュートリアル関連の missing 参照が残る**。
   `HomeView` からフィールドを外したため、シーン側の不要オブジェクトは手で掃除する必要がある。
   `HomeView` のデッキ / 図鑑ボタンは `Initialize()` で無効化してある。
3. **`Assets/Resources` 等に旧 ScriptableObject の `.asset` が残っており missing script になる**。
   新データ構造を決めてから整理する。
4. **セーブデータの形式が変わった**。旧セーブに含まれていたカード閲覧履歴と獲得テーマは読み捨てられる。

## 新ルールで参考になるもの

作り直しの際、まずこの 6 つを読むとよい。旧ルールでも同じ役割が存在した部分。

| 旧ファイル | 新ルールでの相当 |
| --- | --- |
| `UI/Auction/CompetitionView.cs` | 競合フェーズ UI。ただし旧実装は 1v1 前提、新ルールは最大 5 人同時競合 |
| `UI/Auction/BidWindowView.cs` | 入札ウィンドウ |
| `UI/Auction/EmotionResourceDisplayView.cs` | 感情リソースの属性別表示・選択 |
| `UI/Auction/DialoguePhaseView.cs` ほか `Dialogue*` | 対話フェーズ (選択肢 / カットイン / 立ち絵) |
| `UI/Auction/BalanceTiltController.cs` | 天秤の傾き演出。魂 = 天秤という設定と直結する |
| `UI/Auction/AuctionView.cs` | オークション画面全体のフェーズ統合 (452 行) |

## 旧 UI スクリプト一覧

### `UI/Animation/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `CardButtonAnimation.cs` | 54 | — |
| `HomeButtonAnimation.cs` | 64 | — |
| `NormalButtonAnimation.cs` | 58 | — |

### `UI/Auction/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `AcquiredCardTextView.cs` | 13 | 獲得カード一覧の個別アイテム表示 |
| `AcquiredCardView.cs` | 16 | 獲得カード一覧の個別カード表示 |
| `AuctionCardView.cs` | 52 | — |
| `AuctionView.cs` | 453 | — |
| `BalanceTiltController.cs` | 96 | 天秤の傾きをLitMotionで制御する / tilt範囲: -1（敵側が重い）～ 0（水平）～ +1（プレイヤー側が重い） / 実際の天秤と同様に、入札が多い方が下がる |
| `BidAmountIconView.cs` | 18 | — |
| `BidWindowView.cs` | 74 | — |
| `CardAcquisitionView.cs` | 106 | — |
| `CardBidInfoView.cs` | 88 | — |
| `CompetitionView.cs` | 102 | 競合フェーズのView / 引き分け時のリアルタイム上乗せUIを管理 |
| `DeckSlotView.cs` | 96 | — |
| `DialogueChoicesView.cs` | 71 | — |
| `DialogueCutInView.cs` | 102 | — |
| `DialoguePhaseView.cs` | 83 | — |
| `DialoguePortraitView.cs` | 107 | — |
| `DragLineView.cs` | 80 | カードドラッグ時に始点から終点への曲線を描画するコンポーネント |
| `DraggableCardView.cs` | 223 | — |
| `EmotionResourceDisplayView.cs` | 123 | — |
| `EmotionResourceItemView.cs` | 16 | — |
| `ResourceRewardView.cs` | 127 | — |
| `RewardPhaseView.cs` | 42 | 報酬フェーズのコーディネーター / CardAcquisitionView → ResourceRewardView の順にサブViewを呼び出す |

### `UI/Battle/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `BattleCardNumberLabel.cs` | 19 | バトル用カード数字の表示を担当する |
| `CardBattleView.cs` | 351 | カードバトル画面のView / プレイヤーはD&Dでカードを場に出し、敵カードが開示されて横並びに表示される |
| `CoinFlipView.cs` | 56 | — |
| `DeckSelectionView.cs` | 274 | デッキ選択画面のView / 獲得カードをドラッグ&ドロップして3枚のデッキを構成する |
| `DiamondIndicatorView.cs` | 51 | — |
| `SelectableBattleCardView.cs` | 38 | 対象選択用のクリック専用バトルカードView |
| `SkillButtonView.cs` | 44 | スキルボタンのView / Canvas直下に1つだけ配置し、DeckSelection/CardBattle両フェーズで共有する |
| `TargetCardSelectionView.cs` | 95 | 対象選択が必要なスキル用のカード選択モーダル |

### `UI/Exhibit/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `ExhibitOverlayView.cs` | 74 | — |
| `ThanksPresenter.cs` | 52 | 展示モード感謝画面のPresenter / クリック/タッチでタイトルに戻る |

### `UI/Home/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `HomePresenter.cs` | 116 | ホーム画面のPresenter / ビジネスロジックとイベント処理を担当 |
| `HomeView.cs` | 82 | ホーム画面のView / UI要素の参照とイベントの公開を担当 |

### `UI/Main/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `BattleKeyBindings.cs` | 94 | — |
| `BattleUIPresenter.cs` | 217 | UIのビジネスロジックとイベント処理を担当するPresenterクラス / VContainerで依存性注入される |
| `PausePresenter.cs` | 82 | ポーズ機能を管理するPresenterクラス / VContainerで依存性注入される |
| `TutorialKeyBindings.cs` | 15 | — |
| `TutorialPresenter.cs` | 67 | チュートリアル機能の制御を担当するPresenterクラス / AllTutorialDataの管理とTutorialViewへの指示を行う |

### `UI/Navigation/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `ForceSelectable.cs` | 9 | CanvasGroupが有効になった時、このクラスがアタッチされているオブジェクトがフォーカスされる |
| `InputGuideLabel.cs` | 201 | — |
| `MouseHoverUISelector.cs` | 35 | — |
| `SafeNavigationManager.cs` | 107 | — |

### `UI/Presenters/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `HelpPresenter.cs` | 109 | HelpViewとAllHelpDataの橋渡しを行うPresenterクラス |

### `UI/Title/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `TitleIdlePVPresenter.cs` | 54 | タイトル画面でのアイドル時PV再生を制御するPresenter |
| `TitlePVView.cs` | 56 | — |
| `TitlePresenter.cs` | 140 | タイトル画面のPresenter / ビジネスロジックとイベント処理を担当 |
| `TitleView.cs` | 44 | タイトル画面のView / UI要素の参照とイベントの公開を担当 |
| `VersionText.cs` | 15 | — |

### `UI/Views/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `AnnouncementView.cs` | 163 | — |
| `BaseCardView.cs` | 76 | CardView と DeckCardView の共通基底クラス / カードの基本表示ロジックを提供 |
| `BasePhaseView.cs` | 26 | — |
| `BaseWindowView.cs` | 176 | — |
| `BattlePauseView.cs` | 89 | — |
| `BlackOverlayView.cs` | 80 | — |
| `ButtonSelectionGlow.cs` | 50 | — |
| `CardDetailView.cs` | 31 | カード詳細情報を表示するモーダルViewクラス / 既存のDeckCardViewを活用してカード表示の一貫性を保つ |
| `CardLibraryView.cs` | 234 | カード図鑑を表示するViewクラス / ゲーム内の全カードを閲覧できる |
| `CardView.cs` | 153 | カードの表示と基本的なロジックを担当するViewクラス / 元のCard.csをベースに選択機能とアニメーション機能を追加した簡略化されたMVPパターン |
| `ConfirmationDialogView.cs` | 106 | 確認ダイアログの表示を担当するViewクラス |
| `DeckCardView.cs` | 128 | デッキ表示専用の簡易カードViewクラス / CardViewのサブセットで表示のみを担当 |
| `DeckView.cs` | 262 | デッキ内容を表示するViewクラス / カテゴリ別にカードを表示し、統計情報も提供 |
| `EmotionGaugeView.cs` | 106 | 各感情タイプのバーを表示するView |
| `EnemyFaceView.cs` | 13 | 敵の顔アイコンを表示するView |
| `EnemyView.cs` | 63 | — |
| `EyeBlinkTransitionView.cs` | 74 | — |
| `GaugeView.cs` | 20 | ImageのfillAmountを使用したゲージ表示View |
| `HelpButtonView.cs` | 19 | — |
| `HelpView.cs` | 87 | ヘルプ画面を表示するViewクラス |
| `IPauseView.cs` | 15 | ポーズビューの共通インターフェース |
| `NarrationView.cs` | 130 | — |
| `PauseButtonView.cs` | 19 | — |
| `PauseView.cs` | 51 | ポーズ画面を表示するViewコンポーネント |
| `PersonalityLogButtonView.cs` | 20 | — |
| `PlayerFaceView.cs` | 32 | プレイヤーの顔アイコンと状態ゲージを表示するView |
| `SettingButtonView.cs` | 27 | — |
| `SimpleTutorialWindowView.cs` | 132 | — |
| `TextProgressController.cs` | 168 | テキスト表示の進行状態を管理するpure C#クラス / タイピングアニメーションと次への進行待機を制御する |
| `ThemeDetailView.cs` | 35 | テーマ詳細情報を表示するモーダルViewクラス |
| `ThemeView.cs` | 94 | テーマ表示を担当するViewクラス / マウスオーバー時にキーワードを表示 |
| `TutorialView.cs` | 114 | — |

### `UI/Views/MemoryGrowth/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `MemoryCardItemView.cs` | 37 | CardViewをラップして円形配置用の機能を提供するView |
| `MemoryDetailView.cs` | 276 | キャラクター記憶ビュー / 右側パネルでキャラクターとカードを表示 |
| `MemoryGrowthView.cs` | 45 | 記憶育成フェーズのメインビュー |
| `MemoryThemeListItemView.cs` | 73 | 記憶テーマリストのアイテムビュー / 左側パネルの各テーマ表示を担当 |
| `MemoryThemeListView.cs` | 77 | 記憶テーマリストビュー / 左側パネルの獲得済みテーマ一覧を表示 |

### `UI/Views/Settings/`

| ファイル | 行数 | 概要 |
| --- | --- | --- |
| `SettingsWindowView.cs` | 62 | 設定ウィンドウのラッパー / BaseWindowViewを継承し、入力イベントとSettingsPresenterのイベントでShow/Hideを制御 |
## 旧プレハブ一覧

**`Prefabs/BattleSceneView/`**

- `AnnouncementView.prefab`
- `BlackBackground.prefab`
- `DialogueChoiceButton.prefab`
- `EnemyView.prefab`
- `EyeBlinkTransitionView.prefab`
- `PauseButtonView.prefab`
- `PersonalityLogButtonView.prefab`
- `PlayerNarrationView.prefab`
- `ThemeDetailView.prefab`
- `ThemeView.prefab`
- `TutorialView.prefab`

**`Prefabs/Button/`**

- `AuctionNormalButton.prefab`
- `HelpButton.prefab`
- `HomeButton.prefab`
- `MenuButton.prefab`
- `NextPrevButton.prefab`
- `NormalButton.prefab`
- `PauseButton.prefab`
- `SettingButton.prefab`
- `TextButton.prefab`
- `TitleButton.prefab`

**`Prefabs/HomeSceneView/`**

- `CardDetailView.prefab`
- `CardLibraryView.prefab`
- `DeckView.prefab`
- `HelpView.prefab`
- `SimpleTutorialView.prefab`

**`Prefabs/NewBattleSceneView/`**

- `AuctionCardView.prefab`
- `AuctionView.prefab`
- `BalanceView.prefab`
- `BattlePauseView.prefab`
- `BidAmountIconView.prefab`
- `BidWindowView.prefab`
- `CardBidInfoView.prefab`
- `CardView.prefab`
- `CharacterPortrait.prefab`
- `CompetitionView.prefab`
- `EmotionGaugeView.prefab`
- `EnemyFaceView.prefab`
- `Gauge.prefab`
- `PlayerFaceView.prefab`

**`Prefabs/NewBattleSceneView/Battle/`**

- `BattleCardNumberLabel.prefab`
- `CardBattleView.prefab`
- `CoinFlipView.prefab`
- `DeckSelectionView.prefab`
- `DiamondIndicatorView.prefab`
- `SelectableBattleCardView.prefab`
- `SkillButtonView.prefab`
- `TargetCardSelectionView.prefab`

**`Prefabs/NewBattleSceneView/DeckSelection/`**

- `CardContainer.prefab`
- `DeckSlotView.prefab`
- `DraggableCardView.prefab`

**`Prefabs/NewBattleSceneView/DialoguePhase/`**

- `DeckCardView.prefab`
- `DialogueChoicesView.prefab`
- `DialogueCutInView.prefab`
- `DialoguePhaseView.prefab`
- `DialoguePortraitView.prefab`
- `EnemyNarrationView.prefab`

**`Prefabs/NewBattleSceneView/EmotionResourceDisplay/`**

- `EmotionResourceDisplayView.prefab`
- `EmotionResourceItemView.prefab`

**`Prefabs/NewBattleSceneView/MemoryGroth/`**

- `MemoryCardItemView.prefab`
- `MemoryDetailView.prefab`
- `MemoryGrowthView.prefab`
- `MemoryThemeListItemView.prefab`

**`Prefabs/NewBattleSceneView/RewardPhase/`**

- `AcquiredCardTextView.prefab`
- `AcquiredCardView.prefab`
- `CardAcquisitionView.prefab`
- `ResourceRewardView.prefab`
- `RewardPhaseView.prefab`

**`Prefabs/Novel/`**

- `ChoiceButton.prefab`
- `NovelKitView.prefab`

**`Prefabs/Particle/`**

- `ItemGetParticle.prefab`
- `MentalFire.prefab`

**`Prefabs/Root/`**

- `BGMManager.prefab`
- `ConfirmationDialog.prefab`
- `DebugComponents.prefab`
- `EventSystem.prefab`
- `ExhibitOverlayView.prefab`
- `Global Volume.prefab`
- `InputGuideLabel.prefab`
- `PauseView.prefab`
- `RootLifetimeScope.prefab`
- `SEManager.prefab`
- `VersionText.prefab`

**`Prefabs/Settings/`**

- `SettingButton.prefab`
- `SettingContentContainer.prefab`
- `SettingEnum.prefab`
- `SettingSlider.prefab`
- `SettingTabButton.prefab`
- `SettingTitle.prefab`
- `SettingsPanel.prefab`

**`Prefabs/Title/`**

- `ReviewForm.prefab`
- `StorePageQR.prefab`