using UnityEngine;

/// <summary>
/// カード情報を定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "VoidRed/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private CardStatus status;
    [SerializeField] private Sprite image;
    [SerializeField] private float scoreMultiplier = 1.0f; // カード固有の倍率
    [SerializeField] private int collapseThreshold = 3; // 崩壊閾値（この値を超えると崩壊確率が上がる）
    
    // プロパティ
    public string CardName => cardName;
    public CardStatus Effect => status;
    public Sprite CardImage => image;
    public float ScoreMultiplier => scoreMultiplier;
    public int CollapseThreshold => collapseThreshold;
}