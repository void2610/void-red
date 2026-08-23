using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 次のロットを場に出す
/// </summary>
public class LotRevealPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase is AuctionPhase.ThemeAnnounce or AuctionPhase.LotResult;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var lot = context.Session.BeginNextLot();
        context.View.RefreshParticipants();
        context.View.Auction.Show();
        context.View.Auction.ShowLot(lot);
        await context.View.Announcement.DisplayAnnouncement($"第 {context.Session.CurrentLotIndex + 1} 競売\n『{lot.Title}』", 1.6f);
    }
}
