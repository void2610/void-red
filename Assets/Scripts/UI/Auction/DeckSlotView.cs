using LitMotion;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// デッキスロットView
/// ドラッグされたカードを受け入れるドロップ先
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DeckSlotView : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Sprite normalSlotSprite;
    [SerializeField] private Sprite lockedSlotSprite;
    [SerializeField] private Image slotImage;
    [SerializeField] private Image highlightImage;

    public bool IsOccupied => PlacedCard;
    public Transform CardAnchor => this.transform;
    public DraggableCardView PlacedCard { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    public Observable<(DeckSlotView slot, DraggableCardView card)> OnCardDropped => _onCardDropped;

    private const float FADE_DURATION = 0.15f;

    private readonly Subject<(DeckSlotView slot, DraggableCardView card)> _onCardDropped = new();
    private MotionHandle _fadeTween;

    public void SetLocked(bool locked)
    {
        var sprite = locked ? lockedSlotSprite : normalSlotSprite;
        slotImage.sprite = sprite;
    }

    /// <summary>
    /// スロットにカードを配置
    /// </summary>
    public void PlaceCard(DraggableCardView card)
    {
        PlacedCard = card;
        card.SetSlot(this);
        SetHighlight(true);
    }

    /// <summary>
    /// スロットからカードを取り外す
    /// </summary>
    public DraggableCardView RemoveCard()
    {
        var card = PlacedCard;
        if (PlacedCard)
        {
            PlacedCard.SetSlot(null);
        }
        PlacedCard = null;
        SetHighlight(false);
        return card;
    }

    private void SetHighlight(bool highlight)
    {
        _fadeTween.TryCancel();
        _fadeTween = highlight
            ? highlightImage.FadeIn(FADE_DURATION, Ease.OutQuad)
            : highlightImage.FadeOut(FADE_DURATION, Ease.OutQuad);
    }

    /// <summary>
    /// スロットにカードがドロップされた時の処理
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        var draggableCard = eventData.pointerDrag?.GetComponent<DraggableCardView>();
        if (!draggableCard) return;

        _onCardDropped.OnNext((this, draggableCard));
    }

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        highlightImage.SetAlpha(0f);
    }

    private void OnDestroy()
    {
        _fadeTween.TryCancel();
        _onCardDropped.Dispose();
    }
}
