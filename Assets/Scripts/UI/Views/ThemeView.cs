using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VFX;
using Void2610.UnityTemplate;

/// <summary>
/// テーマ表示を担当するViewクラス
/// マウスオーバー時にキーワードを表示
/// </summary>
public class ThemeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI themeText;
    [SerializeField] private Transform keywordContainer;
    [SerializeField] private VisualEffect visualEffect;

    private string _themeTitle;
    private bool _isKeywordsVisible;
    private CancellationTokenSource _textSeCancellationTokenSource;

    /// <summary>
    /// テーマとキーワードを表示
    /// </summary>
    /// <param name="themeData">テーマデータ</param>
    public async UniTask DisplayThemeWithKeywords(string themeTitle, bool isMainTheme)
    {
        OnPointerExit(null);
        await UniTask.Delay(500, cancellationToken: destroyCancellationToken);

        // メインテーマならVFX再生
        visualEffect.SetInt("Rate", isMainTheme ? 1 : 0);

        // BGMボリュームを下げてからME再生
        await BgmManager.Instance.DuckVolume();
        SeManager.Instance.PlaySe("ThemeAppearance", pitch: 1f);
        _themeTitle = themeTitle;

        // テキスト文字送りSEループ開始
        _textSeCancellationTokenSource?.Cancel();
        _textSeCancellationTokenSource?.Dispose();
        _textSeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        SeManager.Instance.PlaySeLoop("SE_TEXT_NEXT", cancellationToken: _textSeCancellationTokenSource.Token).Forget();

        await themeText.TypewriterAnimation(themeTitle);

        _textSeCancellationTokenSource?.Cancel();
        _textSeCancellationTokenSource?.Dispose();
        _textSeCancellationTokenSource = null;

        await UniTask.Delay(4000, cancellationToken: destroyCancellationToken);
        BgmManager.Instance.RestoreVolume().Forget();
    }

    /// <summary>
    /// キーワード表示をトグル（InputSystemアクション用）
    /// </summary>
    public void ToggleKeywords()
    {
        if (!_isKeywordsVisible && string.IsNullOrEmpty(_themeTitle)) return;

        if (!_isKeywordsVisible) OnPointerEnter(null);
        else OnPointerExit(null);
    }

    private void Awake()
    {
        visualEffect.SetInt("Rate", 0);
    }

    /// <summary>
    /// マウスカーソルが乗ったときの処理
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isKeywordsVisible || string.IsNullOrEmpty(_themeTitle)) return;

        VolumeController.Instance.SetScreenSpaceLensFlareIntensity(1f);
        _isKeywordsVisible = true;
    }

    /// <summary>
    /// マウスカーソルが離れたときの処理
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isKeywordsVisible || string.IsNullOrEmpty(_themeTitle)) return;

        VolumeController.Instance.SetScreenSpaceLensFlareIntensity(0f);
        _isKeywordsVisible = false;
    }
}
