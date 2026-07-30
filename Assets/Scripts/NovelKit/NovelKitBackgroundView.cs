using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using UnityEngine;

/// <summary>
/// novel-kit のIBackgroundView実装
/// 既存のDialogBackgroundView (黒経由フェード) へ委譲する
/// </summary>
public class NovelKitBackgroundView : MonoBehaviour, IBackgroundView
{
    [SerializeField] private DialogBackgroundView backgroundView;

    // イベントCGも背景と同じ全画面レイヤーで表示する (専用素材が増えたら分離する)
    public UniTask ShowStillAsync(Sprite sprite, CancellationToken ct) => ShowAsync(sprite, ct);

    public async UniTask ShowAsync(Sprite sprite, CancellationToken ct)
    {
        if (!sprite)
        {
            Debug.LogWarning("[NovelKitBackgroundView] 背景の解決に失敗");
            return;
        }

        await backgroundView.SetBackground(sprite).AttachExternalCancellation(ct);
    }
}
