using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using UnityEngine;

/// <summary>
/// novel-kit のIBackgroundChannel / IStillChannel実装
/// 既存のDialogBackgroundView (黒経由フェード) へ委譲する。イベントCGも同じ全画面レイヤーで出す
/// </summary>
public class NovelKitBackgroundView : MonoBehaviour, IBackgroundChannel, IStillChannel
{
    [SerializeField] private DialogBackgroundView backgroundView;

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
