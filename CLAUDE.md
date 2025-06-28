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

### VContainer + R3 構成
- VContainer: 依存性注入
- R3: リアクティブプログラミング

### カードゲーム設計
- CardData: ScriptableObjectでカード情報を定義
- Card: MonoBehaviourでUIと効果を統合
- CardEffect: 許し、拒絶、空白の3つのfloatパラメータ
- PlayerMove: プレイヤーの手（カード、プレイスタイル、精神ベット）をまとめたクラス

### nullチェックのガイドライン
- SerializeFieldやInspectorで事前に設定すべき要素（GameManager、Player、Enemy、UIManager等）に対するnullチェックは避ける
- これらの要素がnullの場合は設定ミスであり、NullReferenceExceptionが発生して然るべき
- nullチェックを行うのは、ランタイム中に動的に設定される要素のみ
  - 例：動的に生成/破棄されるオブジェクト、ネットワークから取得するデータ、ユーザー入力によって変化する値など
- `?.` や `??` 演算子の過剰な使用は避け、コードの可読性を重視する

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