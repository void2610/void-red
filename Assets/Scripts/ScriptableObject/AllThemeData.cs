using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全てのThemeDataを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "AllThemeData", menuName = "VoidRed/All Theme Data")]
public class AllThemeData : ScriptableObject
{
    [SerializeField] private List<ThemeData> themeList = new ();
    
    // プロパティ
    public List<ThemeData> ThemeList => themeList;
    public int Count => themeList.Count;
    
    /// <summary>
    /// 同じディレクトリ内の全てのThemeDataを自動的に登録
    /// </summary>
    public void RegisterAllThemes()
    {
#if UNITY_EDITOR
        // このScriptableObjectと同じディレクトリパスを取得
        var path = AssetDatabase.GetAssetPath(this);
        path = System.IO.Path.GetDirectoryName(path);
        
        // 指定ディレクトリ内の全てのThemeDataを検索
        var guids = AssetDatabase.FindAssets("t:ThemeData", new[] { path });
        
        // 検索結果をリストに追加
        themeList.Clear();
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var themeData = AssetDatabase.LoadAssetAtPath<ThemeData>(assetPath);
            if (themeData) themeList.Add(themeData);
        }
        
        // テーマ名でソート
        themeList = themeList.OrderBy(x => x.name).ToList();
        
        // ScriptableObjectを更新
        EditorUtility.SetDirty(this);
        
        Debug.Log($"テーマを{themeList.Count}個登録しました");
#endif
    }
    
    /// <summary>
    /// ランダムなテーマを取得
    /// </summary>
    public ThemeData GetRandomTheme()
    {
        if (themeList.Count == 0) return null;
        return themeList[Random.Range(0, themeList.Count)];
    }
    
    /// <summary>
    /// 複数のランダムなテーマを取得（重複なし）
    /// </summary>
    public List<ThemeData> GetRandomThemes(int count)
    {
        if (count >= themeList.Count)
        {
            return new List<ThemeData>(themeList);
        }
        
        var shuffled = new List<ThemeData>(themeList);
        for (int i = 0; i < shuffled.Count; i++)
        {
            var temp = shuffled[i];
            var randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        
        return shuffled.Take(count).ToList();
    }
    
    /// <summary>
    /// 指定されたCardStatusに最も近いテーマを取得
    /// </summary>
    public ThemeData GetClosestTheme(CardStatus targetStatus)
    {
        if (themeList.Count == 0 || targetStatus == null) return null;
        
        ThemeData closestTheme = null;
        float minDistance = float.MaxValue;
        
        foreach (var theme in themeList)
        {
            if (theme.CardStatus == null) continue;
            
            var distance = theme.CardStatus.GetDistanceTo(targetStatus);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTheme = theme;
            }
        }
        
        return closestTheme;
    }
}