using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;
using VContainer;

/// <summary>
/// novel-kit のIBackgroundView実装
/// 既存のDialogBackgroundView (黒経由フェード) に背景差し替えを委譲する
/// </summary>
public class NovelKitBackgroundView : MonoBehaviour, IBackgroundView
{
    [SerializeField] private DialogBackgroundView backgroundView;

    private AddressableImageLoader _imageLoader;

    [Inject]
    public void Construct(AddressableImageLoader imageLoader) => _imageLoader = imageLoader;

    // イベントCGも背景と同じ全画面レイヤーで表示する (専用素材が増えたら分離する)
    public UniTask ShowStillAsync(string stillKey, CancellationToken ct) => ShowAsync(stillKey, ct);

    public async UniTask ShowAsync(string backgroundKey, CancellationToken ct)
    {
        var sprite = await _imageLoader.LoadBackgroundImageAsync(backgroundKey).AttachExternalCancellation(ct);
        if (!sprite)
        {
            Debug.LogWarning($"[NovelKitBackgroundView] 背景が見つからない: {backgroundKey}");
            return;
        }
        await backgroundView.SetBackground(sprite).AttachExternalCancellation(ct);
    }
}
