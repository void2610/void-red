using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全ゲーム情報を統合したセーブデータクラス
/// </summary>
[Serializable]
public class GameSaveData
{
    [Header("ゲーム進行データ")]
    [SerializeField] private int currentStep = 0;
    [SerializeField] private List<string> resultKeys = new();
    [SerializeField] private List<bool> resultValues = new();

    [Header("novel-kit のフラグ / 既読")]
    [SerializeField] private string novelKitState = "";

    // プロパティ
    public int CurrentStep => currentStep;

    /// <summary>
    /// novel-kit の状態スナップショット (NovelSaveSerializer 形式のJSON)
    /// </summary>
    public string NovelKitState
    {
        get => novelKitState;
        set => novelKitState = value;
    }

    /// <summary>
    /// デバッグ用情報文字列
    /// </summary>
    public string GetDebugInfo() => $"Step: {currentStep}, Results: {resultKeys.Count}entries";

    /// <summary>
    /// ゲーム進行情報を更新
    /// </summary>
    public void UpdateGameProgress(int step, Dictionary<string, bool> results)
    {
        currentStep = step;

        resultKeys.Clear();
        resultValues.Clear();

        foreach (var result in results)
        {
            resultKeys.Add(result.Key);
            resultValues.Add(result.Value);
        }
    }

    /// <summary>
    /// 結果辞書を取得
    /// </summary>
    public Dictionary<string, bool> GetBattleResults()
    {
        var results = new Dictionary<string, bool>();

        for (var i = 0; i < Mathf.Min(resultKeys.Count, resultValues.Count); i++)
            results[resultKeys[i]] = resultValues[i];

        return results;
    }
}
