using TMPro;
using UnityEngine;

/// <summary>
/// テーマ詳細情報を表示するモーダルViewクラス
/// </summary>
public class ThemeDetailView : BaseWindowView
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>
    /// テーマ詳細を表示
    /// </summary>
    /// <param name="themeData">表示するテーマデータ</param>
    public void ShowThemeDetail(string title, string description)
    {
        // テーマ詳細情報を設定
        UpdateThemeDisplay(title, description);

        // パネルを表示
        Show();
    }

    /// <summary>
    /// テーマ表示を更新
    /// </summary>
    /// <param name="themeData">表示するテーマデータ</param>
    private void UpdateThemeDisplay(string title, string description)
    {
        titleText.text = $"「{title}」";
        descriptionText.text = description;
    }
}
