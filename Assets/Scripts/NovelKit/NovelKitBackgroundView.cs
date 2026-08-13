using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using Novel.Assets;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のIBackgroundChannel / IStillChannel実装
/// 切り替えは黒を経由してフェードする。イベントCGも同じ全画面レイヤーで出す
/// </summary>
[RequireComponent(typeof(Image))]
public class NovelKitBackgroundView : MonoBehaviour, IBackgroundChannel, IStillChannel
{
    [SerializeField] private float fadeDuration = 0.5f;

    private Image _backgroundImage;

    public async UniTask ShowAsync(ResolvedSprite background, CancellationToken ct)
    {
        if (!background.IsLoaded)
        {
            // 消去指示 (空キー) は黒背景のまま据え置き、ロード失敗だけ警告する
            if (!background.IsCleared) Debug.LogWarning($"[NovelKitBackgroundView] 背景の解決に失敗: {background.Key}");
            return;
        }

        if (_backgroundImage.sprite == background.Sprite) return;

        // 破棄が先に来てもフェード待ちが残らないよう、再生側のctと破棄トークンを併せて待つ
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

        await _backgroundImage.FadeOut(fadeDuration, Ease.InOutQuad).AddTo(gameObject).ToUniTask(cancellationToken: cts.Token);

        _backgroundImage.sprite = background.Sprite;

        await _backgroundImage.FadeIn(fadeDuration, Ease.InOutQuad).AddTo(gameObject).ToUniTask(cancellationToken: cts.Token);
    }

    private void Awake()
    {
        _backgroundImage = GetComponent<Image>();
    }
}
