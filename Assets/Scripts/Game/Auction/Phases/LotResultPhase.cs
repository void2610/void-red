using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// ロットの締め: 落札者を告知し、場を片付ける。最終ロットなら勝敗判定まで進める
/// </summary>
public class LotResultPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.LotResult;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        var winner = session.LastWinner;

        view.RefreshParticipants();
        view.HighlightWinner(winner);
        await view.Announcement.DisplayAnnouncement(winner != null ? $"{winner.DisplayName} が落札" : "流札", 1.4f);
        view.Auction.Clear();
        view.Auction.Hide();

        if (session.IsLastLot) session.FinishLots();
    }
}
