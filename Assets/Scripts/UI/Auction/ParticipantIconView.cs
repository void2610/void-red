using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卓に着いている参加者 1 人分のアイコン
/// 所持リソース数は常時見せ、入札額は開示時だけ見せる
/// </summary>
public class ParticipantIconView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private TextMeshProUGUI bidText;
    [SerializeField] private GameObject winnerMark;
    [SerializeField] private GameObject outMark;
    [SerializeField] private Button selectButton;

    public AuctionParticipant Participant { get; private set; }
    public Observable<AuctionParticipant> OnSelected => selectButton.OnClickAsObservable().Select(_ => Participant);

    public void ShowBid(int total) => bidText.text = total.ToString();

    public void HideBid() => bidText.text = "";

    public void SetWinner(bool isWinner) => winnerMark.SetActive(isWinner);

    public void SetSelected(bool selected) => frame.color = new Color(frame.color.r, frame.color.g, frame.color.b, selected ? 1f : 0.35f);

    public void SetSelectable(bool selectable) => selectButton.interactable = selectable && !Participant.IsPlayer && Participant.CanBid;

    public void Bind(AuctionParticipant participant)
    {
        Participant = participant;
        name = $"Participant_{participant.DisplayName}";
        nameText.text = participant.DisplayName;
        var sprite = participant.Data ? (participant.Data.IconSprite ? participant.Data.IconSprite : participant.Data.Portrait) : null;
        icon.sprite = sprite;
        icon.enabled = sprite;
        // 枠は所属色の細い縁として使う (画像が無い相手でも人数分の区切りは見せる)
        frame.color = participant.Data ? new Color(participant.Data.ThemeColor.r, participant.Data.ThemeColor.g, participant.Data.ThemeColor.b, 0.55f) : new Color(1f, 1f, 1f, 0.35f);
        HideBid();
        Refresh();
    }

    public void Refresh()
    {
        resourceText.text = Participant.Wallet.Total.ToString();
        outMark.SetActive(!Participant.CanBid && !Participant.IsPlayer);
    }
}
