# 12. 実装メモ (プロトタイプ)

`Docs/voidred/01〜10` の仕様を Unity に落とした現状の対応表。コードを読む前の入口として使う。

## コードマップ

| 層 | 場所 | 内容 |
| --- | --- | --- |
| ドメイン (純 C#) | `Assets/Scripts/Game/Auction/` | `AuctionSession` (1 階層の進行と判定) / `BidAI` (NPC の入札予定と反応) / `CompetitionState` (競合) / `EmotionWallet` `EmotionBid` (リソースと入札内訳) / `PersonaState` (人格) / `AuctionParticipant` `WonLot` |
| データ (SO) | `Assets/Scripts/ScriptableObject/` → `Assets/ScriptableObjects/Auction/` | `MemoryLotData` (記憶) / `ParticipantData` + `BiddingProfile` (参加者と入札傾向) / `FloorData` (階層) / `AllFloorData` (カタログ。Root に登録) |
| Presenter | `Assets/Scripts/Game/Auction/AuctionPresenter.cs` | テーマ公開 → 5 ロット (対話 → 入札 → 開示 → 競合) → 洗礼 or ゲームオーバー |
| View | `Assets/Scripts/UI/Auction/` `Assets/Scripts/UI/Lobby/` | `AuctionView` (ルート) と各パネル。ロビーは `MemoryCollectionView` `PersonaView` |
| DI | `Assets/Scripts/VContainer/AuctionLifetimeScope.cs` | 進行度 (`AuctionNode`) から階層を決めてセッションを組む。`AuctionStartRequest` (Root) で階層 / seed / 競合秒数を上書きできる |
| 進行 / セーブ | `GameProgressService` `GameStateRepository` `GameSaveData` | 持ち越しリソース / 人格 (統合・コレクション・累計歪み) / 人格崩壊した参加者 ID / ストーリー Step |
| シーン | `Assets/Scenes/AuctionScene.unity` | HomeScene を複製して Canvas / 設定パネルを流用。`AuctionView` プレハブ + `AuctionLifetimeScope` |
| プレハブ | `Assets/Prefabs/AuctionSceneView/` `Assets/Prefabs/HomeSceneView/` | すべてエディタツールで生成 (下記) |

## 仕様 → 実装の対応

| 仕様 | 実装 |
| --- | --- |
| 5 人 × 5 ロット、出現順ランダム | `AuctionSession` ctor でロットをシャッフル。参加者は `FloorData.Rivals` 4 名 + 主人公 |
| 勝敗は合計枚数のみ、属性は歪みにだけ効く | `RevealResult` は `SubmittedBid.Total` だけを見る。`EmotionBid.DistortionAgainst` |
| 通常入札は落札できなければ返却、競合は返らない | 通常は `ResolveReveal` で落札者だけ消費。競合は `StartCompetition` で提出分を即消費し、上乗せは `TryRaise` で都度消費 |
| 競合はパス無し・最後の上乗せから一定秒で確定 | `CompetitionState.IsTimedOut(now)`。時刻は Presenter が `Time.time` を渡す |
| 同数のまま時間切れ | **抽選で落札者を決める** (`PickTiedLeader`)。流札は全員 0 枚のときだけ |
| 0 枚の参加者は卓から外れる | `AuctionParticipant.CanBid` (所持 0 / 挑発で取りやめ)。**0 枚の入札は不参加扱い** |
| 対話は各コマンド各ライバル 1 ロット 1 回、失敗でもセリフ | `UseDialogue` / `DialogueOutcome`。成功率は `OBSERVE_SUCCESS_RATE` 85 / その他 25 |
| 観察は枚数のみ、逆対話は各ロット各キャラ 1 回 | `DialogueOutcome.ObservedTotal` / `CounterFiredThisLot` |
| 対話への反応はキャラごと | `BiddingProfile` の `BidReaction` (None / 増減 / 大幅増減 / Random / Withdraw / ShiftToNext / PullFromNext) と `ReactionScale` |
| 人格崩壊 = 無落札、主人公はゲームオーバーで同階層やり直し | `FinishLots` で判定。`GameOverView` の「やり直す」は進行度を変えずに `AuctionScene` を再ロード |
| 洗礼: 内訳と歪みを見せ 1 つだけ統合、残りはコレクション | `BaptismView` + `PersonaState.Integrate`。感情状態は入札の主属性 (同数なら記憶の属性) |
| リソース持ち越し + 階層ごとに 8 × 5 補充 | `GameProgressService.PrepareWalletForFloor` (`Refill` は加算) |
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
| `AuctionUiBuilder.BuildPrefabs()` | `ParticipantSlot` `EmotionBidItem` `WonLotEntry` `AuctionView` |
| `AuctionUiBuilder.BuildScene()` | `AuctionScene.unity` (HomeScene 複製 → 不要物削除 → View とスコープ配置) と Build Settings 登録 |
| `AuctionUiBuilder.BuildLobby()` | ロビーの 2 ウィンドウと進行案内を HomeScene に配置、Root の `AllFloorData` 配線 |

レイアウト調整はビルダーの数値を変えて再実行する方が、手でプレハブを触るより差分が追いやすい。

## 検証

- `[LiminalScenario]`: `Auction/Scenario/*` (11 本) と `Lobby/Scenario/*` (3 本)。Test Runner (`uloop run-tests --test-mode PlayMode`) でも全件通る。`Assets/Scripts/Debug/AuctionScenarios.cs` `LobbyScenarios.cs`
- 操作用コマンド: `Auction/Start` (階層 / seed / 競合秒数を指定して起動)、`Auction/ClickPlus` (同名ボタンが 8 個あるため属性で引く)、`Auction/AutoPlayFloor` (5 ロットを実 UI で自動進行)、`UI/Screenshot`
- seed を固定すると NPC の入札予定・対話の成否・逆対話の発生が決定的になる。`Auction/Scenario/Observe...` は seed 1 に依存している

## 未実装 / 仮置き

- 参加者の立ち絵・記憶の画像は未設定 (属性色の四角で代用)
- 数値バランスは全て仮値 (`GameConstants` / 各 `ParticipantData` の `BiddingProfile`)
- ロット 25 件のうち原典にある 8 件以外は「名もなき記憶 n-m」のプレースホルダ
- ノベル側は旧フロー (prologue1 → auction0 → prologue2 → cerica1 → cerica2 → auction1..4) のまま
