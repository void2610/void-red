using UnityEngine;
using VContainer;
using Void2610.UnityTemplate;

/// <summary>
/// プレイヤーの統計データを追跡・管理するサービスクラス
/// </summary>
public class StatsTracker
{
    private PlayerStats _playerStats;
    
    /// <summary>
    /// 現在の統計データ
    /// </summary>
    public PlayerStats PlayerStats => _playerStats ??= new PlayerStats();
    
    /// <summary>
    /// ゲーム結果を記録
    /// </summary>
    /// <param name="playerWon">プレイヤーが勝利したかどうか</param>
    /// <param name="playerMove">プレイヤーの手</param>
    /// <param name="npcMove">NPCの手</param>
    /// <param name="playerCollapsed">プレイヤーのカードが崩壊したかどうか</param>
    /// <param name="npcCollapsed">NPCのカードが崩壊したかどうか</param>
    public void RecordGameResult(bool playerWon, PlayerMove playerMove, PlayerMove npcMove, bool playerCollapsed, bool npcCollapsed)
    {
        if (!playerMove?.SelectedCard) return;
        
        // 統計データを更新
        PlayerStats.RecordGameResult(playerWon, playerMove, playerCollapsed);
        
        // デバッグログ
        Debug.Log($"ゲーム結果記録: {(playerWon ? "勝利" : "敗北")}, " +
                 $"カード: {playerMove.SelectedCard.CardName}, " +
                 $"プレイスタイル: {playerMove.PlayStyle.ToJapaneseString()}, " +
                 $"崩壊: {(playerCollapsed ? "あり" : "なし")}");
        
        Debug.Log($"現在の統計: {PlayerStats.GetStatsString()}");
    }
    
    /// <summary>
    /// 指定したカードの統計を取得
    /// </summary>
    /// <param name="cardData">カードデータ</param>
    /// <returns>カード統計</returns>
    public CardStats GetCardStats(CardData cardData)
    {
        if (!cardData) return new CardStats();
        return PlayerStats.GetCardStats(cardData.CardId);
    }
    
    /// <summary>
    /// 進化条件をチェック
    /// </summary>
    /// <param name="cardData">チェック対象のカード</param>
    /// <returns>進化可能かどうか</returns>
    public bool CanCardEvolve(CardData cardData)
    {
        return PlayerStats.CheckAllEvolutionConditions(cardData);
    }
    
    /// <summary>
    /// 統計データをリセット（デバッグ用）
    /// </summary>
    public void ResetStats()
    {
        _playerStats = new PlayerStats();
        Debug.Log("統計データをリセットしました");
    }
}