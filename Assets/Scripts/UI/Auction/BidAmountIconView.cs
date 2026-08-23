using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 感情別入札額アイコンView
// 感情アイコン画像と入札数値を表示する
public class BidAmountIconView : MonoBehaviour
{
    [SerializeField] private Image emotionIcon;
    [SerializeField] private TMP_Text amountText;

    public void Setup(Sprite icon, int amount)
    {
        emotionIcon.sprite = icon;
        amountText.text = amount.ToString();
    }
}
