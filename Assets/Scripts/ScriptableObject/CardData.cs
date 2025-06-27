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
    
    // プロパティ
    public string CardName => cardName;
    public CardStatus Effect => status;
    public Sprite CardImage => image;
}