using UnityEngine;

/// <summary>
/// 洗礼で並べる落札記憶 1 枚分の札
/// </summary>
public class AcquiredCardView : MonoBehaviour
{
    [SerializeField] private CardView cardView;

    public WonLot WonLot { get; private set; }
    public CardView CardView => cardView;

    public void SetSelectable(bool selectable) => cardView.SetInteractable(selectable);

    public void SetSelected(bool selected) => cardView.SetHighlight(selected);

    public void Initialize(WonLot wonLot)
    {
        WonLot = wonLot;
        cardView.Initialize(wonLot.Lot);
    }
}
