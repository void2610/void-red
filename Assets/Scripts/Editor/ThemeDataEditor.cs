using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// ThemeDataのカスタムエディタ
/// 初期値設定ボタンを追加
/// </summary>
[CustomEditor(typeof(ThemeData))]
public class ThemeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを描画
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // 属性倍率設定ボタン
        if (GUILayout.Button("属性倍率を設定..."))
        {
            AttributeMultiplierWindow.Open((ThemeData)target);
        }
        
        EditorGUILayout.Space();
        
        // 現在の設定を表示
        var themeData = (ThemeData)target;
        if (themeData.AttributeMultipliers.Count > 0)
        {
            EditorGUILayout.LabelField("現在の設定:", EditorStyles.boldLabel);
            foreach (var kvp in themeData.AttributeMultipliers)
            {
                EditorGUILayout.LabelField($"{kvp.Key.ToJapaneseName()}: {kvp.Value:F1}");
            }
        }
    }
    
}