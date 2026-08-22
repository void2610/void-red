/// <summary>
/// 進行度と独立してオークションを起動するための予約 (デバッグ / 検証用)
/// Root に常駐し、AuctionLifetimeScope が消費する
/// </summary>
public class AuctionStartRequest
{
    private int? _floorOverride;
    private int? _seed;
    private float? _competitionTimeout;

    public int? ConsumeFloorOverride() => Consume(ref _floorOverride);

    public int? ConsumeSeed() => Consume(ref _seed);

    public float? ConsumeCompetitionTimeout() => Consume(ref _competitionTimeout);

    public void Set(int floorIndex, int seed, float competitionTimeout)
    {
        _floorOverride = floorIndex;
        _seed = seed;
        _competitionTimeout = competitionTimeout;
    }

    private static T? Consume<T>(ref T? slot) where T : struct
    {
        var v = slot;
        slot = null;
        return v;
    }
}
