using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// 属性倍率設定用のポップアップウィンドウ
/// </summary>
public class AttributeMultiplierWindow : EditorWindow
{
    private Dictionary<CardAttribute, float> _multipliers = new Dictionary<CardAttribute, float>();
    private ThemeData _targetThemeData;
    
    /// <summary>
    /// ウィンドウを開く
    /// </summary>
    /// <param name="themeData">編集対象のThemeData</param>
    public static void Open(ThemeData themeData)
    {
        var window = GetWindow<AttributeMultiplierWindow>(true, "属性倍率設定", true);
        window._targetThemeData = themeData;
        window.InitializeValues();
        window.minSize = new Vector2(300, 350);
        window.maxSize = new Vector2(300, 350);
        window.ShowModal();
    }
    
    /// <summary>
    /// 初期値を設定
    /// </summary>
    private void InitializeValues()
    {
        _multipliers.Clear();
        
        // 全ての属性を初期値1.0で初期化
        var allAttributes = Enum.GetValues(typeof(CardAttribute));
        foreach (CardAttribute attribute in allAttributes)
        {
            // 既存の値があれば使用、なければ1.0
            float existingValue = _targetThemeData?.GetMultiplier(attribute) ?? 1.0f;
            _multipliers[attribute] = existingValue;
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("属性倍率設定", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (_targetThemeData != null)
        {
            EditorGUILayout.LabelField($"テーマ: {_targetThemeData.Title}", EditorStyles.helpBox);
            EditorGUILayout.Space();
        }
        
        // 各属性の倍率入力フィールド
        var allAttributes = Enum.GetValues(typeof(CardAttribute));
        foreach (CardAttribute attribute in allAttributes)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 属性名（日本語）を表示
            EditorGUILayout.LabelField(attribute.ToJapaneseName(), GUILayout.Width(60));
            
            // 倍率入力フィールド
            if (_multipliers.ContainsKey(attribute))
            {
                _multipliers[attribute] = EditorGUILayout.FloatField(_multipliers[attribute]);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 一括設定ボタン群
        EditorGUILayout.LabelField("一括設定:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("全て0.5"))
        {
            SetAllValues(0.5f);
        }
        if (GUILayout.Button("全て1.0"))
        {
            SetAllValues(1.0f);
        }
        if (GUILayout.Button("全て1.5"))
        {
            SetAllValues(1.5f);
        }
        if (GUILayout.Button("全て2.0"))
        {
            SetAllValues(2.0f);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 確定・キャンセルボタン
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("確定"))
        {
            ApplyValues();
            Close();
        }
        
        if (GUILayout.Button("キャンセル"))
        {
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 全ての値を指定した値に設定
    /// </summary>
    /// <param name="value">設定する値</param>
    private void SetAllValues(float value)
    {
        var keys = new List<CardAttribute>(_multipliers.Keys);
        foreach (var key in keys)
        {
            _multipliers[key] = value;
        }
    }
    
    /// <summary>
    /// 設定した値をThemeDataに適用
    /// </summary>
    private void ApplyValues()
    {
        if (_targetThemeData == null) return;
        
        // Undoに登録
        Undo.RecordObject(_targetThemeData, "Set Attribute Multipliers");
        
        // 値を適用
        foreach (var kvp in _multipliers)
        {
            _targetThemeData.AttributeMultipliers[kvp.Key] = kvp.Value;
        }
        
        // 変更をマーク
        EditorUtility.SetDirty(_targetThemeData);
        
        Debug.Log($"ThemeData '{_targetThemeData.Title}' に属性倍率を設定しました");
    }
}