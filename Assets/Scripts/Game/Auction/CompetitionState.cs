using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 同数入札の参加者同士で 1 枚ずつ上乗せする競合フェーズ
/// 時刻は外から渡し、タイマーの判定を決定的にする
/// </summary>
public class CompetitionState
{
    public MemoryLotData Lot { get; }
    public IReadOnlyList<AuctionParticipant> Competitors => _competitors;
    public float TimeoutSeconds { get; }
    public bool IsActive { get; private set; } = true;

    private readonly List<AuctionParticipant> _competitors;
    private readonly Dictionary<AuctionParticipant, EmotionBid> _raises = new();
    private readonly float _startedAt;
    private float _lastActionTime;

    public CompetitionState(MemoryLotData lot, IEnumerable<AuctionParticipant> competitors, float now, float timeoutSeconds)
    {
        Lot = lot;
        _competitors = competitors.ToList();
        foreach (var c in _competitors) _raises[c] = new EmotionBid();
        _lastActionTime = now;
        _startedAt = now;
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>競合に入っていない参加者は 0 を返す (見学側の表示に使う)</summary>
    public int TotalOf(AuctionParticipant p) => _raises.TryGetValue(p, out var raises) ? p.SubmittedBid.Total + raises.Total : 0;

    public EmotionBid RaisesOf(AuctionParticipant p) => _raises.TryGetValue(p, out var raises) ? raises : new EmotionBid();

    public float RemainingSeconds(float now) => Math.Max(0f, TimeoutSeconds - (now - _lastActionTime));

    /// <summary>
    /// 最後の上乗せから確定時間が経った、または競合そのものが長引きすぎた
    /// (上乗せが続く限り終わらないと進行が止まるため、絶対的な打ち切りを持つ)
    /// </summary>
    public bool IsTimedOut(float now) => now - _lastActionTime >= TimeoutSeconds || now - _startedAt >= TimeoutSeconds * GameConstants.COMPETITION_HARD_LIMIT_RATIO;

    /// <summary>
    /// 同数のまま誰も上乗せできなくなった (全員リソース切れ)
    /// </summary>
    public bool IsDeadlocked() => Leader() == null && _competitors.All(c => c.Wallet.Total == 0);

    public void End() => IsActive = false;

    /// <summary>
    /// 1 枚上乗せする。所持が無ければ失敗。パスの概念はなく、時間内なら何度でも戻れる
    /// </summary>
    public bool TryRaise(AuctionParticipant p, EmotionType emotion, float now)
    {
        if (!IsActive || !_competitors.Contains(p)) return false;
        if (!p.Wallet.TryConsume(emotion, 1)) return false;
        _raises[p].Add(emotion);
        _lastActionTime = now;
        return true;
    }

    /// <summary>
    /// 単独最高額なら勝者。まだ同数なら null
    /// </summary>
    public AuctionParticipant Leader()
    {
        var max = _competitors.Max(TotalOf);
        var leaders = _competitors.Where(c => TotalOf(c) == max).ToList();
        return leaders.Count == 1 ? leaders[0] : null;
    }

    /// <summary>
    /// 競合に入った入札は落札の可否にかかわらず返らない。最終的な内訳 (提出分 + 上乗せ分) を返す
    /// </summary>
    public EmotionBid FinalBidOf(AuctionParticipant p)
    {
        var total = p.SubmittedBid.Clone();
        foreach (var e in EmotionWallet.ALL_EMOTIONS) total.Add(e, _raises[p].Get(e));
        return total;
    }
}
