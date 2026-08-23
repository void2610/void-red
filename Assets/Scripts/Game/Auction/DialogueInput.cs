/// <summary>
/// 対話フェーズでプレイヤーが行った 1 つの入力
/// </summary>
public class DialogueInput
{
    public DialogueCommand Command { get; private set; }
    public AuctionParticipant Target { get; private set; }
    public bool IsProceedToBidding { get; private set; }

    public static DialogueInput OfCommand(DialogueCommand command) => new() { Command = command };

    public static DialogueInput OfTarget(AuctionParticipant target) => new() { Target = target };

    public static DialogueInput ProceedToBidding() => new() { IsProceedToBidding = true };
}
