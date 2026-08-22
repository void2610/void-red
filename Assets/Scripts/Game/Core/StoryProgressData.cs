/// <summary>
/// ストーリー進行データを保持するクラス
/// </summary>
public class StoryProgressData
{
    public int CurrentStep { get; set; }
    public StoryNode CurrentNode { get; set; }

    public void AdvanceStep() => CurrentStep++;

    public void Reset()
    {
        CurrentStep = 0;
        CurrentNode = null;
    }
}
