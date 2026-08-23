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
    public const float NPC_RAISE_INTERVAL_MIN = 0.8f;
    public const float NPC_RAISE_INTERVAL_MAX = 2f;

    /// <summary>
    /// 競合フェーズ全体の打ち切り時間 (確定時間の何倍まで粘れるか)
    /// </summary>
    public const float COMPETITION_HARD_LIMIT_RATIO = 4f;

    /// <summary>
    /// NPC が競合で上乗せできる枚数の上限 (提出額 + この値)。際限のない競り上げで決着しなくなるのを防ぐ
    /// </summary>
    public const int NPC_MAX_RAISE_MARGIN = 2;

    /// <summary>
    /// 共鳴値 100 の記憶 (目玉) に NPC が上乗せする枚数。共鳴が高いほど競りが集まる
    /// </summary>
    public const int RESONANCE_BID_BONUS_MAX = 3;

    /// <summary>
    /// 無落札のまま残りロットがこの数以下になった NPC は、人格崩壊を避けようと必死に食い下がる
    /// </summary>
    public const int DESPERATE_REMAINING_LOTS = 2;

    /// <summary>
    /// 必死になった NPC の上乗せ確率の下限と、上限枚数への上乗せ分
    /// </summary>
    public const int NPC_DESPERATE_MIN_CHANCE = 85;
    public const int NPC_DESPERATE_EXTRA_MARGIN = 3;

    /// <summary>
    /// 最終階層の番号 (0〜4 の 5 階層)
    /// </summary>
    public const int LAST_FLOOR_INDEX = 4;
}
