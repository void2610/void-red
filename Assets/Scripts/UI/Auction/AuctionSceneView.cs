using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

/// <summary>
/// オークションシーンの View をまとめる窓口
/// Presenter はここ経由で各 View を触る
/// </summary>
public class AuctionSceneView : MonoBehaviour
{
    [Header("フェーズ View")]
    [SerializeField] private ThemeView theme;
    [SerializeField] private AnnouncementView announcement;
    [SerializeField] private AuctionView auction;
    [SerializeField] private DialoguePhaseView dialogue;
    [SerializeField] private CompetitionView competition;
    [SerializeField] private BaptismView baptism;
    [SerializeField] private GameOverView gameOver;

    [Header("参加者")]
    [SerializeField] private Sprite playerPortrait;
    [SerializeField] private Transform participantBar;
    [SerializeField] private ParticipantIconView participantIconPrefab;

    public ThemeView Theme => theme;
    public AnnouncementView Announcement => announcement;
    public AuctionView Auction => auction;
    public DialoguePhaseView Dialogue => dialogue;
    public CompetitionView Competition => competition;
    public BaptismView Baptism => baptism;
    public GameOverView GameOver => gameOver;
    public AuctionParticipant SelectedTarget { get; private set; }
    public Sprite PlayerPortrait => playerPortrait;

    private readonly List<ParticipantIconView> _icons = new();
    private readonly Subject<AuctionParticipant> _onTargetChanged = new();
    private readonly CompositeDisposable _disposables = new();

    /// <summary>
    /// 対話フェーズの入力を 1 つ待つ (相手の選び直し / コマンド / 入札へ進む)
    /// </summary>
    public async UniTask<DialogueInput> WaitDialogueInputAsync(AuctionSession session, CancellationToken ct)
    {
        SetTargetSelectable(true);
        auction.SetConfirmLabel("入札へ");
        dialogue.SetCommandAvailability(i => SelectedTarget != null && session.CanUseDialogue(SelectedTarget, (DialogueCommand)i));
        auction.SetConfirmInteractable(true);

        var picked = await UniTask.WhenAny(
            dialogue.WaitForCommandAsync(),
            _onTargetChanged.FirstAsync(ct).AsUniTask(),
            auction.OnBiddingConfirmed.FirstAsync(ct).AsUniTask());

        return picked.winArgumentIndex switch
        {
            0 => DialogueInput.OfCommand((DialogueCommand)picked.result1),
            1 => DialogueInput.OfTarget(picked.result2),
            _ => DialogueInput.ProceedToBidding(),
        };
    }

    /// <summary>対話中の入力を一時的に止める (演出中)</summary>
    public void SetInputEnabled(bool enabled)
    {
        SetTargetSelectable(enabled);
        dialogue.SetChoicesInteractable(enabled);
        auction.SetConfirmInteractable(enabled);
    }

    public void Initialize(AuctionSession session)
    {
        foreach (var p in session.Participants)
        {
            var icon = Instantiate(participantIconPrefab, participantBar);
            icon.Bind(p);
            icon.OnSelected.Subscribe(target =>
            {
                SelectedTarget = target;
                _onTargetChanged.OnNext(target);
            }).AddTo(_disposables);
            _icons.Add(icon);
        }
        SetTargetSelectable(false);
        auction.UpdateEmotionResources(EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, session.Player.Wallet.Get));
        competition.UpdateResources(EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, session.Player.Wallet.Get));
        auction.Hide();
        dialogue.Hide();
        competition.Hide();
        baptism.Hide();
        gameOver.Hide();
    }

    public void SetSelectedTarget(AuctionParticipant target)
    {
        SelectedTarget = target;
        foreach (var icon in _icons) icon.SetSelected(icon.Participant == target);
    }

    public void SetTargetSelectable(bool selectable)
    {
        foreach (var icon in _icons) icon.SetSelectable(selectable);
    }

    public void RefreshParticipants()
    {
        foreach (var icon in _icons)
        {
            icon.Refresh();
            icon.HideBid();
            icon.SetWinner(false);
        }
    }

    /// <summary>一斉開示: 入札した参加者の額を頭上に出す</summary>
    public void ShowParticipantBids(RevealResult reveal)
    {
        foreach (var bidder in reveal.Bidders) IconOf(bidder).ShowBid(bidder.SubmittedBid.Total);
    }

    public void ShowCompetitionTotals(CompetitionState competition)
    {
        foreach (var c in competition.Competitors) IconOf(c).ShowBid(competition.TotalOf(c));
    }

    public void HighlightWinner(AuctionParticipant winner)
    {
        foreach (var icon in _icons) icon.SetWinner(winner != null && icon.Participant == winner);
    }

    /// <summary>逆対話の二択を対話選択肢 View で受ける (前 2 つだけ使う)</summary>
    public async UniTask<int> WaitCounterChoiceAsync(CounterDialogue counter, CancellationToken ct)
    {
        await announcement.DisplayAnnouncement($"{counter.ChoiceA}  /  {counter.ChoiceB}", 1.2f);
        var index = await dialogue.WaitForCommandAsync();
        return index % 2;
    }

    private ParticipantIconView IconOf(AuctionParticipant p)
    {
        return _icons.First(i => i.Participant == p);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
        _onTargetChanged.Dispose();
    }
}
