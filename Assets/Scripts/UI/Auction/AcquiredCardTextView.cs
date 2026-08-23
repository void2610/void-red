using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 洗礼で札の脇に出す 1 件分の内訳 (記憶名 / 入札 / 歪み)
/// </summary>
public class AcquiredCardTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cardNameText;

    public void Initialize(WonLot wonLot)
    {
        var distortion = wonLot.Distortion == 0 ? "歪みなし" : $"歪み +{wonLot.Distortion}";
        var breakdown = string.Join(" ", EmotionWallet.ALL_EMOTIONS
            .Where(e => wonLot.Bid.Get(e) > 0)
            .Select(e => $"{e.ToJapaneseName()}{wonLot.Bid.Get(e)}"));
        var others = wonLot.OtherBids.Count == 0
            ? "他に入札なし"
            : string.Join(" / ", wonLot.OtherBids.Select(kv => $"{kv.Key} {kv.Value}"));

        cardNameText.text =
            $"第 {wonLot.LotIndex + 1} 競売  『{wonLot.Lot.Title}』 ({wonLot.Lot.Emotion.ToJapaneseName()})\n" +
            $"入札 {wonLot.Bid.Total} 枚 [{breakdown}]  {distortion}{(wonLot.ViaCompetition ? "  競合" : "")}\n" +
            $"他の入札: {others}";
    }
}
