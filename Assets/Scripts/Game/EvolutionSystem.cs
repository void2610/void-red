using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 進化条件のタイプを表すenum
/// </summary>
public enum EvolutionConditionType
{
    /// <summary>特定プレイスタイルでの勝利回数</summary>
    PlayStyleWin,
    /// <summary>特定プレイスタイルでの敗北回数</summary>
    PlayStyleLose,
    /// <summary>総勝利回数</summary>
    TotalWin,
    /// <summary>崩壊回数</summary>
    CollapseCount,
    /// <summary>連続勝利回数</summary>
    ConsecutiveWin,
    /// <summary>総使用回数</summary>
    TotalUse
}

/// <summary>
/// カードの進化条件を定義するクラス
/// </summary>
[Serializable]
public class EvolutionCondition
{
    [Header("進化条件")]
    [SerializeField] private EvolutionConditionType conditionType;
    [SerializeField] private PlayStyle requiredPlayStyle = PlayStyle.Impulse; // PlayStyleWin/PlayStyleLoseの場合のみ使用
    [SerializeField] private int requiredCount;
    
    public EvolutionConditionType ConditionType => conditionType;
    public PlayStyle RequiredPlayStyle => requiredPlayStyle;
    public int RequiredCount => requiredCount;
}

/// <summary>
/// カード毎の統計データ
/// </summary>
[Serializable]
public class CardStats
{
    [SerializeField] private int totalUse;
    [SerializeField] private int totalWin;
    [SerializeField] private int totalLoss;
    [SerializeField] private int collapseCount;
    [SerializeField] private int currentConsecutiveWin;
    [SerializeField] private int maxConsecutiveWin;
    
    // プレイスタイル別勝利回数（拡張性のため辞書で管理）
    [SerializeField] private Void2610.UnityTemplate.SerializableDictionary<PlayStyle, int> playStyleWins = new();
    // プレイスタイル別敗北回数
    [SerializeField] private Void2610.UnityTemplate.SerializableDictionary<PlayStyle, int> playStyleLosses = new();
    
    public int TotalUse => totalUse;
    public int TotalWin => totalWin;
    public int TotalLoss => totalLoss;
    public int CollapseCount => collapseCount;
    public int CurrentConsecutiveWin => currentConsecutiveWin;
    public int MaxConsecutiveWin => maxConsecutiveWin;
    
    public int GetPlayStyleWins(PlayStyle playStyle)
    {
        return playStyleWins.GetValueOrDefault(playStyle, 0);
    }
    
    public int GetPlayStyleLosses(PlayStyle playStyle)
    {
        return playStyleLosses.GetValueOrDefault(playStyle, 0);
    }
    
    /// <summary>
    /// カード使用を記録
    /// </summary>
    public void RecordUse()
    {
        totalUse++;
    }
    
    /// <summary>
    /// 勝利を記録
    /// </summary>
    public void RecordWin(PlayStyle playStyle)
    {
        totalWin++;
        currentConsecutiveWin++;
        if (currentConsecutiveWin > maxConsecutiveWin)
            maxConsecutiveWin = currentConsecutiveWin;
        
        // プレイスタイル別勝利回数を更新
        playStyleWins.TryAdd(playStyle, 0);
        playStyleWins[playStyle]++;
    }
    
    /// <summary>
    /// 敗北を記録
    /// </summary>
    public void RecordLoss(PlayStyle playStyle)
    {
        totalLoss++;
        currentConsecutiveWin = 0;
        
        // プレイスタイル別敗北回数を更新
        playStyleLosses.TryAdd(playStyle, 0);
        playStyleLosses[playStyle]++;
    }
    
    /// <summary>
    /// 崩壊を記録
    /// </summary>
    public void RecordCollapse()
    {
        collapseCount++;
    }
}