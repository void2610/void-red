using UnityEngine;

/// <summary>
/// テーマデータを保持するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewThemeData", menuName = "VoidRed/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("テーマ情報")]
    [SerializeField] private string title;
    [SerializeField] private CardStatus cardStatus;
    
    // プロパティ
    public string Title => title;
    public CardStatus CardStatus => cardStatus;
}