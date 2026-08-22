using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 場に出ている記憶 (ロット) の表示。共鳴値は見せない
/// </summary>
public class LotView : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI flavorText;
    [SerializeField] private TextMeshProUGUI emotionText;

    public void Hide() => gameObject.SetActive(false);

    public void Show(MemoryLotData lot, int lotNumber)
    {
        gameObject.SetActive(true);
        image.sprite = lot.Image;
        image.color = lot.Emotion.GetColor();
        numberText.text = $"ロット {lotNumber}";
        titleText.text = $"『{lot.Title}』";
        flavorText.text = lot.Flavor;
        emotionText.text = lot.Emotion.ToJapaneseName();
        emotionText.color = lot.Emotion.GetColor();
    }
}
