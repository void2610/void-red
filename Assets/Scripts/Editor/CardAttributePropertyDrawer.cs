using UnityEngine;
using UnityEditor;

/// <summary>
/// CardAttribute enumのカスタムProperty Drawer
/// Inspectorで日本語名を表示する
/// </summary>
[CustomPropertyDrawer(typeof(CardAttribute))]
public class CardAttributePropertyDrawer : PropertyDrawer
{
    /// <summary>
    /// 属性の日本語名を取得
    /// </summary>
    private static string GetAttributeJapaneseName(CardAttribute attribute)
    {
        return attribute switch
        {
            CardAttribute.Forgiveness => "赦し",
            CardAttribute.Anger => "怒り",
            CardAttribute.Anxiety => "不安",
            CardAttribute.Rejection => "拒絶",
            CardAttribute.Loss => "喪失",
            CardAttribute.Hope => "希望",
            _ => "不明"
        };
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 現在の値を取得
        var currentValue = (CardAttribute)property.enumValueIndex;
        
        // 選択肢を作成（日本語名を含む）
        var enumValues = System.Enum.GetValues(typeof(CardAttribute));
        var options = new string[enumValues.Length];
        
        for (int i = 0; i < enumValues.Length; i++)
        {
            var value = (CardAttribute)enumValues.GetValue(i);
            var japaneseName = GetAttributeJapaneseName(value);
            options[i] = $"{value} ({japaneseName})";
        }
        
        // ドロップダウンを表示
        var selectedIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, options);
        property.enumValueIndex = selectedIndex;
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}