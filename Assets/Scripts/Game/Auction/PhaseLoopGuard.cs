/// <summary>
/// 同じフェーズが同じ状態で回り続けていないかを見張る安全弁
/// フェーズの遷移条件を間違えると進行が無限ループし、画面が固まったまま戻らなくなるため
/// </summary>
public class PhaseLoopGuard
{
    /// <summary>同じ状態でこの回数続いたら異常とみなす</summary>
    public const int STUCK_THRESHOLD = 50;

    private string _lastKey = "";
    private int _repeatCount;

    public string Describe() => $"{_lastKey} が {_repeatCount} 回続いている";

    /// <summary>
    /// 1 巡ぶんの状態を記録し、進行が止まっていれば true
    /// </summary>
    public bool IsStuck(string phaseName, AuctionPhase phase, int lotIndex)
    {
        var key = $"{phaseName}/{phase}/{lotIndex}";
        if (key != _lastKey)
        {
            _lastKey = key;
            _repeatCount = 0;
        }

        _repeatCount++;
        return _repeatCount >= STUCK_THRESHOLD;
    }
}
