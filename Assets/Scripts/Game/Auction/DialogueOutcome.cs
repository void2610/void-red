/// <summary>
/// 対話コマンド 1 回の結果。失敗しても必ずセリフが返る
/// </summary>
public class DialogueOutcome
{
    public DialogueCommand Command { get; }
    public AuctionParticipant Target { get; }
    public bool Success { get; }
    public string Line { get; }

    /// <summary>観察成功時の入札予定枚数。属性内訳は見せない</summary>
    public int? ObservedTotal { get; }

    /// <summary>観察で逆対話が発生したときの問いかけ</summary>
    public CounterDialogue Counter { get; }

    public DialogueOutcome(DialogueCommand command, AuctionParticipant target, bool success, string line, int? observedTotal = null, CounterDialogue counter = null)
    {
        Command = command;
        Target = target;
        Success = success;
        Line = line;
        ObservedTotal = observedTotal;
        Counter = counter;
    }
}
