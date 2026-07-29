using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using Novel.Runtime;
using UnityEngine;

/// <summary>
/// novel-kit のIPortraitView実装
/// キーからスプライトを解決し、描画はPortraitStageへ委譲する
/// </summary>
public class NovelKitPortraitView : IPortraitView
{
    private readonly PortraitStage _stage;
    private readonly ISpriteLoader _spriteLoader;
    private readonly HashSet<int> _visibleSlots = new();

    public NovelKitPortraitView(PortraitStage stage, ISpriteLoader spriteLoader)
    {
        _stage = stage;
        _spriteLoader = spriteLoader;
    }

    // 座標を書き換えるだけなので待機は発生しない
    public UniTask SwitchLayoutAsync(PortraitLayout layout, CancellationToken ct)
    {
        _stage.ApplyLayout(layout.Id);
        return UniTask.CompletedTask;
    }

    public async UniTask ShowAsync(int slotIndex, string character, string portraitKey, CancellationToken ct)
    {
        if (slotIndex < 0 || slotIndex >= _stage.SlotCount)
        {
            Debug.LogWarning($"[NovelKitPortraitView] slot 範囲外: {slotIndex} ({character})");
            return;
        }

        var sprite = await _spriteLoader.LoadAsync(portraitKey, ct);
        if (!sprite)
        {
            Debug.LogWarning($"[NovelKitPortraitView] 立ち絵が見つからない: {portraitKey}");
            return;
        }

        await _stage.SetSpriteAsync(slotIndex, sprite).AttachExternalCancellation(ct);
        if (_visibleSlots.Add(slotIndex)) await _stage.FadeInAsync(slotIndex).AttachExternalCancellation(ct);
    }

    public async UniTask HideAsync(int slotIndex, CancellationToken ct)
    {
        if (!_visibleSlots.Remove(slotIndex)) return;
        await _stage.FadeOutAsync(slotIndex).AttachExternalCancellation(ct);
    }
}
