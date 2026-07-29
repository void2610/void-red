using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using Novel.Runtime;
using UnityEngine;

/// <summary>
/// novel-kit のIBackgroundView実装
/// キーからスプライトを解決し、描画はDialogBackgroundView (黒経由フェード) へ委譲する
/// </summary>
public class NovelKitBackgroundView : IBackgroundView
{
    private readonly DialogBackgroundView _view;
    private readonly ISpriteLoader _spriteLoader;

    public NovelKitBackgroundView(DialogBackgroundView view, ISpriteLoader spriteLoader)
    {
        _view = view;
        _spriteLoader = spriteLoader;
    }

    // イベントCGも背景と同じ全画面レイヤーで表示する (専用素材が増えたら分離する)
    public UniTask ShowStillAsync(string stillKey, CancellationToken ct) => ShowAsync(stillKey, ct);

    public async UniTask ShowAsync(string backgroundKey, CancellationToken ct)
    {
        var sprite = await _spriteLoader.LoadAsync(backgroundKey, ct);
        if (!sprite)
        {
            Debug.LogWarning($"[NovelKitBackgroundView] 背景が見つからない: {backgroundKey}");
            return;
        }
        await _view.SetBackground(sprite).AttachExternalCancellation(ct);
    }
}
