using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Void2610.UnityTemplate;

/// <summary>
/// カードの表示と基本的なロジックを担当するViewクラス
/// 元のCard.csをベースに選択機能とアニメーション機能を追加した簡略化されたMVPパターン
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI forgivenessText;
    [SerializeField] private TextMeshProUGUI rejectionText;
    [SerializeField] private TextMeshProUGUI blankText;
    [SerializeField] private Button cardButton;
    
    public CardData CardData { get; private set; }
    public Observable<CardView> OnClicked => _onClicked;
    
    private readonly Subject<CardView> _onClicked = new();
    private Vector2 _originalPosition;
    private RectTransform _rectTransform;
    
    public void SetInteractable(bool interactable) => cardButton.interactable = interactable;
    public void UpdateOriginalPosition(Vector2 position) => _originalPosition = position;
    
    /// <summary>
    /// カードデータを設定して初期化
    /// </summary>
    public void Initialize(CardData cardData)
    {
        CardData = cardData;
        _originalPosition = _rectTransform.anchoredPosition;
        UpdateDisplay();
        cardButton.onClick.AddListener(OnCardClicked);
    }
    
    /// <summary>
    /// デッキからドローされるアニメーション
    /// </summary>
    public async UniTask PlayDrawAnimation(Vector2 startPosition)
    {
        // 開始位置を設定
        _rectTransform.anchoredPosition = startPosition;
        _rectTransform.localScale = Vector3.one * 0.1f;
        
        // 移動とスケールのアニメーション
        var moveTask = LMotion.Create(startPosition, _originalPosition, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
            
        var scaleTask = LMotion.Create(Vector3.one * 0.1f, Vector3.one, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToLocalScale(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
        
        await UniTask.WhenAll(moveTask, scaleTask);
    }
    
    /// <summary>
    /// 削除アニメーション
    /// </summary>
    public async UniTask PlayRemoveAnimation(bool isCollapse = false)
    {
        if (isCollapse)
        {
            // 崩壊アニメーション：ランダムな方向に飛び散りながら回転
            var randomDirection = new Vector2(
                UnityEngine.Random.Range(-200f, 200f),
                UnityEngine.Random.Range(100f, 300f)
            );
            var currentPos = _rectTransform.anchoredPosition;
            var targetPos = currentPos + randomDirection;
            
            var moveTask = LMotion.Create(currentPos, targetPos, 0.5f)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(_rectTransform)
                .AddTo(gameObject)
                .ToUniTask();
                
            var startRotation = _rectTransform.rotation;
            var targetRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-360f, 360f));
            var rotateTask = LMotion.Create(startRotation, targetRotation, 0.5f)
                .WithEase(Ease.OutCubic)
                .BindToRotation(_rectTransform)
                .AddTo(gameObject)
                .ToUniTask();
                
            var scaleTask = LMotion.Create(Vector3.one, Vector3.zero, 0.5f)
                .WithEase(Ease.InCubic)
                .BindToLocalScale(_rectTransform)
                .AddTo(gameObject)
                .ToUniTask();
                
            await UniTask.WhenAll(moveTask, rotateTask, scaleTask);
        }
        else
        {
            // 通常の削除アニメーション：上に移動しながらスケール縮小
            var currentPos = _rectTransform.anchoredPosition;
            var targetPos = new Vector2(currentPos.x, currentPos.y + 100f);
            
            var moveTask = LMotion.Create(currentPos, targetPos, 0.3f)
                .WithEase(Ease.InCubic)
                .BindToAnchoredPosition(_rectTransform)
                .AddTo(gameObject)
                .ToUniTask();
                
            var scaleTask = LMotion.Create(Vector3.one, Vector3.one * 0.5f, 0.3f)
                .WithEase(Ease.InCubic)
                .BindToLocalScale(_rectTransform)
                .AddTo(gameObject)
                .ToUniTask();
                
            await UniTask.WhenAll(moveTask, scaleTask);
        }
    }
    
    /// <summary>
    /// 配置アニメーション
    /// </summary>
    public async UniTask PlayArrangeAnimation(Vector2 targetPosition, Quaternion targetRotation)
    {
        _originalPosition = targetPosition;
        
        // 位置のアニメーション
        var moveTask = LMotion.Create(_rectTransform.anchoredPosition, targetPosition, 0.3f)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
        
        // 回転のアニメーション
        var rotateTask = LMotion.Create(_rectTransform.rotation, targetRotation, 0.3f)
            .WithEase(Ease.OutCubic)
            .BindToRotation(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
        
        await UniTask.WhenAll(moveTask, rotateTask);
    }
    
    /// <summary>
    /// デッキ戻りアニメーション
    /// </summary>
    public async UniTask PlayReturnToDeckAnimation(Vector2 deckPosition)
    {
        var currentPos = _rectTransform.anchoredPosition;
        var currentScale = _rectTransform.localScale;
        var currentRotation = _rectTransform.rotation;
        
        // 移動、スケール、回転のアニメーション
        var moveTask = LMotion.Create(currentPos, deckPosition, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
            
        var scaleTask = LMotion.Create(currentScale, Vector3.one * 0.1f, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToLocalScale(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
            
        var rotateTask = LMotion.Create(currentRotation, Quaternion.identity, 0.4f)
            .WithEase(Ease.InCubic)
            .BindToRotation(_rectTransform)
            .AddTo(gameObject)
            .ToUniTask();
        
        await UniTask.WhenAll(moveTask, scaleTask, rotateTask);
    }
    
    /// <summary>
    /// ハイライト表示
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        // 選択時は少し上に移動、非選択時は元の位置に戻る
        var currentPos = _rectTransform.anchoredPosition;
        var targetPos = new Vector2(currentPos.x, highlight ? _originalPosition.y + 30f : _originalPosition.y);
        
        // 位置のアニメーション
        LMotion.Create(currentPos, targetPos, 0.2f)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject);
        
        if (highlight) SeManager.Instance.PlaySe("Card");
    }
    
    /// <summary>
    /// 表示を更新
    /// </summary>
    private void UpdateDisplay()
    {
        if (!CardData) return;
        
        // カード名を設定
        cardNameText.text = CardData.CardName;
        
        // カード画像を設定
        cardImage.sprite = CardData.CardImage;
        
        // 効果パラメータを設定
        var effect = CardData.Effect;
        forgivenessText.text = $"許し: {effect.Forgiveness:F1}";
        rejectionText.text = $"拒絶: {effect.Rejection:F1}";
        blankText.text = $"空白: {effect.Blank:F1}";
    }
    
    /// <summary>
    /// カードがクリックされた時の処理
    /// </summary>
    private void OnCardClicked()
    {
        _onClicked.OnNext(this);
    }
    
    private void Awake()
    {
        _rectTransform = this.GetComponent<RectTransform>();
    }
    
    private void OnDestroy()
    {
        _onClicked?.Dispose();
        if (cardButton)
            cardButton.onClick.RemoveListener(OnCardClicked);
    }
}