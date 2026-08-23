# 12. 実装メモ (プロトタイプ)

`Docs/voidred/01〜10` の仕様を Unity に落とした現状の対応表。コードを読む前の入口として使う。

## コードマップ

| 層 | 場所 | 内容 |
| --- | --- | --- |
| ドメイン (純 C#) | `Assets/Scripts/Game/Auction/` | `AuctionSession` (1 階層の進行と判定) / `BidAI` (NPC の入札予定と反応) / `CompetitionState` (競合) / `EmotionWallet` `EmotionBid` (リソースと入札内訳) / `PersonaState` (人格) / `AuctionParticipant` `WonLot` |
| フェーズ | `Assets/Scripts/Game/Auction/Phases/` | `IAuctionPhase` を実装した 8 フェーズ。並び順は `AuctionFlow.CreatePhases()` |
| データ (SO) | `Assets/Scripts/ScriptableObject/` → `Assets/ScriptableObjects/Auction/` | `MemoryLotData` (記憶) / `ParticipantData` + `BiddingProfile` (参加者と入札傾向) / `FloorData` (階層) / `AllFloorData` (カタログ。Root に登録) |
| Presenter | `Assets/Scripts/Game/Auction/AuctionPresenter.cs` | 「今の状態で実行できるフェーズ」を回すだけの薄い進行役 |
| View | `Assets/Scripts/UI/Auction/` `Assets/Scripts/UI/Lobby/` | `AuctionSceneView` (窓口) の下に旧資産の `AuctionView` / `BidWindowView` / `EmotionResourceDisplayView` (感情ホイール) / `DialoguePhaseView` (4 択 + カットイン + 立ち絵) / `CompetitionView` (天秤 + タイマー) / `BaptismView` / `GameOverView` / `ParticipantIconView`。ロビーは `MemoryCollectionView` `PersonaView` |
| DI | `Assets/Scripts/VContainer/AuctionLifetimeScope.cs` | 進行度 (`AuctionNode`) から階層を決めてセッションを組む。`AuctionStartRequest` (Root) で階層 / seed / 競合秒数を上書きできる |
| 進行 / セーブ | `GameProgressService` `GameStateRepository` `GameSaveData` | 持ち越しリソース / 人格 (統合・コレクション・累計歪み) / 人格崩壊した参加者 ID / ストーリー Step |
| シーン | `Assets/Scenes/AuctionScene.unity` | **BattleScene を複製**して背景 / 机 / ヴィネット / VFX / カメラ / 旧 UI プレハブを引き継ぐ。旧ルール専用のオブジェクトはビルダーが削除する |
| プレハブ | `Assets/Prefabs/NewBattleSceneView/` `Assets/Prefabs/BattleSceneView/` (旧資産) + `Assets/Prefabs/AuctionSceneView/` (新規の小物) | 新規分と配置はエディタツールで生成 (下記) |

## 仕様 → 実装の対応

| 仕様 | 実装 |
| --- | --- |
| 5 人 × 5 ロット、出現順ランダム | `AuctionSession` ctor でロットをシャッフル。参加者は `FloorData.Rivals` 4 名 + 主人公 |
| 勝敗は合計枚数のみ、属性は歪みにだけ効く | `RevealResult` は `SubmittedBid.Total` だけを見る。`EmotionBid.DistortionAgainst` |
| 通常入札は落札できなければ返却、競合は返らない | 通常は `ResolveReveal` で落札者だけ消費。競合は `StartCompetition` で提出分を即消費し、上乗せは `TryRaise` で都度消費 |
| 競合はパス無し・最後の上乗せから一定秒で確定 | `CompetitionState.IsTimedOut(now)`。時刻は Presenter が `Time.time` を渡す |
| 同数のまま時間切れ | **抽選で落札者を決める** (`PickTiedLeader`)。流札は全員 0 枚のときだけ |
| 競合が終わらない問題 | NPC の上乗せは提出額 + `GameConstants.NPC_MAX_RAISE_MARGIN` 枚まで。加えて `CompetitionState` が確定時間の `COMPETITION_HARD_LIMIT_RATIO` 倍で打ち切る |
| 終盤ほど競りが激化する | 無落札のまま残り `DESPERATE_REMAINING_LOTS` ロットになった NPC は上乗せ確率と上限が上がる (`TryNpcRaise`)。競合を避ける性格 (確率 0) は据え置き |
| 各ロットに目玉が 1 つ / 出現順と相関させない | 共鳴値が高いほど NPC の入札が厚くなる (`BidAI.Plan` の `RESONANCE_BID_BONUS_MAX`)。目玉は階層ごとに位置をずらして `AuctionDataBuilder` が配置する |
| 進行が止まる問題 | `PhaseLoopGuard` が同じフェーズの繰り返しを検知。例外時は `AuctionPresenter` がロビーへ戻す |
| 0 枚の参加者は卓から外れる | `AuctionParticipant.CanBid` (所持 0 / 挑発で取りやめ)。**0 枚の入札は不参加扱い** |
| 対話は各コマンド各ライバル 1 ロット 1 回、失敗でもセリフ | `UseDialogue` / `DialogueOutcome`。成功率は `OBSERVE_SUCCESS_RATE` 85 / その他 25 |
| 観察は枚数のみ、逆対話は各ロット各キャラ 1 回 | `DialogueOutcome.ObservedTotal` / `CounterFiredThisLot` |
| 対話への反応はキャラごと | `BiddingProfile` の `BidReaction` (None / 増減 / 大幅増減 / Random / Withdraw / ShiftToNext / PullFromNext) と `ReactionScale` |
| 人格崩壊 = 無落札、主人公はゲームオーバーで同階層やり直し | `FinishLots` で判定。`GameOverView` の「やり直す」は進行度を変えずに `AuctionScene` を再ロード |
| 洗礼: 内訳と歪みを見せ 1 つだけ統合、残りはコレクション | `BaptismView` が旧リザルトの `CardAcquisitionView` (札 + 内訳のスタガー表示) を内包し、札をクリックして選ぶ。`PersonaState.Integrate` で統合。感情状態は入札の主属性 (同数なら記憶の属性) |
| リソース持ち越し + 階層ごとに 8 × 5 補充 | `GameProgressService.PrepareWalletForFloor` (`Refill` は加算)。ゲームオーバーのやり直しで補充が重ならないよう、階層に入ったときの手持ちを控えて復元する |
| 記憶テーマの鮮明化 | 出品の過半 (3/5) を落札したとき洗礼の見出しに鮮明化後テーマを出す (`IsThemeClarified`) |
| ロビー | HomeScene の旧デッキ / 図鑑ボタンを「人格」「記憶コレクション」に転用。進行案内は `HomeView.SetProgressText` |
| 4 階層の獲得必須の記憶 (楽園への鍵) | `MemoryLotData.IsKey`。鍵のある階層で取り逃すと `MissedKey` でゲームオーバー (同階層やり直し)。初期データでは `4-5『楽園への鍵』` |
| ヘルプ | `HelpData/Battle/*` の本文をオークションのルールに書き換え (画像は旧バトルのまま) |
| 入札のマウスホイール操作 | `EmotionBidItemView.OnScroll` で行の上のホイールを +/- に変換 |

## エディタツール (再生成)

`Assets/Scripts/Editor/` の静的メソッドを `uloop execute-dynamic-code --code 'AuctionUiBuilder.BuildPrefabs(); return "ok";'` のように呼ぶ (menu item はセキュリティ設定でブロックされる)。

| メソッド | 生成物 |
| --- | --- |
| `AuctionDataBuilder.Build()` | 参加者 17 / ロット 25 / 階層 5 / `AllFloorData` (既存アセットは上書き。Inspector で調整した値は消える) |
| `AuctionUiBuilder.BuildAll()` | 下記 3 つをまとめて実行 |
| `AuctionUiBuilder.BuildPrefabs()` | 対話フェーズプレハブの組み替え (選択肢を立ち絵の中から出す / カットインを初期非表示) と `ParticipantIcon` `WonLotEntry` `BaptismView` `GameOverView` |
| `AuctionUiBuilder.BuildScene()` | `AuctionScene.unity` (BattleScene 複製 → 旧ルール要素の削除 → 配置・重なり順・ラベル・ボタン名の適用) と Build Settings 登録 |
| `AuctionUiBuilder.BuildLobby()` | ロビーの 2 ウィンドウと進行案内を HomeScene に配置、Root の `AllFloorData` 配線 |

レイアウト調整はビルダーの数値を変えて再実行する方が、手でプレハブを触るより差分が追いやすい。

## 画面の組み立て方 (重要)

旧 UI プレハブは入れ子構造が深く、**シーン上のインスタンスでは親子の付け替えが保存できない**。
そのため配置の変更は次の使い分けで行う。

| やりたいこと | やり方 |
| --- | --- |
| 親子関係を変える / 子を消す | `BuildDialoguePhasePrefab()` のように **プレハブ本体**を `LoadPrefabContents` で開いて編集する |
| 位置・スケール・ラベル・ボタン名 | `BuildScene()` の `ApplyXxxLayout` で **シーン上のインスタンス**に適用する (`SerializedObject` 経由で書く) |
| 重なり順 | `ApplyDrawOrder()` に列挙する |

数値を直してビルダーを流し直すのが基本。プレハブを手で触ると再生成で消える。

## UI 操作を待つときの作法

演出を挟む画面では「フェーズが変わった瞬間」と「操作を受け付ける瞬間」がずれる。
検証がその隙間で操作すると取りこぼすため、次のようにしている。

- 対話フェーズ: `AuctionSceneView.IsWaitingDialogueInput` (`Auction/DialogueReady`) が立ってから押す
- 洗礼: 札が並んだか (`Auction/BaptismReady`)、選択が反映されたか (`Auction/BaptismSelected`) を待つ
- 洗礼 / ゲームオーバーのボタンは Observable を待たず **View 側がフラグで受ける** (`FinishRequested` / `RetryRequested`)。
  Presenter の購読開始前に押されても取りこぼさない
- 検証時は `Auction/Start` の `speed` で演出を早送りする (既定 6 倍)。`ScenarioFragments.ResetProgress()` が毎回 1 倍に戻す

## 検証

- **EditMode テスト** (`Assets/Tests/EditMode/`、57 ケース): ルールの判定、競合が必ず決着すること、フェーズ遷移に穴が無いこと
- **E2E シナリオ**: `Auction/Scenario/*` 11 本、`Lobby/Scenario/*` 3 本。実 UI の導線を見る
- 進行が止まる不具合は製品側で塞ぐ (`CompetitionRunner` / `PhaseLoopGuard` / 例外時のロビー復帰)。テストを減らして避けない`Assets/Scripts/Debug/AuctionScenarios.cs` `LobbyScenarios.cs`
- 操作用コマンド: `Auction/Start` (階層 / seed / 競合秒数を指定して起動)、`Auction/ClickPlus` (同名ボタンが 8 個あるため属性で引く)、`Auction/AutoPlayFloor` (5 ロットを実 UI で自動進行)、`UI/Screenshot`
- seed を固定すると NPC の入札予定・対話の成否・逆対話の発生が決定的になる。`Auction/Scenario/Observe...` は seed 1 に依存している

## 未実装 / 仮置き

- 立ち絵はアルヴ / セリカ / ヴェイルのみ。モブと他キャラは未設定 (UI 側で隠す)
- 数値バランスは全て仮値 (`GameConstants` / 各 `ParticipantData` の `BiddingProfile`)
- ロット 25 件のうち原典にある 8 件以外は「名もなき記憶 n-m」のプレースホルダ
- ノベル側は旧フロー (prologue1 → auction0 → prologue2 → cerica1 → cerica2 → auction1..4) のまま
