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

        await _backgroundImage.FadeOut(fadeDuration, Ease.InOutQuad).AddTo(gameObject).ToUniTask(cancellationToken: ct);

        _backgroundImage.sprite = background.Sprite;

        await _backgroundImage.FadeIn(fadeDuration, Ease.InOutQuad).AddTo(gameObject).ToUniTask(cancellationToken: ct);
    }

    private void Awake()
    {
        _backgroundImage = GetComponent<Image>();
    }
}
