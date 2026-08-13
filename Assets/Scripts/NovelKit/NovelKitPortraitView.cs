using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using Novel.Assets;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// novel-kit のIPortraitChannel実装
/// レイアウト別のスロット座標へ立ち絵を配置し、表示・退場を行う
/// </summary>
public class NovelKitPortraitView : MonoBehaviour, IPortraitChannel, IStageLayoutEditor
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

    public IEnumerable<StageLayoutInfo> EnumerateLayouts() => layouts.Select(e => new StageLayoutInfo(e.layoutId, e.slotPositions?.Length ?? 0));

    public IReadOnlyList<Vector2> GetLayoutPositions(string layoutId) => layouts.FirstOrDefault(e => e.layoutId == layoutId).slotPositions ?? Array.Empty<Vector2>();

    public IReadOnlyList<Vector2> GetCurrentSlotPositions() => slots.Select(s => ((RectTransform)s.group.transform).anchoredPosition).ToArray();

    public void SetLayoutPositions(string layoutId, IReadOnlyList<Vector2> positions)
    {
        for (var i = 0; i < layouts.Length; i++)
        {
            if (layouts[i].layoutId != layoutId) continue;

            layouts[i].slotPositions = positions.ToArray();
            return;
        }
    }

    // 座標を書き換えるだけなので待機は発生しない
    public UniTask SwitchLayoutAsync(PortraitLayout layout, CancellationToken ct)
    {
        ApplyLayout(layout.Id);
        return UniTask.CompletedTask;
    }

    public async UniTask ShowAsync(int slotIndex, ResolvedSprite portrait, CancellationToken ct)
    {
        if (!IsValidSlot(slotIndex)) return;
        if (!portrait.IsLoaded)
        {
            // 消去指示 (空キー) は退場として扱い、ロード失敗だけ警告する
            if (portrait.IsCleared) await HideAsync(slotIndex, ct);
            else Debug.LogWarning($"[NovelKitPortraitView] 立ち絵の解決に失敗: {portrait.Key}");
            return;
        }

        var slot = slots[slotIndex];
        if (!Application.isPlaying)
        {
            // 編集モードではモーションが進まないので、待たずに最終状態へ飛ばす
            slot.image.SetImmediate(portrait.Sprite);
            slot.group.alpha = 1f;
            _visibleSlots.Add(slotIndex);
            return;
        }

        await slot.image.CrossfadeAsync(portrait.Sprite, ct);
        if (_visibleSlots.Add(slotIndex))
            await slot.group.FadeIn(fadeDuration).ToUniTask(cancellationToken: ct);
    }

    public async UniTask HideAsync(int slotIndex, CancellationToken ct)
    {
        if (!IsValidSlot(slotIndex)) return;

        if (!Application.isPlaying)
        {
            // プレビューの片付け。参照を残すとプレハブがその立ち絵を抱え込むのでスプライトごと外す
            _visibleSlots.Remove(slotIndex);
            slots[slotIndex].image.Clear();
            slots[slotIndex].group.alpha = 0f;
            return;
        }

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
