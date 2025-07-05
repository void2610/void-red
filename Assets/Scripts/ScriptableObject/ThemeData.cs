using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// テーマデータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewThemeData", menuName = "VoidRed/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("テーマ情報")]
    [SerializeField] private string title;
    [SerializeField] private SerializableDictionary<CardAttribute, float> attributeMultipliers = new SerializableDictionary<CardAttribute, float>();
    
    public string Title => title;
    public SerializableDictionary<CardAttribute, float> AttributeMultipliers => attributeMultipliers;
    
    /// <summary>
    /// 指定された属性のスコア倍率を取得
    /// </summary>
    /// <param name="attribute">カード属性</param>
    /// <returns>スコア倍率（設定されていない場合は1.0）</returns>
    public float GetMultiplier(CardAttribute attribute)
    {
        return attributeMultipliers.TryGetValue(attribute, out float multiplier) ? multiplier : 1.0f;
    }
}