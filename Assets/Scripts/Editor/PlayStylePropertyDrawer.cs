using UnityEngine;
using UnityEditor;

/// <summary>
/// PlayStyle enumのカスタムProperty Drawer
/// Inspectorで日本語名を表示する
/// </summary>
[CustomPropertyDrawer(typeof(PlayStyle))]
public class PlayStylePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 選択肢を作成（日本語名を含む）
        var enumValues = System.Enum.GetValues(typeof(PlayStyle));
        var options = new string[enumValues.Length];
        
        for (var i = 0; i < enumValues.Length; i++)
        {
            var value = (PlayStyle)enumValues.GetValue(i);
            var japaneseName = value.ToJapaneseString();
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