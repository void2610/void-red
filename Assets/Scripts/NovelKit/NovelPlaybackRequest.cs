/// <summary>
/// ストーリー進行とは独立にノベルシーンで再生するシナリオの予約 (回想 / デバッグ再生用)
/// NovelKitStarter が次回のシーン開始時に 1 回だけ消費し、再生後は進行度を進めずホームへ戻る
/// </summary>
public sealed class NovelPlaybackRequest
{
    public string ScenarioKey { get; private set; }

    public bool HasRequest => !string.IsNullOrEmpty(ScenarioKey);

    public void Set(string scenarioKey) => ScenarioKey = scenarioKey;

    /// <summary>予約を取り出して消費する (未予約なら null)</summary>
    public string Consume()
    {
        var key = ScenarioKey;
        ScenarioKey = null;
        return key;
    }
}
