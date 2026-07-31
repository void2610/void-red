# ノベルシナリオの Excel → novel-kit 移行

`Assets/StreamingAssets/Dialog.xlsx` の各シートを `Assets/Resources/Scenarios/<NodeId>.rb` へ移行した際の対応関係と、移行できなかった箇所の記録。

## シート ↔ ノード

シナリオのファイル名は `StoryNode.NodeId` と一致させる必要がある（`NovelKitStarter` が `node.NodeId` をシナリオキーとして再生するため）。

| シート | ノード | 備考 |
|---|---|---|
| prologue1 | `NovelNode("prologue1")` | |
| prologue2 | `NovelNode("prologue2")` | |
| cerica1 | `NovelNode("cerica1")` | |
| cerica2 | `NovelNode("cerica2")` | |
| ending | なし | 現行の進行グラフから未参照。`DemoEnding` は Thanks シーンへ遷移しノベルを再生しない |
| test | なし | 旧実装の動作確認用。移行対象外 |

## 列の対応

| Excel の ParamType | novel-kit | 備考 |
|---|---|---|
| `CharacterImageName` | `portrait` | 値 `Alv/Mask` → `Character/Alv/Mask.png` |
| `BackgroundImageName` | `bg` | 値 `LobbyDark` → `Background/LobbyDark.jpg`。拡張子は資産ごとに png / jpg が混在する |
| `SEClipName` | `se` | |
| `CustomCharSpeed` | 未対応 | |
| `AutoAdvance` | 未対応 | |
| `GetItem` | 未対応 | prologue1 に 1 箇所。TODO コメントで位置のみ保持 |
| `CardChoice` | 未対応 | prologue2 に 1 箇所。札画像なしの素の `choose` で代替 |

`CharacterImageName` は「話者」ではなく「画面に出ている立ち絵」を指す。主人公のセリフ行にも相手の立ち絵が指定されるため、`say` の第 3 引数ではなく `portrait` で切り替える。

## 話者 ID

カタログ (`Assets/ScriptableObjects/NovelKit/CharacterCatalog.asset`) の ID は `player` / `cerica` / `alv` の 3 つ。Excel の話者欄は表示名が直接書かれており、正体を伏せる演出で同一人物が複数の表記を持つため `display_as` で上書きする。

| Excel の話者欄 | ID | display_as |
|---|---|---|
| `■■■` | `player` | `■■■` |
| `主人公` | `player` | なし（カタログの「主人公」） |
| `???` (prologue) | `alv` | `???` |
| `???` (cerica) | `cerica` | `???` |
| `アルヴ` / `セリカ` | `alv` / `cerica` | なし |

数字だけの話者欄 (`92`, `103` 等) はテキストが空で、行番号の書き損じと判断して移行していない。

## 移行時に判断した箇所

- prologue2 で主人公の表記が `主人公` と `■■■` に割れている。原文どおり保持したため画面上も混在する
- cerica2 の 1 行だけ主人公のセリフに `???` が指定されている。原文の表示を保つため `player` + `display_as: '???'` とした
- 旧実装は `ending` シナリオ完走時に Steam ストアページを開いていた (`NovelPresenter`)。この副作用は移行していない
