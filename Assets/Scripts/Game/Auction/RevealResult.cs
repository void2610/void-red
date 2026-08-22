using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一斉開示の結果。単独最高額なら Winner、同数なら TiedParticipants が競合に入る
/// </summary>
public class RevealResult
{
    public IReadOnlyList<AuctionParticipant> Bidders { get; }
    public AuctionParticipant Winner { get; }
    public IReadOnlyList<AuctionParticipant> TiedParticipants { get; }

    public bool IsTie => Winner == null && TiedParticipants.Count > 1;
    public bool NoBidders => Bidders.Count == 0;

    public RevealResult(IReadOnlyList<AuctionParticipant> bidders)
    {
        Bidders = bidders;
        if (bidders.Count == 0)
        {
            TiedParticipants = new List<AuctionParticipant>();
            return;
        }
        var max = bidders.Max(b => b.SubmittedBid.Total);
        var top = bidders.Where(b => b.SubmittedBid.Total == max).ToList();
        Winner = top.Count == 1 ? top[0] : null;
        TiedParticipants = top;
    }
}
