/// <summary>
/// ノベル進行データを保持するクラス
/// </summary>
public class NovelProgressData
{
    /// <summary>
    /// novel-kit のフラグ / 既読スナップショット (NovelSaveSerializer 形式のJSON)
    /// </summary>
    public string NovelKitState { get; set; } = "";

    /// <summary>
    /// リセット
    /// </summary>
    public void Reset()
    {
        NovelKitState = "";
    }
}
