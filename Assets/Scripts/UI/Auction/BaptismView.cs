using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 洗礼 (リザルト)
/// 落札した記憶を札で並べ、内訳と歪みを見せて 1 つを人格に統合させる
/// </summary>
public class BaptismView : BasePhaseView
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI remainingText;
    [SerializeField] private TextMeshProUGUI collapsedText;
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private CardAcquisitionView acquisitionView;
    [SerializeField] private Button finishButton;

    /// <summary>選ばれている記憶 (未選択なら null)</summary>
    public WonLot SelectedLot { get; private set; }

    /// <summary>洗礼を受ける操作が行われたか (押下の取りこぼしを避けるためフラグで持つ)</summary>
    public bool FinishRequested { get; private set; }

    private readonly CompositeDisposable _disposables = new();

    public async UniTask ShowAsync(AuctionSession session)
    {
        Show();
        headerText.text = session.IsThemeClarified
            ? $"洗礼 — 記憶テーマが鮮明になった\n「{session.Floor.ThemeTitle}」→「{session.Floor.ClarifiedTheme}」"
            : $"洗礼 — 記憶テーマ「{session.Floor.ThemeTitle}」";
        remainingText.text = $"残りリソース {session.Player.Wallet.Total} 枚";
        var collapsed = session.Rivals.Where(r => r.HasCollapsed).Select(r => r.DisplayName).ToList();
        collapsedText.text = collapsed.Count == 0 ? "人格崩壊: なし" : $"人格崩壊: {string.Join(", ", collapsed)}";
        selectedText.text = "人格に統合する記憶を 1 つ選ぶ";
        SelectedLot = null;
        FinishRequested = false;
        finishButton.interactable = false;

        await acquisitionView.DisplayLotsAsync(session.Player.WonLots);
    }

    public override void Hide()
    {
        acquisitionView.Hide();
        base.Hide();
    }

    private void SetSelected(WonLot wonLot)
    {
        SelectedLot = wonLot;
        selectedText.text = $"『{wonLot.Lot.Title}』を人格に統合する";
        finishButton.interactable = true;
    }

    protected override void Awake()
    {
        base.Awake();
        // 選択と完了の受け付けは View 内で完結させる (購読側の準備状況に左右されない)
        acquisitionView.OnLotSelected.Subscribe(SetSelected).AddTo(_disposables);
        finishButton.OnClickAsObservable().Subscribe(_ => FinishRequested = SelectedLot != null).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
