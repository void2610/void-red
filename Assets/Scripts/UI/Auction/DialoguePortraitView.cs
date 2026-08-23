using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 会話シーンの立ち絵表示を担当するViewクラス
/// プレイヤーと敵の立ち絵をクロスフェードで切り替える
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DialoguePortraitView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image portraitImageBack;

    [SerializeField] private float crossFadeDuration = 0.3f;
    [SerializeField] private float hiddenX = 600f;
    [SerializeField] private float shownX = 300f;
    [SerializeField] private float slideDuration = 0.3f;

    private RectTransform _portraitTransform;
    private MotionHandle _slideHandle;
    private MotionHandle _fadeHandle;
    private MotionHandle _fadeBackHandle;

    /// <summary>
    /// 立ち絵をクロスフェードで切り替え
    /// </summary>
    public async UniTask ChangePortrait(Sprite newSprite)
    {
        // 現在のスプライトと同じ場合はスキップ
        if (portraitImage.sprite == newSprite) return;

        // 背面画像に新しいスプライトを設定
        portraitImageBack.sprite = newSprite;
        portraitImageBack.color = new Color(1, 1, 1, 0);

        // 進行中のアニメーションをキャンセル
        _fadeHandle.TryCancel();
        _fadeBackHandle.TryCancel();

        // クロスフェード実行
        var fadeOut = portraitImage.FadeOut(crossFadeDuration, Ease.InOutQuad);
        var fadeIn = portraitImageBack.FadeIn(crossFadeDuration, Ease.InOutQuad);

        _fadeHandle = fadeOut.AddTo(gameObject);
        _fadeBackHandle = fadeIn.AddTo(gameObject);

        await UniTask.WhenAll(
            _fadeHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy()),
            _fadeBackHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
        );

        // アニメーション完了後、前面と背面を入れ替え
        (portraitImage.sprite, portraitImageBack.sprite) = (portraitImageBack.sprite, portraitImage.sprite);
        portraitImage.color = Color.white;
        portraitImageBack.color = new Color(1, 1, 1, 0);
        ApplyVisibility(portraitImage);
        ApplyVisibility(portraitImageBack);
    }

    public void SetPortraitImmediate(Sprite sprite)
    {
        portraitImage.sprite = sprite;
        portraitImage.color = Color.white;
        portraitImageBack.color = new Color(1, 1, 1, 0);
        ApplyVisibility(portraitImage);
        ApplyVisibility(portraitImageBack);
    }

    /// <summary>
    /// 立ち絵をスライドインで表示
    /// </summary>
    public void SlideIn()
    {
        _slideHandle.TryCancel();
        _portraitTransform.anchoredPosition = new Vector2(hiddenX, _portraitTransform.anchoredPosition.y);
        _slideHandle = _portraitTransform.MoveToX(shownX, slideDuration, Ease.OutCubic);
    }

    /// <summary>
    /// 立ち絵をスライドアウトで非表示
    /// </summary>
    public void SlideOut()
    {
        _slideHandle.TryCancel();
        _portraitTransform = this.GetComponent<RectTransform>();
        _slideHandle = _portraitTransform.MoveToX(hiddenX, slideDuration, Ease.InCubic);
    }

    /// <summary>
    /// スプライトを即座に設定（アニメーションなし）
    /// </summary>
    private static void ApplyVisibility(Image image)
    {
        // 立ち絵が無い相手 (モブ) では白い矩形が出てしまうため隠す
        image.enabled = image.sprite;
    }

    private void Awake()
    {
        // 背面画像を透明に初期化
        _portraitTransform = this.GetComponent<RectTransform>();
        _portraitTransform.anchoredPosition = new Vector2(hiddenX, _portraitTransform.anchoredPosition.y);

        portraitImageBack.color = new Color(1, 1, 1, 0);
    }

    private void OnDestroy()
    {
        _slideHandle.TryCancel();
        _fadeHandle.TryCancel();
        _fadeBackHandle.TryCancel();
    }
}
