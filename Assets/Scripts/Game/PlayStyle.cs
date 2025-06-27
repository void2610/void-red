using UnityEngine;

/// <summary>
/// カードの出し方を表すenum
/// </summary>
public enum PlayStyle
{
    /// <summary>
    /// 迷い
    /// </summary>
    Hesitation = 0,
    
    /// <summary>
    /// 衝動
    /// </summary>
    Impulse = 1,
    
    /// <summary>
    /// 確信
    /// </summary>
    Conviction = 2
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
}