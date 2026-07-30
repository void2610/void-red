using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using Novel.Assets;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のIPortraitView実装
/// レイアウト別のスロット座標へ立ち絵を配置し、表示・退場を行う
/// </summary>
public class NovelKitPortraitView : MonoBehaviour, IPortraitView
{
    [Serializable]
    private struct Slot
    {
        public CanvasGroup group;
        public CrossfadeImage image;
    }

    [Serializable]
    private struct LayoutEntry
    {
        public string layoutId;
        public Vector2[] slotPositions;
    }

    [SerializeField] private Slot[] slots;
    [SerializeField] private LayoutEntry[] layouts;
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly HashSet<int> _visibleSlots = new();

    // 座標を書き換えるだけなので待機は発生しない
    public UniTask SwitchLayoutAsync(PortraitLayout layout, CancellationToken ct)
    {
        ApplyLayout(layout.Id);
        return UniTask.CompletedTask;
    }

    public async UniTask ShowAsync(int slotIndex, string character, ResolvedSprite portrait, CancellationToken ct)
    {
        if (!IsValidSlot(slotIndex)) return;
        if (!portrait.IsLoaded)
        {
            // 消去指示 (空キー) は退場として扱い、ロード失敗だけ警告する
            if (portrait.IsCleared) await HideAsync(slotIndex, ct);
            else Debug.LogWarning($"[NovelKitPortraitView] 立ち絵の解決に失敗: {character} ({portrait.Key})");
            return;
        }

        var slot = slots[slotIndex];
        await slot.image.CrossfadeAsync(portrait.Sprite, ct);
        if (_visibleSlots.Add(slotIndex))
            await slot.group.FadeIn(fadeDuration).ToUniTask(cancellationToken: ct);
    }

    public async UniTask HideAsync(int slotIndex, CancellationToken ct)
    {
        if (!_visibleSlots.Remove(slotIndex)) return;

        await slots[slotIndex].group.FadeOut(fadeDuration).ToUniTask(cancellationToken: ct);
    }

    private void ApplyLayout(string layoutId)
    {
        foreach (var entry in layouts)
        {
            if (entry.layoutId != layoutId) continue;
            for (var i = 0; i < slots.Length && i < entry.slotPositions.Length; i++)
                ((RectTransform)slots[i].group.transform).anchoredPosition = entry.slotPositions[i];
            return;
        }
        Debug.LogWarning($"[NovelKitPortraitView] 未定義のレイアウト: {layoutId}");
    }

    private bool IsValidSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Length) return true;

        Debug.LogWarning($"[NovelKitPortraitView] slot 範囲外: {slotIndex} (SlotCount={slots.Length})");
        return false;
    }
}
