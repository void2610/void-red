using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 記憶コレクションの 1 行。未収集は伏せ字、統合済みは印を付ける
/// </summary>
public class MemoryCollectionEntryView : MonoBehaviour
{
    [SerializeField] private Image colorBar;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI flavorText;
    [SerializeField] private TextMeshProUGUI markText;

    public void Bind(MemoryLotData lot, bool collected, bool integrated)
    {
        name = $"Entry_{lot.LotId}";
        colorBar.color = collected ? lot.Emotion.GetColor() : new Color(0.3f, 0.3f, 0.3f);
        titleText.text = collected ? $"{lot.LotId} 『{lot.Title}』 ({lot.Emotion.ToJapaneseName()})" : $"{lot.LotId} 『？？？』";
        flavorText.text = collected ? lot.Flavor : "";
        markText.text = integrated ? "統合" : collected ? "所持" : "";
    }
}
