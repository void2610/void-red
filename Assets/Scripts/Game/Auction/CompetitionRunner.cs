using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 競合フェーズの進行判定
/// 時刻を外から渡すことで、View も PlayerLoop も無しに 1 ステップずつ進められる
/// (「必ず決着する」ことを EditMode テストで保証するための切り出し)
/// </summary>
public class CompetitionRunner
{
    /// <summary>直前の Step で上乗せした NPC (演出用)</summary>
    public IReadOnlyCollection<AuctionParticipant> Npcs => _nextRaiseAt.Keys;
    /// <summary>NPC が次に上乗せを検討する時刻</summary>
    private readonly Dictionary<AuctionParticipant, float> _nextRaiseAt = new();

    private readonly AuctionSession _session;
    private readonly System.Random _rng;

    public CompetitionRunner(AuctionSession session, System.Random rng, float now)
    {
        _session = session;
        _rng = rng;
        foreach (var npc in session.Competition.Competitors.Where(c => !c.IsPlayer)) _nextRaiseAt[npc] = now + NextInterval();
    }

    private float NextInterval()
    {
        return GameConstants.NPC_RAISE_INTERVAL_MIN + (float)_rng.NextDouble() * (GameConstants.NPC_RAISE_INTERVAL_MAX - GameConstants.NPC_RAISE_INTERVAL_MIN);
    }

    /// <summary>
    /// 1 ステップ進める。まだ競っているなら true
    /// </summary>
    public bool Step(float now)
    {
        var competition = _session.Competition;
        if (competition.IsTimedOut(now) || competition.IsDeadlocked()) return false;

        foreach (var npc in _nextRaiseAt.Keys.ToList())
        {
            if (now < _nextRaiseAt[npc]) continue;
            _session.TryNpcRaise(npc, now);
            _nextRaiseAt[npc] = now + NextInterval();
        }
        return true;
    }
}
