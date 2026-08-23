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
        cardNameText.text = $"『{wonLot.Lot.Title}』  {wonLot.Bid.Total} 枚  {distortion}{(wonLot.ViaCompetition ? "  競合" : "")}";
    }
}
