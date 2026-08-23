using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 個別の感情リソース表示アイテム
// 色インジケーター、残量テキスト、選択状態ボーダーを持つ
public class EmotionResourceItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;

    public EmotionType Emotion { get; private set; }

    public void UpdateAmount(int amount) => amountText.text = amount.ToString();
    public void Initialize(EmotionType emotion) => Emotion = emotion;
}
