using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲームオーバー: 進行度を変えずに同じ階層をやり直すか、ロビーへ戻る
/// </summary>
public class GameOverPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.GameOver;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        view.SetParticipantBarVisible(false);
        view.GameOver.Show(context.Session.Floor.FloorIndex, context.Session.MissedKey);
        await UniTask.WaitUntil(() => view.GameOver.RetryRequested.HasValue, cancellationToken: ct);
        var toRetry = view.GameOver.RetryRequested.Value;
        await context.SceneTransition.TransitionToSceneWithFade(toRetry ? SceneType.Auction : SceneType.Home);
    }
}
