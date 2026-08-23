using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// ロットの締め: 落札者を告知し、場を片付ける。最終ロットなら勝敗判定まで進める
/// </summary>
public class LotResultPhase : IAuctionPhase
{
    // 同じロットの結果を二度流さない (Phase は落札確定後も LotResult のままのため)
    private int _shownLotIndex = -1;

    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.LotResult && _shownLotIndex != session.CurrentLotIndex;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        _shownLotIndex = context.Session.CurrentLotIndex;
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
