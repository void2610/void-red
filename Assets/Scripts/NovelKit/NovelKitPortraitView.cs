using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;
using VContainer;

/// <summary>
/// novel-kit のIPortraitView実装
/// レイアウト別のスロット座標へDialogCharacterViewを配置し、スプライト差し替えを行う
/// </summary>
public class NovelKitPortraitView : MonoBehaviour, IPortraitView
{
    [Serializable]
    private struct LayoutEntry
    {
        public string layoutId;
        public Vector2[] slotPositions;
    }

    [SerializeField] private DialogCharacterView[] slots;
    [SerializeField] private LayoutEntry[] layouts;

    private readonly Dictionary<int, bool> _visible = new();
    private AddressableImageLoader _imageLoader;

    [Inject]
    public void Construct(AddressableImageLoader imageLoader) => _imageLoader = imageLoader;

    public async UniTask SwitchLayoutAsync(PortraitLayout layout, CancellationToken ct)
    {
        foreach (var entry in layouts)
        {
            if (entry.layoutId != layout.Id) continue;
            for (var i = 0; i < slots.Length && i < entry.slotPositions.Length; i++)
                ((RectTransform)slots[i].transform).anchoredPosition = entry.slotPositions[i];
            return;
        }
        Debug.LogWarning($"[NovelKitPortraitView] 未定義のレイアウト: {layout.Id}");
        await UniTask.CompletedTask;
    }

    public async UniTask ShowAsync(int slotIndex, string character, string portraitKey, CancellationToken ct)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[NovelKitPortraitView] slot 範囲外: {slotIndex} ({character})");
            return;
        }

        var sprite = await _imageLoader.LoadCharacterImageAsync(portraitKey);
        ct.ThrowIfCancellationRequested();
        if (!sprite)
        {
            Debug.LogWarning($"[NovelKitPortraitView] 立ち絵が見つからない: {portraitKey}");
            return;
        }

        var slot = slots[slotIndex];
        slot.SetCharacterImage(sprite);
        if (!_visible.GetValueOrDefault(slotIndex))
        {
            _visible[slotIndex] = true;
            await slot.FadeIn();
        }
    }

    public async UniTask HideAsync(int slotIndex, CancellationToken ct)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        if (!_visible.GetValueOrDefault(slotIndex)) return;

        _visible[slotIndex] = false;
        await slots[slotIndex].FadeOut();
    }
}
