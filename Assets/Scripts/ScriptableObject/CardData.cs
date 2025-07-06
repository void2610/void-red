using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// カード情報を定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "VoidRed/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string cardId;
    [SerializeField] private string cardName;
    [SerializeField] private CardAttribute attribute;
    [SerializeField] private Sprite image;
    [SerializeField] private float scoreMultiplier = 1.0f;
    [SerializeField] private int collapseThreshold = 3;
    
    [Header("進化システム")]
    [SerializeField] private List<EvolutionCondition> evolutionConditions = new List<EvolutionCondition>();
    [SerializeField] private CardData evolutionTarget;
    [SerializeField] private bool isEvolutionCard = false; // 進化後のカードかどうか
    
    public string CardId => cardId;
    public string CardName => cardName;
    public CardAttribute Attribute => attribute;
    public Sprite CardImage => image;
    public float ScoreMultiplier => scoreMultiplier;
    public int CollapseThreshold => collapseThreshold;
    
    public List<EvolutionCondition> EvolutionConditions => evolutionConditions;
    public CardData EvolutionTarget => evolutionTarget;
    public bool IsEvolutionCard => isEvolutionCard;
    
    /// <summary>
    /// 進化可能かどうかを判定
    /// </summary>
    public bool CanEvolve => evolutionTarget && evolutionConditions.Count > 0;
}