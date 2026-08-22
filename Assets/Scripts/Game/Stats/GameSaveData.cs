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

    [Header("感情リソース (EmotionType の順)")]
    [SerializeField] private List<int> emotionCounts = new();

    [Header("人格")]
    [Tooltip("-1 で未定。それ以外は EmotionType の値")]
    [SerializeField] private int emotionState = -1;
    [SerializeField] private List<string> integratedLotIds = new();
    [SerializeField] private List<string> collectionLotIds = new();
    [SerializeField] private int totalDistortion = 0;

    [Header("人格崩壊した参加者")]
    [SerializeField] private List<string> collapsedParticipantIds = new();

    [Header("novel-kit のフラグ / 既読")]
    [SerializeField] private string novelKitState = "";

    public int CurrentStep
    {
        get => currentStep;
        set => currentStep = value;
    }

    public List<int> EmotionCounts => emotionCounts;
    public int EmotionState
    {
        get => emotionState;
        set => emotionState = value;
    }
    public List<string> IntegratedLotIds => integratedLotIds;
    public List<string> CollectionLotIds => collectionLotIds;
    public int TotalDistortion
    {
        get => totalDistortion;
        set => totalDistortion = value;
    }
    public List<string> CollapsedParticipantIds => collapsedParticipantIds;

    /// <summary>
    /// novel-kit の状態スナップショット (NovelSaveSerializer 形式のJSON)
    /// </summary>
    public string NovelKitState
    {
        get => novelKitState;
        set => novelKitState = value;
    }

    public string GetDebugInfo() => $"Step: {currentStep}, Emotions: [{string.Join(",", emotionCounts)}], State: {emotionState}, Integrated: {integratedLotIds.Count}, Collection: {collectionLotIds.Count}, Collapsed: {collapsedParticipantIds.Count}";
}
