using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// カードのUIと効果を管理するクラス
/// MonoBehaviourを継承してGameObjectとして機能
/// </summary>
public class Card : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI forgivenessText;
    [SerializeField] private TextMeshProUGUI rejectionText;
    [SerializeField] private TextMeshProUGUI blankText;
    [SerializeField] private Button cardButton;
    
    // プロパティ
    public CardData CardData { get; private set; }

    /// <summary>
    /// カードデータを注入して初期化
    /// </summary>
    public void Init(CardData cardData)
    {
        CardData = cardData;
        UpdateUI();
    }
    
    /// <summary>
    /// UIを更新
    /// </summary>
    private void UpdateUI()
    {
        if (!CardData) return;
        
        // カード名を設定
        cardNameText.text = CardData.CardName;
        
        // カード画像を設定
        if (CardData.CardImage)
        {
            cardImage.sprite = CardData.CardImage;
        }
        
        // 効果パラメータを設定
        var effect = CardData.Effect;
        forgivenessText.text = $"許し: {effect.Forgiveness:F1}";
        rejectionText.text = $"拒絶: {effect.Rejection:F1}";
        blankText.text = $"空白: {effect.Blank:F1}";
    }
}