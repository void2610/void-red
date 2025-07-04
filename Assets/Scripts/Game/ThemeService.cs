using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// テーマを管理するサービスクラス
/// VContainerによりシングルトンとして管理される
/// </summary>
public class ThemeService
{
    public int AvailableThemeCount => _availableThemes.Count;
    public int TotalThemeCount => _allThemeData.Count;
    
    private readonly AllThemeData _allThemeData;
    private readonly List<ThemeData> _availableThemes;
    
    /// <summary>
    /// コンストラクタ（AllThemeDataをDIで受け取る）
    /// </summary>
    /// <param name="allThemeData">全テーマデータリスト</param>
    public ThemeService(AllThemeData allThemeData)
    {
        _allThemeData = allThemeData;
        _allThemeData.RegisterAllThemes(); // 全テーマを登録
        _availableThemes = new List<ThemeData>(_allThemeData.ThemeList);
    }
    
    /// <summary>
    /// ランダムなテーマを1つ取得
    /// </summary>
    /// <returns>ランダムなテーマ</returns>
    public ThemeData GetRandomTheme()
    {
        if (_availableThemes.Count == 0)
        {
            return null;
        }
        
        var randomIndex = Random.Range(0, _availableThemes.Count);
        return _availableThemes[randomIndex];
    }
    
    /// <summary>
    /// 複数のランダムなテーマを取得（重複あり）
    /// </summary>
    /// <param name="count">取得するテーマ数</param>
    /// <returns>ランダムなテーマのリスト</returns>
    public List<ThemeData> GetRandomThemes(int count)
    {
        if (count <= 0) return new List<ThemeData>();
        if (_availableThemes.Count == 0)
        {
            return new List<ThemeData>();
        }
        
        var result = new List<ThemeData>();
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(0, _availableThemes.Count);
            result.Add(_availableThemes[randomIndex]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 指定した条件に合うテーマを取得
    /// </summary>
    /// <param name="predicate">検索条件</param>
    /// <returns>条件に合うテーマのリスト</returns>
    public List<ThemeData> GetThemesWhere(System.Func<ThemeData, bool> predicate)
    {
        return _availableThemes.Where(predicate).ToList();
    }
    
    /// <summary>
    /// 特定のテーマ名でテーマを取得
    /// </summary>
    /// <param name="themeName">テーマ名</param>
    /// <returns>見つかったテーマ（存在しない場合はnull）</returns>
    public ThemeData GetThemeByName(string themeName)
    {
        return _availableThemes.FirstOrDefault(theme => theme.name == themeName);
    }
}