using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 対話フェーズ: 相手を選び、対話コマンドで入札予定を揺さぶる
/// 入力は DialogueInputView が 1 つの選択として返し、ここは結果の適用と演出だけを持つ
/// </summary>
public class DialoguePhase : IAuctionPhase
{
    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.Dialogue;

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        view.Dialogue.Show();
        await SelectTargetAsync(context, context.DialogueTarget ?? session.FirstAvailableRival());

        while (true)
        {
            // 入力が即座に返る状況でも 1 フレームは進める (取りこぼしと暴走の両方を避ける)
            await UniTask.Yield(ct);
            var input = await view.WaitDialogueInputAsync(session, ct);
            if (input.IsProceedToBidding) break;

            if (input.Target != null)
            {
                await SelectTargetAsync(context, input.Target);
                continue;
            }

            var target = context.DialogueTarget;
            if (!session.CanUseDialogue(target, input.Command)) continue;

            view.SetInputEnabled(false);
            var outcome = session.UseDialogue(target, input.Command);
            await PlayOutcomeAsync(context, outcome, ct);
            view.SetInputEnabled(true);
        }

        view.Dialogue.Hide();
        view.SetTargetSelectable(false);
        session.EnterBidding();
    }

    private static async UniTask SelectTargetAsync(AuctionContext context, AuctionParticipant target)
    {
        context.DialogueTarget = target;
        context.View.SetSelectedTarget(target);
        await context.View.Dialogue.SetTargetAsync(target.Data);
    }

    private static async UniTask PlayOutcomeAsync(AuctionContext context, DialogueOutcome outcome, CancellationToken ct)
    {
        var view = context.View;
        await view.Dialogue.ShowPlayerLineAsync(DialogueLines.PlayerLine(outcome.Command));
        await view.Dialogue.ShowTargetLineAsync(outcome.Line);

        if (outcome.ObservedTotal.HasValue)
        {
            view.ShowObservedBid(outcome.Target, outcome.ObservedTotal.Value);
            await view.Announcement.DisplayAnnouncement($"{outcome.Target.DisplayName} の入札予定: {outcome.ObservedTotal.Value} 枚", 1.5f);
        }
        else if (!outcome.Success)
        {
            await view.Announcement.DisplayAnnouncement("手応えがない", 1.2f);
        }

        if (outcome.Counter == null) return;

        await view.Dialogue.ShowTargetLineAsync(outcome.Counter.Prompt);
        var choice = await view.WaitCounterChoiceAsync(outcome.Counter, ct);
        context.Session.AnswerCounterDialogue(outcome.Target, choice);
        await view.Dialogue.ShowPlayerLineAsync(choice == 0 ? outcome.Counter.ChoiceA : outcome.Counter.ChoiceB);
    }
}

/// <summary>
/// 対話の主人公側セリフ
/// </summary>
public static class DialogueLines
{
    public static string PlayerLine(DialogueCommand command) => command switch
    {
        DialogueCommand.Observe => "……あなたの手の内、見せてもらう。",
        DialogueCommand.Provoke => "その程度で、この記憶が欲しいの？",
        DialogueCommand.Empathize => "その気持ち、分かる気がする。",
        DialogueCommand.Persuade => "その記憶は、あなたには必要ないはず。",
        _ => "……。",
    };
}
