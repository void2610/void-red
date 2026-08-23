using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// オークションフェーズの View
/// 場に出ているロット (記憶) と、感情ホイール・入札ウィンドウを束ねる
/// </summary>
public class AuctionView : BasePhaseView
{
    [Header("ロット表示")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private AuctionCardView auctionCardPrefab;

    [Header("入札UI")]
    [SerializeField] private BidWindowView bidWindowView;
    [SerializeField] private Button confirmBiddingButton;

    [Header("感情リソース表示")]
    [SerializeField] private EmotionResourceDisplayView emotionResourceDisplayView;

    [Header("ロット登場アニメーション")]
    [SerializeField] private StaggeredSlideInGroup cardStagger;

    /// <summary>感情ホイールで選択中の属性が変わった</summary>
    public Observable<EmotionType> OnEmotionSelected => emotionResourceDisplayView.OnEmotionSelected;

    /// <summary>入札ウィンドウの + / -</summary>
    public Observable<Unit> OnIncrease => bidWindowView.OnIncrease;
    public Observable<Unit> OnDecrease => bidWindowView.OnDecrease;
    public Observable<Unit> OnBiddingConfirmed => confirmBiddingButton.OnClickAsObservable();

    public AuctionCardView CurrentCard => _cards.Count > 0 ? _cards[^1] : null;
    public EmotionType SelectedEmotion => emotionResourceDisplayView.SelectedEmotion;

    private readonly List<AuctionCardView> _cards = new();

    public override void Show() => CanvasGroup.Show();

    public void UpdateEmotionResources(IReadOnlyDictionary<EmotionType, int> resources) => emotionResourceDisplayView.UpdateResources(resources);

    public void SetSelectedEmotion(EmotionType emotion) => emotionResourceDisplayView.SetSelectedEmotion(emotion);

    public void SetEmotionInteractable(bool interactable) => emotionResourceDisplayView.SetInteractable(interactable);

    public void SetConfirmInteractable(bool interactable) => confirmBiddingButton.interactable = interactable;

    /// <summary>確定ボタンの文言をフェーズに合わせて変える</summary>
    public void SetConfirmLabel(string label) => confirmBiddingButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = label;

    public void SetIncreaseInteractable(bool interactable) => bidWindowView.SetIncreaseInteractable(interactable);

    public void UpdateBidAmount(int amount) => bidWindowView.UpdateBidAmount(amount);

    public void SetBidEmotion(EmotionType emotion) => bidWindowView.SetEmotion(emotion);

    public void HideBidWindow() => bidWindowView.Hide();

    /// <summary>入札ウィンドウの表示 (ロット名 / 属性 / 枚数)</summary>
    public void ShowBidWindow(MemoryLotData lot, EmotionType emotion, int amount)
    {
        bidWindowView.Show();
        bidWindowView.SetCardName(lot.Title);
        bidWindowView.SetEmotion(emotion);
        bidWindowView.UpdateBidAmount(amount);
    }

    /// <summary>新しいロットを場に出す</summary>
    public void ShowLot(MemoryLotData lot)
    {
        Clear();
        var card = Instantiate(auctionCardPrefab, cardContainer);
        card.Initialize(lot);
        card.SetInteractable(false);
        _cards.Add(card);
        cardStagger.Play();
    }

    /// <summary>入札内訳と他参加者の最高額を札の上に出す</summary>
    public void ShowBids(Dictionary<EmotionType, int> playerBids, int rivalTopBid)
    {
        var card = CurrentCard;
        if (!card) return;
        card.BidInfoView.ShowBidAmounts(rivalTopBid);
        card.BidInfoView.ShowPlayerBidsWithEmotion(playerBids);
    }

    /// <summary>落札結果を札に反映する</summary>
    public async UniTask ShowResultAsync(bool isPlayerWon, bool isDraw, bool noBids, Color rivalColor)
    {
        var card = CurrentCard;
        if (!card) return;

        if (noBids)
        {
            await card.FadeOutAsync();
            return;
        }
        if (isDraw)
        {
            card.CardView.SetGrowEffect(CardView.CardBidState.DrawBid, rivalColor);
            card.BidInfoView.ShowDraw();
            SeManager.Instance.PlaySe("SE_RESULT_CLASH", pitch: 1f);
            await UniTask.Delay(400);
            return;
        }
        card.CardView.SetGrowEffect(isPlayerWon ? CardView.CardBidState.PlayerBid : CardView.CardBidState.EnemyBid, rivalColor);
        card.BidInfoView.ShowResult(isPlayerWon);
        SeManager.Instance.PlaySe(isPlayerWon ? "SE_RESULT_WIN" : "SE_RESULT_LOSE", pitch: 1f);
        await UniTask.Delay(700);
    }

    public void Clear()
    {
        cardStagger.Cancel();
        foreach (var card in _cards) Destroy(card.gameObject);
        _cards.Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}
