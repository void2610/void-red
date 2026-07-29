using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using Novel.Runtime;
using UnityEngine;

/// <summary>
/// novel-kit の IPortraitView を満たし、自前の PortraitView へ委譲するアダプタ
/// キーからスプライトを解決し、スロットの表示状態を管理する
/// </summary>
public class NovelKitPortraitAdapter : IPortraitView
{
    private readonly PortraitView _view;
    private readonly ISpriteLoader _spriteLoader;
    private readonly HashSet<int> _visibleSlots = new();

    public NovelKitPortraitAdapter(PortraitView view, ISpriteLoader spriteLoader)
    {
        _view = view;
        _spriteLoader = spriteLoader;
    }

    // 座標を書き換えるだけなので待機は発生しない
    public UniTask SwitchLayoutAsync(PortraitLayout layout, CancellationToken ct)
    {
        _view.ApplyLayout(layout.Id);
        return UniTask.CompletedTask;
    }

    public async UniTask ShowAsync(int slotIndex, string character, string portraitKey, CancellationToken ct)
    {
        // 範囲外ならロード自体が無駄なので先に弾く (PortraitView 側も public API として自衛する)
        if (slotIndex < 0 || slotIndex >= _view.SlotCount)
        {
            Debug.LogWarning($"[NovelKitPortraitAdapter] slot 範囲外: {slotIndex} ({character})");
            return;
        }

        var sprite = await _spriteLoader.LoadAsync(portraitKey, ct);
        if (!sprite)
        {
            Debug.LogWarning($"[NovelKitPortraitAdapter] 立ち絵が見つからない: {portraitKey}");
            return;
        }

        await _view.SetSpriteAsync(slotIndex, sprite).AttachExternalCancellation(ct);
        if (_visibleSlots.Add(slotIndex)) await _view.FadeInAsync(slotIndex).AttachExternalCancellation(ct);
    }

    public async UniTask HideAsync(int slotIndex, CancellationToken ct)
    {
        if (!_visibleSlots.Remove(slotIndex)) return;
        await _view.FadeOutAsync(slotIndex).AttachExternalCancellation(ct);
    }
}
