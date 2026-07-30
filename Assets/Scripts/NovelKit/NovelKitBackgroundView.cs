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
    public UniTask ShowStillAsync(ResolvedSprite still, CancellationToken ct) => ShowAsync(still, ct);

    public async UniTask ShowAsync(ResolvedSprite background, CancellationToken ct)
    {
        if (!background.IsLoaded)
        {
            // 消去指示 (空キー) は黒背景のまま据え置き、ロード失敗だけ警告する
            if (!background.IsCleared) Debug.LogWarning($"[NovelKitBackgroundView] 背景の解決に失敗: {background.Key}");
            return;
        }

        await backgroundView.SetBackground(background.Sprite).AttachExternalCancellation(ct);
    }
}
