using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Void2610.UnityTemplate;

/// <summary>
/// 入札フェーズ: 感情ホイールで属性を選び、入札ウィンドウで枚数を積む
/// </summary>
public class BiddingPhase : IAuctionPhase
{
    private readonly EmotionBid _draft = new();
    private EmotionType _selected = EmotionType.Joy;

    public bool CanRun(AuctionSession session) => session.Phase == AuctionPhase.Bidding;

    private void UpdateIncreaseInteractable(AuctionContext context)
    {
        context.View.Auction.SetIncreaseInteractable(_draft.Get(_selected) < context.Session.Player.Wallet.Get(_selected));
    }

    private Dictionary<EmotionType, int> Remaining(AuctionSession session)
    {
        return EmotionWallet.ALL_EMOTIONS.ToDictionary(e => e, e => session.Player.Wallet.Get(e) - _draft.Get(e));
    }

    public async UniTask RunAsync(AuctionContext context, CancellationToken ct)
    {
        var view = context.View;
        var session = context.Session;
        _draft.Clear();
        _selected = view.Auction.SelectedEmotion;

        view.Auction.ShowBidWindow(session.CurrentLot, _selected, 0);
        view.Auction.UpdateEmotionResources(Remaining(session));
        view.Auction.SetEmotionInteractable(true);
        view.Auction.SetConfirmInteractable(true);
        UpdateIncreaseInteractable(context);

        using var d = new CompositeDisposable();
        view.Auction.OnEmotionSelected.Subscribe(e =>
        {
            _selected = e;
            view.Auction.SetBidEmotion(e);
            view.Auction.UpdateBidAmount(_draft.Get(e));
            UpdateIncreaseInteractable(context);
        }).AddTo(d);

        view.Auction.OnIncrease.Subscribe(_ =>
        {
            if (_draft.Get(_selected) >= session.Player.Wallet.Get(_selected)) return;
            _draft.Add(_selected);
            SeManager.Instance.PlaySe(_selected.ToResourceSeName(), pitch: 1f);
            AfterChanged(context);
        }).AddTo(d);

        view.Auction.OnDecrease.Subscribe(_ =>
        {
            if (_draft.Get(_selected) <= 0) return;
            _draft.Add(_selected, -1);
            AfterChanged(context);
        }).AddTo(d);

        await view.Auction.OnBiddingConfirmed.FirstAsync(ct);
        view.Auction.HideBidWindow();
        view.Auction.SetEmotionInteractable(false);
        session.SubmitPlayerBid(_draft);
    }

    private void AfterChanged(AuctionContext context)
    {
        context.View.Auction.UpdateBidAmount(_draft.Get(_selected));
        context.View.Auction.UpdateEmotionResources(Remaining(context.Session));
        UpdateIncreaseInteractable(context);
    }
}
