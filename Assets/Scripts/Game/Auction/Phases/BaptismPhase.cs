using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

/// <summary>
/// 洗礼: 落札した記憶から 1 つを人格に統合し、階層を締めてロビーへ戻る
/// </summary>
public class BaptismPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.Baptism;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        WonLot chosen = null;

        await view.Announcement.DisplayAnnouncement("洗礼", 1.6f);
        view.Baptism.Show(session);

        using var d = new CompositeDisposable();
        view.Baptism.OnIntegrate.Subscribe(w =>
        {
            chosen = w;
            view.Baptism.SetSelected(w);
        }).AddTo(d);

        await view.Baptism.OnFinish.Where(_ => chosen != null).FirstAsync(ct);
        session.Finish();
        var collapsed = session.Rivals.Where(r => r.HasCollapsed).Select(r => r.Data.ParticipantId);
        context.Progress.RecordAuctionClearAndSave(session.Player.Wallet, chosen, session.Player.WonLots, collapsed);
        await context.SceneTransition.TransitionToSceneWithFade(SceneType.Home);
    }
}
