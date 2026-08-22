using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 洗礼 (リザルト)。落札した記憶を並べ、1 つを人格に統合させて階層を締める
/// </summary>
public class BaptismView : MonoBehaviour
{
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI remainingText;
    [SerializeField] private TextMeshProUGUI collapsedText;
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private Button finishButton;

    public Observable<WonLot> OnIntegrate => _onIntegrate;
    public Observable<Unit> OnFinish => finishButton.OnClickAsObservable();

    private readonly List<WonLotEntryView> _entries = new();
    private readonly Subject<WonLot> _onIntegrate = new();
    private readonly CompositeDisposable _disposables = new();

    public void Hide() => gameObject.SetActive(false);

    public void Show(AuctionSession session)
    {
        gameObject.SetActive(true);
        foreach (var e in _entries) Destroy(e.gameObject);
        _entries.Clear();
        foreach (var won in session.Player.WonLots)
        {
            var entry = Instantiate(entryPrefab, entryContainer).GetComponent<WonLotEntryView>();
            entry.Bind(won);
            entry.OnIntegrate.Subscribe(_onIntegrate.OnNext).AddTo(_disposables);
            _entries.Add(entry);
        }
        remainingText.text = $"残りリソース {session.Player.Wallet.Total} 枚";
        var collapsed = new List<string>();
        foreach (var r in session.Rivals) if (r.HasCollapsed) collapsed.Add(r.DisplayName);
        collapsedText.text = collapsed.Count == 0 ? "人格崩壊: なし" : $"人格崩壊: {string.Join(", ", collapsed)}";
        selectedText.text = "統合する記憶を選んでください";
        finishButton.interactable = false;
    }

    public void SetSelected(WonLot wonLot)
    {
        foreach (var e in _entries) e.SetSelected(e.WonLot == wonLot);
        selectedText.text = $"『{wonLot.Lot.Title}』を人格に統合する";
        finishButton.interactable = true;
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
