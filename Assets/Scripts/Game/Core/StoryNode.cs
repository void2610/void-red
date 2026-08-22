/// <summary>
/// ストーリー進行における分岐ノードを表す抽象基底クラス
/// </summary>
public abstract class StoryNode
{
    public string NodeId { get; protected set; }

    /// <summary>
    /// このノード終了後にホームに戻るかどうか
    /// true: ホームに戻る、false: 次のノードへ直接遷移
    /// </summary>
    public bool ReturnToHome { get; set; } = true;
}

/// <summary>
/// 記憶オークション 1 階層分を表すノード
/// </summary>
public class AuctionNode : StoryNode
{
    public int FloorIndex { get; }

    public AuctionNode(int floorIndex, bool returnToHome = true)
    {
        NodeId = $"auction{floorIndex}";
        FloorIndex = floorIndex;
        ReturnToHome = returnToHome;
    }
}

/// <summary>
/// ノベルパートを表すノード
/// </summary>
public class NovelNode : StoryNode
{
    /// <summary>
    /// シナリオID
    /// </summary>
    public string ScenarioId { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="scenarioId">シナリオID</param>
    /// <param name="returnToHome">このノード終了後にホームに戻るか（デフォルト: true）</param>
    public NovelNode(string scenarioId, bool returnToHome = true)
    {
        NodeId = scenarioId;
        ScenarioId = scenarioId;
        ReturnToHome = returnToHome;
    }
}

public class DemoEnding : StoryNode
{
    public DemoEnding()
    {
        NodeId = "demo_ending";
        ReturnToHome = true;
    }
}
