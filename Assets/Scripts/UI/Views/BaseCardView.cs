using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CardView と DeckCardView の共通基底クラス
/// カードの基本表示ロジックを提供
/// </summary>
public abstract class BaseCardView : MonoBehaviour
{
    // 抽象プロパティ：各サブクラスで実装
    protected abstract Image CardImage { get; }
    protected abstract Image CurtainImage { get; }
    protected abstract TextMeshProUGUI CardNameText { get; }
    protected abstract Image CardFrame { get; }

    private TMProArchedText _archedText;
    private bool _archedTextCached;

    // 表示中のロット取得（各サブクラスで実装）
    protected abstract MemoryLotData GetLot();
    protected abstract Sprite GetCurtainSprite();

    /// <summary>
    /// 札の基本表示を更新（画像、名前、カーテン、フレーム）
    /// </summary>
    protected void UpdateCardDisplay()
    {
        var lot = GetLot();
        if (!lot) return;

        // 記憶の画像と名前を設定
        if (lot.Image) CardImage.sprite = lot.Image;
        CardNameText.text = lot.Title;
        CurtainImage.sprite = GetCurtainSprite();

        if (!_archedTextCached)
        {
            _archedText = CardNameText.GetComponent<TMProArchedText>();
            _archedTextCached = true;
        }
        if (_archedText) _archedText.ForceUpdate();

        CardImage.color = lot.Image ? Color.white : Color.clear;
    }

    /// <summary>
    /// 未落札で流れた札をグレーアウトする
    /// </summary>
    protected void ApplyDimmedDisplay()
    {
        CardImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        CardFrame.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    }
}
