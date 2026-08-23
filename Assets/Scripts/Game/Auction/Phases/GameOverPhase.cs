using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

/// <summary>
/// ゲームオーバー: 進行度を変えずに同じ階層をやり直すか、ロビーへ戻る
/// </summary>
public class GameOverPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.GameOver;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        view.GameOver.Show(context.Session.Floor.FloorIndex, context.Session.MissedKey);
        var toRetry = await Observable.Merge(
            view.GameOver.OnRetry.Select(_ => true),
            view.GameOver.OnLobby.Select(_ => false)).FirstAsync(ct);
        await context.SceneTransition.TransitionToSceneWithFade(toRetry ? SceneType.Auction : SceneType.Home);
    }
}
