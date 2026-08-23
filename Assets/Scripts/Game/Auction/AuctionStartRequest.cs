/// <summary>
/// 進行度と独立してオークションを起動するための予約 (デバッグ / 検証用)
/// Root に常駐し、AuctionLifetimeScope が参照する。ゲームオーバーのやり直しでも同じ条件で再開できるよう、
/// 予約はホームへ戻るまで保持され、HomePresenter が破棄する
/// </summary>
public class AuctionStartRequest
{
    public int? FloorOverride { get; private set; }
    public int? Seed { get; private set; }
    public float? CompetitionTimeout { get; private set; }

    public void Set(int floorIndex, int seed, float competitionTimeout)
    {
        FloorOverride = floorIndex;
        Seed = seed;
        CompetitionTimeout = competitionTimeout;
    }

    public void Clear()
    {
        FloorOverride = null;
        Seed = null;
        CompetitionTimeout = null;
    }
}
