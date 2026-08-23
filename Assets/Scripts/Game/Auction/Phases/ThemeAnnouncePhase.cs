using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 記憶テーマの公開
/// </summary>
public class ThemeAnnouncePhase : IAuctionPhase
{
    private bool _announced;

    public bool CanRun(AuctionSession session) => !_announced && session.Phase == AuctionPhase.ThemeAnnounce;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        _announced = true;
        await context.View.Theme.DisplayThemeWithKeywords(context.Session.Floor.ThemeTitle, true);
    }
}
