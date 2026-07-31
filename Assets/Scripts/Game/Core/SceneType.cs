using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ゲーム内のシーン種別を定義するEnum
/// タイポを防ぎ、型安全性を保証する
/// </summary>
public enum SceneType
{
    /// <summary>タイトル画面</summary>
    Title,
    /// <summary>ホーム画面（メインメニュー）</summary>
    Home,
    /// <summary>バトル画面</summary>
    Battle,
    /// <summary>ノベル画面</summary>
    Novel,
    /// <summary>展示モード感謝画面</summary>
    Thanks
}

/// <summary>
/// SceneType列挙型の拡張メソッド
/// SceneTypeとシーン名文字列の変換機能を提供
/// </summary>
public static class SceneTypeExtensions
{
    /// <summary>
    /// SceneTypeとUnityシーン名のマッピング辞書
    /// </summary>
    private static readonly Dictionary<SceneType, string> _sceneNames = new()
    {
        { SceneType.Title, "TitleScene" },
        { SceneType.Home, "HomeScene" },
        { SceneType.Battle, "BattleScene" },
        { SceneType.Novel, "NovelKitScene" },
        { SceneType.Thanks, "ThanksScene" }
    };

    /// <summary>
    /// 内部マッピング辞書への読み取り専用アクセス（SceneUtilityから使用）
    /// </summary>
    internal static IReadOnlyDictionary<SceneType, string> SceneNames => _sceneNames;

    /// <summary>
    /// 指定したSceneTypeが有効なシーン名を持つかチェック
    /// </summary>
    /// <param name="sceneType">シーンタイプ</param>
    /// <returns>有効なシーン名を持つかどうか</returns>
    public static bool IsValid(this SceneType sceneType) => _sceneNames.ContainsKey(sceneType);

    /// <summary>
    /// SceneTypeから対応するUnityシーン名を取得
    /// </summary>
    /// <param name="sceneType">シーンタイプ</param>
    /// <returns>Unityシーン名</returns>
    public static string ToSceneName(this SceneType sceneType)
    {
        if (_sceneNames.TryGetValue(sceneType, out var sceneName)) return sceneName;

        Debug.LogError($"SceneType {sceneType} に対応するシーン名が見つかりません");
        return string.Empty;
    }
}

/// <summary>
/// シーン関連のユーティリティクラス
/// 文字列からSceneTypeへの変換機能を提供
/// </summary>
public static class SceneUtility
{
    /// <summary>
    /// Unityシーン名から対応するSceneTypeを取得
    /// </summary>
    /// <param name="sceneName">Unityシーン名</param>
    /// <returns>シーンタイプ（見つからない場合はTitle）</returns>
    public static SceneType GetSceneType(string sceneName)
    {
        var sceneNames = SceneTypeExtensions.SceneNames;
        var pair = sceneNames.FirstOrDefault(x => x.Value == sceneName);
        if (!pair.Equals(default(KeyValuePair<SceneType, string>))) return pair.Key;

        // デバッグ情報を詳しく出力
        var availableScenes = string.Join(", ", sceneNames.Values);
        Debug.LogWarning($"シーン名 '{sceneName}' に対応するSceneTypeが見つかりません。利用可能なシーン: [{availableScenes}] Titleを返します");
        return SceneType.Title;
    }

    /// <summary>
    /// 現在のシーンのSceneTypeを取得
    /// </summary>
    /// <returns>現在のシーンタイプ</returns>
    public static SceneType GetCurrentSceneType()
    {
        var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return GetSceneType(currentSceneName);
    }
}
