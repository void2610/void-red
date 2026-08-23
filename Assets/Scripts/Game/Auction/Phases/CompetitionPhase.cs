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
        view.Competition.SetInstruction(playerCompeting ? "上乗せして競り勝て" : "競合を見守る");
        var topRival = competition.Competitors.Where(c => !c.IsPlayer).OrderByDescending(competition.TotalOf).FirstOrDefault();
        view.Competition.SetPortraits(view.PlayerPortrait, topRival?.Data ? topRival.Data.Portrait : null);
        view.Competition.SetEmotionInteractable(playerCompeting);
        view.Competition.SetRaiseInteractable(playerCompeting);

        using var d = new CompositeDisposable();
        view.Competition.OnEmotionSelected.Subscribe(e => _selected = e).AddTo(d);
        view.Competition.OnRaise.Subscribe(_ =>
        {
            if (!session.TryPlayerRaise(_selected, Time.time)) return;
            SeManager.Instance.PlaySe(_selected.ToResourceSeName(), pitch: 1f);
            view.Competition.UpdateResources(Remaining(session));
            view.RefreshParticipants();
        }).AddTo(d);

        var npcNext = competition.Competitors.Where(c => !c.IsPlayer).ToDictionary(c => c, _ => Time.time + NextNpcInterval());
        while (!competition.IsTimedOut(Time.time) && !competition.IsDeadlocked())
        {
            foreach (var npc in npcNext.Keys.ToList())
            {
                if (Time.time < npcNext[npc]) continue;
                if (session.TryNpcRaise(npc, Time.time)) SeManager.Instance.PlaySe(npc.Data.Emotion.ToResourceSeName(), pitch: 1f);
                npcNext[npc] = Time.time + NextNpcInterval();
            }
            view.Competition.UpdateBids(competition.TotalOf(session.Player), RivalTop(competition));
            view.Competition.UpdateTimer(competition.RemainingSeconds(Time.time), competition.TimeoutSeconds);
            view.ShowCompetitionTotals(competition);
            if (playerCompeting) view.Competition.SetRaiseInteractable(session.Player.Wallet.Total > 0);
            await UniTask.Yield(ct);
        }

        view.Competition.Hide();
        var winner = session.ResolveCompetition();
        await view.Auction.ShowResultAsync(winner != null && winner.IsPlayer, false, false, RevealPhase.RivalColor(context));
    }

    private static int RivalTop(CompetitionState competition)
    {
        return competition.Competitors.Where(c => !c.IsPlayer).Select(competition.TotalOf).DefaultIfEmpty(0).Max();
    }

    private static System.Collections.Generic.Dictionary<EmotionType, int> Remaining(AuctionSession session)
    {
        return EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, session.Player.Wallet.Get);
    }

    private static float NextNpcInterval()
    {
        return Random.Range(GameConstants.NPC_RAISE_INTERVAL_MIN, GameConstants.NPC_RAISE_INTERVAL_MAX);
    }
}
