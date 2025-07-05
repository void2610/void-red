using UnityEngine;

/// <summary>
/// ゲームの状態を表すEnum
/// </summary>
public enum GameState
{
    ThemeAnnouncement,  // お題発表
    PlayerCardSelection, // プレイヤーカード選択
    EnemyCardSelection,  // 敵カード選択
    Evaluation,         // 評価
    ResultDisplay       // 勝敗表示
}

/// <summary>
/// カードの出し方を表すenum
/// </summary>
public enum PlayStyle
{
    Hesitation, // 迷い
    Impulse, // 衝動
    Conviction // 確信
}

/// <summary>
/// PlayStyleに関する拡張メソッド
/// </summary>
public static class PlayStyleExtensions
{
    /// <summary>
    /// PlayStyleを日本語の文字列に変換
    /// </summary>
    public static string ToJapaneseString(this PlayStyle playStyle)
    {
        return playStyle switch
        {
            PlayStyle.Hesitation => "迷い",
            PlayStyle.Impulse => "衝動",
            PlayStyle.Conviction => "確信",
            _ => "不明"
        };
    }
    
    /// <summary>
    /// PlayStyleの説明を取得
    /// </summary>
    public static string GetDescription(this PlayStyle playStyle)
    {
        return playStyle switch
        {
            PlayStyle.Hesitation => "慎重に、しかし迷いながら",
            PlayStyle.Impulse => "感情のまま、衝動的に",
            PlayStyle.Conviction => "強い信念を持って、確信的に",
            _ => ""
        };
    }
    
    /// <summary>
    /// PlayStyleのスコア倍率を取得
    /// </summary>
    public static float GetScoreMultiplier(this PlayStyle playStyle)
    {
        return playStyle switch
        {
            PlayStyle.Hesitation => 0.8f,  // 迷い：スコア低減、崩壊率低
            PlayStyle.Impulse => 1.0f,     // 衝動：標準
            PlayStyle.Conviction => 1.3f,  // 確信：スコア増加、崩壊率高
            _ => 1.0f
        };
    }
    
    /// <summary>
    /// PlayStyleの崩壊率倍率を取得
    /// </summary>
    public static float GetCollapseMultiplier(this PlayStyle playStyle)
    {
        return playStyle switch
        {
            PlayStyle.Hesitation => 0.5f,  // 迷い：崩壊率低
            PlayStyle.Impulse => 1.0f,     // 衝動：標準
            PlayStyle.Conviction => 1.8f,  // 確信：崩壊率高
            _ => 1.0f
        };
    }
}