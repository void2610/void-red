using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卓に着いている参加者 1 人分の表示。所持リソース総数は常時見せ、入札額は開示時だけ見せる
/// </summary>
public class ParticipantSlotView : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private TextMeshProUGUI bidText;
    [SerializeField] private GameObject winnerLabel;
    [SerializeField] private GameObject outLabel;
    [SerializeField] private Image highlight;
    [SerializeField] private Button selectButton;

    public AuctionParticipant Participant { get; private set; }
    public Observable<AuctionParticipant> OnSelected => selectButton.OnClickAsObservable().Select(_ => Participant);

    public void ShowBid(int total) => bidText.text = total.ToString();

    public void HideBid() => bidText.text = "";

    public void SetWinner(bool isWinner) => winnerLabel.SetActive(isWinner);

    public void SetHighlighted(bool on) => highlight.enabled = on;

    public void SetSelectable(bool on) => selectButton.interactable = on && !Participant.IsPlayer && Participant.CanBid;

    public void Bind(AuctionParticipant participant)
    {
        Participant = participant;
        name = $"Slot_{participant.DisplayName}";
        nameText.text = participant.DisplayName;
        portrait.sprite = participant.Data != null ? participant.Data.Portrait : null;
        portrait.color = participant.Data != null ? participant.Data.Emotion.GetTintColor() : Color.white;
        selectButton.interactable = !participant.IsPlayer;
        HideBid();
        Refresh();
    }

    public void Refresh()
    {
        resourceText.text = $"{Participant.Wallet.Total}";
        outLabel.SetActive(!Participant.CanBid && !Participant.IsPlayer);
    }
}
