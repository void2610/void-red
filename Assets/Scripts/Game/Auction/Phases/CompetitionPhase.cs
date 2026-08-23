using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// 競合フェーズ: 同数入札の参加者同士で 1 枚ずつ上乗せする
/// </summary>
public class CompetitionPhase : IAuctionPhase
{
    private EmotionType _selected = EmotionType.Joy;

    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.Competition;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        session.StartCompetition(Time.time);
        var competition = session.Competition;
        var playerCompeting = competition.Competitors.Contains(session.Player);

        view.Competition.Initialize(playerCompeting ? competition.TotalOf(session.Player) : 0, RivalTop(competition), Remaining(session));
        view.Competition.SetInstruction(playerCompeting ? "競り上げろ" : "競合を見守る");
        var topRival = competition.Competitors.Where(c => !c.IsPlayer).OrderByDescending(competition.TotalOf).FirstOrDefault();
        view.Competition.SetPortraits(view.PlayerPortrait, topRival?.Data ? topRival.Data.Portrait : null);
        view.Competition.SetEmotionInteractable(playerCompeting);
        UpdateRaiseInteractable(context, playerCompeting);

        using var d = new CompositeDisposable();
        view.Competition.OnEmotionSelected.Subscribe(e =>
        {
            _selected = e;
            UpdateRaiseInteractable(context, playerCompeting);
        }).AddTo(d);
        view.Competition.OnRaise.Subscribe(_ =>
        {
            if (!session.TryPlayerRaise(_selected, Time.time)) return;
            SeManager.Instance.PlaySe(_selected.ToResourceSeName(), pitch: 1f);
            view.Competition.UpdateResources(Remaining(session));
            view.RefreshParticipants();
        }).AddTo(d);

        // 進行判定は CompetitionRunner が持ち、ここは描画と入力の反映だけを担う
        var runner = new CompetitionRunner(session, session.Rng, Time.time);
        while (runner.Step(Time.time))
        {
            view.Competition.UpdateBids(competition.TotalOf(session.Player), RivalTop(competition));
            view.Competition.UpdateTimer(competition.RemainingSeconds(Time.time), competition.TimeoutSeconds);
            view.ShowCompetitionTotals(competition);
            UpdateRaiseInteractable(context, playerCompeting);
            await UniTask.Yield(ct);
        }

        view.Competition.Hide();
        var winner = session.ResolveCompetition();
        await view.Auction.ShowResultAsync(winner != null && winner.IsPlayer, false, false, RevealPhase.RivalColor(context));
    }

    /// <summary>
    /// 上乗せは選んでいる属性を 1 枚使うので、その属性が枯れていたら押せなくする
    /// (総数だけで判定すると、押せるのに上乗せできない状態が生まれる)
    /// </summary>
    private void UpdateRaiseInteractable(AuctionContext context, bool playerCompeting)
    {
        context.View.Competition.SetRaiseInteractable(playerCompeting && context.Session.Player.Wallet.Get(_selected) > 0);
    }

    private static int RivalTop(CompetitionState competition)
    {
        return competition.Competitors.Where(c => !c.IsPlayer).Select(competition.TotalOf).DefaultIfEmpty(0).Max();
    }

    private static System.Collections.Generic.Dictionary<EmotionType, int> Remaining(AuctionSession session)
    {
        return EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, session.Player.Wallet.Get);
    }

}
