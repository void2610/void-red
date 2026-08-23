using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 洗礼で落札した記憶を並べ、1 つを選ばせるサブ View
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardAcquisitionView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button nextButton;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private AcquiredCardView cardPrefab;
    [SerializeField] private Transform textContainer;
    [SerializeField] private AcquiredCardTextView cardTextPrefab;
    [SerializeField] private StaggeredSlideInGroup cardStagger;
    [SerializeField] private StaggeredSlideInGroup textStagger;

    [Header("アニメーション設定")]
    [SerializeField] private float initialDelay = 0.3f;

    /// <summary>
    /// 獲得カードを一括表示し、nextボタンで進行
    /// </summary>
    /// <summary>選ばれた記憶</summary>
    public Observable<WonLot> OnLotSelected => _onLotSelected;

    public Observable<Unit> OnNext => _onNextButtonClicked;

    private readonly Subject<Unit> _onNextButtonClicked = new();
    private readonly Subject<WonLot> _onLotSelected = new();
    private readonly List<GameObject> _instantiatedItems = new();
    private readonly List<AcquiredCardView> _cards = new();

    public async UniTask DisplayLotsAsync(IEnumerable<WonLot> lots)
    {
        Show();
        ClearInstantiatedItems();

        foreach (var wonLot in lots)
        {
            // 札を cardContainer に生成（アニメーション前は非表示）
            var acquiredCard = Instantiate(cardPrefab, cardContainer);
            acquiredCard.Initialize(wonLot);
            acquiredCard.SetSelectable(true);
            acquiredCard.CardView.OnClicked.Subscribe(_ => Select(acquiredCard)).AddTo(this);
            acquiredCard.gameObject.GetOrAddComponent<CanvasGroup>().alpha = 0f;
            _instantiatedItems.Add(acquiredCard.gameObject);
            _cards.Add(acquiredCard);

            // 内訳を textContainer に生成（アニメーション前は非表示）
            var textItem = Instantiate(cardTextPrefab, textContainer);
            textItem.Initialize(wonLot);
            textItem.gameObject.GetOrAddComponent<CanvasGroup>().alpha = 0f;
            _instantiatedItems.Add(textItem.gameObject);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(initialDelay));
        SeManager.Instance.PlaySe("SE_REWARD_CARD", pitch: 1f);
        cardStagger.Play();
        SeManager.Instance.PlaySe("SE_RESOURCE_TEXT", pitch: 1f);
        textStagger.Play();
    }

    public void Hide()
    {
        cardStagger.Cancel();
        textStagger.Cancel();
        canvasGroup.Hide();
        _cards.Clear();
        ClearInstantiatedItems();
    }

    private void Select(AcquiredCardView card)
    {
        foreach (var c in _cards) c.SetSelected(c == card);
        _onLotSelected.OnNext(card.WonLot);
    }

    private void Show()
    {
        canvasGroup.Show();
    }

    private void ClearInstantiatedItems()
    {
        foreach (var item in _instantiatedItems)
            Destroy(item);
        _instantiatedItems.Clear();
    }

    private void Awake()
    {
        canvasGroup.Hide();

        nextButton.OnClickAsObservable()
            .Subscribe(_ => _onNextButtonClicked.OnNext(Unit.Default))
            .AddTo(this);
    }

    private void OnDestroy()
    {
        _onNextButtonClicked.Dispose();
    }
}
