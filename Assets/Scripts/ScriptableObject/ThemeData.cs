using UnityEngine;

/// <summary>
/// テーマデータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewThemeData", menuName = "VoidRed/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("テーマ情報")]
    [SerializeField] private string title;
    [SerializeField] private CardAttribute targetAttribute;
    
    public string Title => title;
    public CardAttribute TargetAttribute => targetAttribute;
}