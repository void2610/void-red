/// <summary>
/// ゲーム全体で使用される定数。数値バランスは仮値で、実装後に調整する
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// 階層開始時に各感情属性へ補充する枚数 (8 種類 × 5 枚 = 40 枚)
    /// </summary>
    public const int EMOTION_REFILL_PER_FLOOR = 5;

    /// <summary>
    /// 1 階層で出品されるロット数
    /// </summary>
    public const int LOTS_PER_FLOOR = 5;

    /// <summary>
    /// 最後の上乗せからこの秒数誰も動かなければ競合が確定する
    /// </summary>
    public const float COMPETITION_TIMEOUT_SECONDS = 10f;

    /// <summary>
    /// NPC が競合で上乗せを検討する間隔 (秒) の下限 / 上限
    /// </summary>
    public const float NPC_RAISE_INTERVAL_MIN = 1.5f;
    public const float NPC_RAISE_INTERVAL_MAX = 4f;

    /// <summary>
    /// 最終階層の番号 (0〜4 の 5 階層)
    /// </summary>
    public const int LAST_FLOOR_INDEX = 4;
}
