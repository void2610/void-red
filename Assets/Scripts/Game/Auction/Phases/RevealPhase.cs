using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 一斉開示: 入札額を公開し、単独最高額ならその場で落札を確定する
/// </summary>
public class RevealPhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.LastReveal != null && !session.RevealShown && session.Phase is AuctionPhase.Reveal or AuctionPhase.Competition;

    public static Dictionary<EmotionType, int> Breakdown(EmotionBid bid) => EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, bid.Get);

    public static Color RivalColor(AuctionContext context) => context.DialogueTarget?.Data ? context.DialogueTarget.Data.ThemeColor : Color.red;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        var reveal = session.LastReveal;
        var rivalTop = reveal.Bidders.Where(b => !b.IsPlayer).Select(b => b.SubmittedBid.Total).DefaultIfEmpty(0).Max();

        session.RevealShown = true;
        view.Auction.ShowBids(Breakdown(session.Player.SubmittedBid), rivalTop);
        view.ShowParticipantBids(reveal);
        await UniTask.Delay(900, cancellationToken: ct);

        if (reveal.IsTie)
        {
            await view.Auction.ShowResultAsync(false, true, false, RivalColor(context));
            return;
        }

        var winner = session.ResolveReveal();
        await view.Auction.ShowResultAsync(winner != null && winner.IsPlayer, false, winner == null, RivalColor(context));
    }
}
