using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 対話フェーズのカットイン演出を担当するViewクラス
/// キャラ画像・カットイン画像・セリフが右から左にスライドする
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DialogueCutInView : MonoBehaviour
{
    [SerializeField] private Image standingImage;
    [SerializeField] private Image cutInImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;

    [Header("プレイヤーカットイン画像")]
    [SerializeField] private Sprite playerStandingSprite;
    [SerializeField] private Sprite playerCutInSprite;

    [Header("アニメーション設定")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float exitDuration = 0.3f;
    [SerializeField] private float slideOffset = 1200f;
    [SerializeField] private float intervalDuration = 0.3f;
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private float _initialX;
    private MotionHandle _slideHandle;
    private MotionHandle _fadeHandle;
    private MotionHandle _bgFadeHandle;

    /// <summary>
    /// プレイヤーのカットイン演出を再生する
    /// </summary>
    public UniTask PlayPlayerCutInAsync(string text) => PlayCutInAsync(playerStandingSprite, playerCutInSprite, text);

    /// <summary>
    /// カットイン演出を再生する
    /// </summary>
    public async UniTask PlayCutInAsync(Sprite standing, Sprite cutIn, string text)
    {
        _slideHandle.TryCancel();
        _fadeHandle.TryCancel();
        _bgFadeHandle.TryCancel();

        standingImage.sprite = standing;
        cutInImage.sprite = cutIn;
        // 素材が無い相手では白い矩形になるため隠す
        standingImage.enabled = standing;
        cutInImage.enabled = cutIn;
        dialogueText.text = $"「{text}」";

        // フェードイン + 右画面外からスライドイン
        _canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = new Vector2(_initialX + slideOffset, rectTransform.anchoredPosition.y);
        _fadeHandle = LMotion.Create(0f, 1f, fadeDuration)
            .WithEase(Ease.OutCubic)
            .BindToAlpha(_canvasGroup)
            .AddTo(gameObject);
        _bgFadeHandle = backgroundCanvasGroup.FadeIn(fadeDuration, Ease.OutCubic);
        _slideHandle = rectTransform.MoveToX(_initialX, slideDuration, Ease.OutCubic);
        await _slideHandle.ToUniTask();

        await UniTask.Delay((int)(displayDuration * 1000));

        // フェードアウト
        _fadeHandle = _canvasGroup.FadeOut(exitDuration, Ease.InCubic);
        _bgFadeHandle = backgroundCanvasGroup.FadeOut(exitDuration, Ease.InCubic);
        await _fadeHandle.ToUniTask();

        rectTransform.anchoredPosition = new Vector2(_initialX, rectTransform.anchoredPosition.y);

        // 次のカットインとの間隔
        await UniTask.Delay((int)(intervalDuration * 1000));
    }

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _initialX = rectTransform.anchoredPosition.x;

        // 背景の初期状態を透明にする
        backgroundCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        _slideHandle.TryCancel();
        _fadeHandle.TryCancel();
        _bgFadeHandle.TryCancel();
    }
}
