using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// オークション画面のルート View。各フェーズのパネルと参加者スロットを束ねる
/// </summary>
public class AuctionView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI floorText;
    [SerializeField] private TextMeshProUGUI themeText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private LotView lotView;
    [SerializeField] private DialoguePanelView dialoguePanel;
    [SerializeField] private CounterDialogueView counterDialogue;
    [SerializeField] private BidPanelView bidPanel;
    [SerializeField] private CompetitionPanelView competitionPanel;
    [SerializeField] private BaptismView baptismView;
    [SerializeField] private GameOverView gameOverView;
    [SerializeField] private Button nextButton;

    public DialoguePanelView DialoguePanel => dialoguePanel;
    public CounterDialogueView CounterDialogue => counterDialogue;
    public BidPanelView BidPanel => bidPanel;
    public CompetitionPanelView CompetitionPanel => competitionPanel;
    public BaptismView Baptism => baptismView;
    public GameOverView GameOver => gameOverView;
    public IReadOnlyList<ParticipantSlotView> Slots => _slots;
    public Observable<AuctionParticipant> OnSlotSelected => _onSlotSelected;
    public Observable<Unit> OnNext => nextButton.OnClickAsObservable();
    public bool IsWaitingNext => nextButton.gameObject.activeSelf;
    public string Message => messageText.text;

    private readonly List<ParticipantSlotView> _slots = new();
    private readonly Subject<AuctionParticipant> _onSlotSelected = new();
    private readonly CompositeDisposable _disposables = new();

    public void SetMessage(string text) => messageText.text = text;

    public void SetNextInteractable(bool on) => nextButton.interactable = on;

    public ParticipantSlotView SlotOf(AuctionParticipant p) => _slots.First(s => s.Participant == p);

    public void LotViewShow(MemoryLotData lot, int number) => lotView.Show(lot, number);

    public void Initialize(AuctionSession session)
    {
        floorText.text = $"第 {session.Floor.FloorIndex} 階層";
        themeText.text = $"記憶テーマ「{session.Floor.ThemeTitle}」";
        foreach (var p in session.Participants)
        {
            var slot = Instantiate(slotPrefab, slotContainer).GetComponent<ParticipantSlotView>();
            slot.Bind(p);
            slot.OnSelected.Subscribe(_onSlotSelected.OnNext).AddTo(_disposables);
            _slots.Add(slot);
        }
        HideAllPanels();
    }

    public void RefreshSlots()
    {
        foreach (var s in _slots) s.Refresh();
    }

    public void HideBids()
    {
        foreach (var s in _slots)
        {
            s.HideBid();
            s.SetWinner(false);
            s.SetHighlighted(false);
        }
    }

    public void SetSlotsSelectable(bool on)
    {
        foreach (var s in _slots) s.SetSelectable(on);
    }

    public void HideAllPanels()
    {
        dialoguePanel.Hide();
        counterDialogue.Hide();
        bidPanel.Hide();
        competitionPanel.Hide();
        baptismView.Hide();
        gameOverView.Hide();
        nextButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 「次へ」が押されるまで待つ。演出の区切りをプレイヤーに委ねる
    /// </summary>
    public async UniTask WaitNextAsync(string message)
    {
        SetMessage(message);
        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
        await OnNext.FirstAsync(destroyCancellationToken);
        nextButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
