using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 立ち絵スロットの配置と表示を担当するView
/// </summary>
public class PortraitStage : MonoBehaviour
{
    [Serializable]
    private struct LayoutEntry
    {
        public string layoutId;
        public Vector2[] slotPositions;
    }

    [SerializeField] private DialogCharacterView[] slots;
    [SerializeField] private LayoutEntry[] layouts;

    public int SlotCount => slots.Length;

    public UniTask SetSpriteAsync(int slotIndex, Sprite sprite) => slots[slotIndex].SetCharacterImageAsync(sprite);
    public UniTask FadeInAsync(int slotIndex) => slots[slotIndex].FadeIn();
    public UniTask FadeOutAsync(int slotIndex) => slots[slotIndex].FadeOut();

    /// <summary>
    /// レイアウトIDに対応するスロット座標を適用する
    /// </summary>
    public void ApplyLayout(string layoutId)
    {
        foreach (var entry in layouts)
        {
            if (entry.layoutId != layoutId) continue;
            for (var i = 0; i < slots.Length && i < entry.slotPositions.Length; i++)
                ((RectTransform)slots[i].transform).anchoredPosition = entry.slotPositions[i];
            return;
        }
        Debug.LogWarning($"[PortraitStage] 未定義のレイアウト: {layoutId}");
    }
}
