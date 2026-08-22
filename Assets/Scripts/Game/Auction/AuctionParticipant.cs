using System.Collections.Generic;

/// <summary>
/// オークション卓に着いている 1 人の実行時状態
/// </summary>
public class AuctionParticipant
{
    public ParticipantData Data { get; }
    public bool IsPlayer { get; }
    public string DisplayName { get; }
    public EmotionWallet Wallet { get; }
    public List<WonLot> WonLots { get; } = new();

    /// <summary>NPC が今のロットに入れる予定の内訳。対話で書き換わる</summary>
    public EmotionBid PlannedBid { get; set; } = new();

    /// <summary>このロットで提出が確定した入札。不参加なら null</summary>
    public EmotionBid SubmittedBid { get; set; }

    /// <summary>対話コマンドの振り分けで次のロットへ持ち越す増減</summary>
    public int CarryToNext { get; set; }

    /// <summary>挑発で入札を取りやめた状態</summary>
    public bool Withdrawn { get; set; }

    public HashSet<DialogueCommand> UsedCommandsThisLot { get; } = new();
    public bool CounterFiredThisLot { get; set; }

    /// <summary>このオークションで 1 つも落札できず人格崩壊した</summary>
    public bool HasCollapsed { get; set; }

    /// <summary>リソースが尽きた参加者はそのロットの入札に参加しない</summary>
    public bool CanBid => Wallet.Total > 0 && !Withdrawn;

    public AuctionParticipant(ParticipantData data, EmotionWallet wallet, bool isPlayer, string displayName)
    {
        Data = data;
        Wallet = wallet;
        IsPlayer = isPlayer;
        DisplayName = displayName;
    }

    public void ResetForLot()
    {
        PlannedBid = new EmotionBid();
        SubmittedBid = null;
        Withdrawn = false;
        UsedCommandsThisLot.Clear();
        CounterFiredThisLot = false;
    }
}

/// <summary>
/// 落札した記憶と、そのときの入札内訳
/// </summary>
public class WonLot
{
    public MemoryLotData Lot { get; }
    public int LotIndex { get; }
    public EmotionBid Bid { get; }
    public bool ViaCompetition { get; }
    public IReadOnlyDictionary<string, int> OtherBids { get; }

    public int Distortion => Bid.DistortionAgainst(Lot.Emotion);

    public WonLot(MemoryLotData lot, int lotIndex, EmotionBid bid, bool viaCompetition, IReadOnlyDictionary<string, int> otherBids)
    {
        Lot = lot;
        LotIndex = lotIndex;
        Bid = bid;
        ViaCompetition = viaCompetition;
        OtherBids = otherBids;
    }
}
