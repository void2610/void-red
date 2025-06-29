using TMPro;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// テーマ表示を担当するViewクラス
/// </summary>
public class ThemeView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI themeText;

    /// <summary>
    /// テーマをアニメーション付きで表示
    /// </summary>
    public void DisplayTheme(string themeTitle)
    {
        var fullText = $"『{themeTitle}』";
        
        LMotion.String.Create128Bytes("", fullText, 1f)
            .WithEase(Ease.OutQuad)
            .WithScrambleChars(ScrambleMode.None)
            .BindToText(themeText)
            .AddTo(gameObject);
    }
}