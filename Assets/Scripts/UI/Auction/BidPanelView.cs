using System.Collections.Generic;
using System.Linq;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 入札フェーズのパネル。8 属性の行を持ち、合計枚数と確定ボタンを出す
/// 競合フェーズでは同じ行を 1 枚ずつ上乗せするボタンとして使う
/// </summary>
public class BidPanelView : MonoBehaviour
{
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private Button confirmButton;

    public Observable<EmotionType> OnPlus => _onPlus;
    public Observable<EmotionType> OnMinus => _onMinus;
    public Observable<Unit> OnConfirm => confirmButton.OnClickAsObservable();

    private readonly List<EmotionBidItemView> _items = new();
    private readonly Subject<EmotionType> _onPlus = new();
    private readonly Subject<EmotionType> _onMinus = new();
    private readonly CompositeDisposable _disposables = new();

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    public void SetConfirmInteractable(bool on) => confirmButton.interactable = on;

    public void Refresh(EmotionWallet wallet, EmotionBid bid)
    {
        EnsureItems();
        foreach (var item in _items) item.Refresh(wallet.Get(item.Emotion), bid.Get(item.Emotion));
        totalText.text = $"合計 {bid.Total} 枚";
    }

    public void RefreshAsRaise(EmotionWallet wallet)
    {
        EnsureItems();
        foreach (var item in _items) item.RefreshAsRaise(wallet.Get(item.Emotion));
        totalText.text = "";
        confirmButton.gameObject.SetActive(false);
    }

    public void ResetMode()
    {
        EnsureItems();
        confirmButton.gameObject.SetActive(true);
        foreach (var item in _items)
        {
            // 競合モードで隠したマイナスを戻す
            var minus = item.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "MinusButton");
            if (minus != null) minus.gameObject.SetActive(true);
        }
    }

    private void EnsureItems()
    {
        if (_items.Count > 0) return;
        foreach (var emotion in EmotionWallet.ALL_EMOTIONS)
        {
            var item = Instantiate(itemPrefab, itemContainer).GetComponent<EmotionBidItemView>();
            item.Bind(emotion);
            item.OnPlus.Subscribe(_onPlus.OnNext).AddTo(_disposables);
            item.OnMinus.Subscribe(_onMinus.OnNext).AddTo(_disposables);
            _items.Add(item);
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
