using System.Collections.Generic;
using Coffee.UIEffects;
using Cysharp.Threading.Tasks;
using LitMotion;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// カードの表示と基本的なロジックを担当するViewクラス
/// 元のCard.csをベースに選択機能とアニメーション機能を追加した簡略化されたMVPパターン
/// </summary>
public class CardView : BaseCardView
{
    public enum CardBidState
    {
        None,       // 入札なし
        PlayerBid, // プレイヤーが入札
        EnemyBid,   // 敵が入札
        DrawBid    // 引き分け入札
    }

    [Header("UIコンポーネント")]
    [SerializeField] private Image cardImage;
    // カードの記憶種類によって変わるカーテンの色
    [SerializeField] private Image curtainImage;
    [SerializeField] private Image cardFrame;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private Button cardButton;
    [SerializeField] private Image growImage;
    [SerializeField] private SerializableDictionary<MemoryType, Sprite> curtainSprites;

    public MemoryLotData Lot { get; private set; }
    public Observable<CardView> OnClicked { get; private set; }

    private static readonly int _alpha = Shader.PropertyToID("_Alpha");
    private static readonly int _value = Shader.PropertyToID("_Value");
    private static readonly int _color2 = Shader.PropertyToID("_Color2");

    private RectTransform _rectTransform;
    // Tween管理用
    private MotionHandle _backTransitionTween;
    private MotionHandle _edgeColorTween;
    private Material _instancedGrowMaterial;
    private MotionHandle _growAlphaHandle;

    public void SetInteractable(bool interactable) => cardButton.interactable = interactable;

    /// <summary>
    /// 流札の札をグレーアウトする
    /// </summary>
    public void SetDimmed() => ApplyDimmedDisplay();

    /// <summary>
    /// カードデータを設定して初期化
    /// </summary>
    public void Initialize(MemoryLotData lot)
    {
        Lot = lot;
        UpdateCardDisplay();
    }

    /// <summary>
    /// ハイライト表示
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        _rectTransform.ScaleTo(highlight ? Vector3.one * 1.1f : Vector3.one, 0.1f);
        if (highlight) SeManager.Instance.PlaySe("CardSelect");
    }

    /// <summary>
    /// フェードアウトアニメーション（入札なしカード用）
    /// </summary>
    public async UniTask PlayFadeOutAsync(float duration = 0.3f)
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        await LMotion.Create(1f, 0f, duration)
            .WithEase(Ease.OutCubic)
            .Bind(alpha => canvasGroup.alpha = alpha)
            .ToUniTask();
    }

    public void SetGrowEffect(CardBidState state, Color enemyColor, float fadeDuration = 0.3f)
    {
        _growAlphaHandle.TryCancel();

        switch (state)
        {
            case CardBidState.PlayerBid:
                _instancedGrowMaterial.SetFloat(_value, 1.5f);
                break;
            case CardBidState.EnemyBid:
                _instancedGrowMaterial.SetColor(_color2, enemyColor * CalculateHdrIntensity(enemyColor));
                _instancedGrowMaterial.SetFloat(_value, 0f);
                break;
            case CardBidState.DrawBid:
                _instancedGrowMaterial.SetColor(_color2, enemyColor * CalculateHdrIntensity(enemyColor));
                _instancedGrowMaterial.SetFloat(_value, 0.5f);
                break;
        }

        var targetAlpha = state != CardBidState.None ? 0.6f : 0f;
        _growAlphaHandle = _instancedGrowMaterial.MaterialFloatTo(_alpha, targetAlpha, fadeDuration, Ease.OutCubic, gameObject);
    }

    protected override MemoryLotData GetLot()
    {
        return Lot;
    }
    protected override Sprite GetCurtainSprite()
    {
        return curtainSprites.GetValueOrDefault(Lot.VisualStyle);
    }

    /// <summary>
    /// 知覚的輝度に基づいてHDR色強度を計算する
    /// 高輝度色（黄色など）は低い乗数、低輝度色（青など）は高い乗数を返す
    /// </summary>
    private static float CalculateHdrIntensity(Color color)
    {
        var luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        return Mathf.Lerp(4.0f, 1.5f, Mathf.Clamp01(luminance));
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _instancedGrowMaterial = Instantiate(growImage.material);
        growImage.material = _instancedGrowMaterial;

        // R3のOnClickAsObservableでボタンクリックを購読し、CardView自身を発行
        OnClicked = cardButton.OnClickAsObservable().Select(_ => this);
    }

    private void OnDestroy()
    {
        // Tweenのクリーンアップ
        _edgeColorTween.TryCancel();
        _backTransitionTween.TryCancel();
        _growAlphaHandle.TryCancel();
    }

    // BaseCardView 抽象プロパティの実装
    protected override Image CardImage => cardImage;
    protected override Image CurtainImage => curtainImage;
    protected override TextMeshProUGUI CardNameText => cardNameText;
    protected override Image CardFrame => cardFrame;
}
