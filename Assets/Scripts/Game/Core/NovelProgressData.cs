using System.Collections.Generic;

/// <summary>
/// ノベル進行データを保持するクラス
/// ノベル選択結果を管理
/// </summary>
public class NovelProgressData
{
    /// <summary>
    /// novel-kit のフラグ / 既読スナップショット (NovelSaveSerializer 形式のJSON)
    /// </summary>
    public string NovelKitState { get; set; } = "";

    /// <summary>
    /// ノベル選択結果のリスト
    /// </summary>
    private readonly List<NovelChoiceResult> _choiceResults;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public NovelProgressData()
    {
        _choiceResults = new List<NovelChoiceResult>();
    }

    /// <summary>
    /// 選択結果を記録
    /// </summary>
    public void RecordChoice(NovelChoiceResult choiceResult) => _choiceResults.Add(choiceResult);

    public List<NovelChoiceResult> GetAllChoiceResults() => new List<NovelChoiceResult>(_choiceResults);

    /// <summary>
    /// 特定のシナリオの選択結果を取得
    /// </summary>
    /// <param name="scenarioId">シナリオID</param>
    /// <returns>該当する選択結果のリスト</returns>
    public List<NovelChoiceResult> GetChoiceResultsByScenario(string scenarioId) => _choiceResults.FindAll(result => result.ScenarioId == scenarioId);

    public void LoadFrom(List<NovelChoiceResult> getAllChoiceResults)
    {
        _choiceResults.Clear();
        _choiceResults.AddRange(getAllChoiceResults);
    }

    /// <summary>
    /// リセット
    /// </summary>
    public void Reset()
    {
        _choiceResults.Clear();
        NovelKitState = "";
    }
}
