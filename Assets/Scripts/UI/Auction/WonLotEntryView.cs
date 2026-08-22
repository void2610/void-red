using System.Linq;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 洗礼で 1 つの落札記憶を確認する行。内訳と歪みを見せ、統合する記憶を選ばせる
/// </summary>
public class WonLotEntryView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI distortionText;
    [SerializeField] private Button integrateButton;
    [SerializeField] private Image frame;

    public WonLot WonLot { get; private set; }
    public Observable<WonLot> OnIntegrate => integrateButton.OnClickAsObservable().Select(_ => WonLot);

    public void SetSelected(bool on) => frame.color = on ? new Color(1f, 0.9f, 0.4f) : Color.white;

    public void Bind(WonLot wonLot)
    {
        WonLot = wonLot;
        name = $"Won_{wonLot.Lot.LotId}";
        titleText.text = $"ロット {wonLot.LotIndex + 1} 『{wonLot.Lot.Title}』 ({wonLot.Lot.Emotion.ToJapaneseName()})";
        var breakdown = string.Join(" ", EmotionWallet.ALL_EMOTIONS.Where(e => wonLot.Bid.Get(e) > 0).Select(e => $"{e.ToJapaneseName()}{wonLot.Bid.Get(e)}"));
        var others = string.Join(" / ", wonLot.OtherBids.Select(kv => $"{kv.Key}:{kv.Value}"));
        detailText.text = $"入札 {wonLot.Bid.Total} 枚 [{breakdown}]{(wonLot.ViaCompetition ? " 競合" : "")}\n他: {others}";
        distortionText.text = wonLot.Distortion == 0 ? "歪み なし" : $"歪み +{wonLot.Distortion}";
        distortionText.color = wonLot.Distortion == 0 ? Color.white : new Color(0.9f, 0.3f, 0.3f);
    }
}
