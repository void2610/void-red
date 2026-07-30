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

    [Header("カード閲覧履歴")]
    [SerializeField] private List<string> viewedCardIds = new();

    [Header("ノベル選択結果")]
    [SerializeField] private List<NovelChoiceResult> novelChoiceResults = new();

    [Header("獲得記憶テーマ")]
    [SerializeField] private List<SavedAcquiredTheme> acquiredThemes = new();

    [Header("novel-kit のフラグ / 既読")]
    [SerializeField] private string novelKitState = "";

    // プロパティ
    public int CurrentStep => currentStep;
    public List<NovelChoiceResult> NovelChoiceResults => novelChoiceResults;
    public IReadOnlyList<SavedAcquiredTheme> AcquiredThemes => acquiredThemes;

    /// <summary>
    /// novel-kit の状態スナップショット (NovelSaveSerializer 形式のJSON)
    /// </summary>
    public string NovelKitState
    {
        get => novelKitState;
        set => novelKitState = value;
    }

    /// <summary>
    /// カードが閲覧済みかチェック
    /// </summary>
    /// <param name="cardId">チェックするカードのID</param>
    /// <returns>閲覧済みの場合true</returns>
    public bool IsCardViewed(string cardId) => viewedCardIds.Contains(cardId);

    /// <summary>
    /// 閲覧済みカードIDリストを取得
    /// </summary>
    /// <returns>閲覧済みカードIDのHashSet</returns>
    public HashSet<string> GetViewedCardIds() => new HashSet<string>(viewedCardIds);

    /// <summary>
    /// ノベル選択結果を追加
    /// </summary>
    /// <param name="choiceResult">追加する選択結果</param>
    public void AddNovelChoiceResult(NovelChoiceResult choiceResult) => novelChoiceResults.Add(choiceResult);

    /// <summary>
    /// 特定のシナリオIDの選択結果を取得
    /// </summary>
    /// <param name="scenarioId">シナリオID</param>
    /// <returns>該当する選択結果のリスト</returns>
    public List<NovelChoiceResult> GetChoiceResultsByScenario(string scenarioId) => novelChoiceResults.FindAll(result => result.ScenarioId == scenarioId);

    /// <summary>
    /// 全ての選択結果を取得
    /// </summary>
    /// <returns>全選択結果のリスト</returns>
    public List<NovelChoiceResult> GetAllChoiceResults() => new List<NovelChoiceResult>(novelChoiceResults);

    /// <summary>
    /// 獲得テーマを追加
    /// </summary>
    /// <param name="theme">追加する獲得テーマ</param>
    public void AddAcquiredTheme(SavedAcquiredTheme theme) => acquiredThemes.Add(theme);

    /// <summary>
    /// デバッグ用情報文字列
    /// </summary>
    public string GetDebugInfo() => $"Step: {currentStep}, Results: {resultKeys.Count}entries, ViewedCards: {viewedCardIds.Count}, Choices: {novelChoiceResults.Count}, Themes: {acquiredThemes.Count}";

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

    /// <summary>
    /// カード閲覧を記録
    /// </summary>
    /// <param name="cardId">閲覧したカードのID</param>
    public void RecordCardView(string cardId)
    {
        if (!string.IsNullOrEmpty(cardId) && !viewedCardIds.Contains(cardId)) viewedCardIds.Add(cardId);
    }

    /// <summary>
    /// 獲得テーマリストを更新
    /// </summary>
    /// <param name="themes">獲得テーマリスト</param>
    public void UpdateAcquiredThemes(IEnumerable<SavedAcquiredTheme> themes)
    {
        acquiredThemes.Clear();
        acquiredThemes.AddRange(themes);
    }
}
