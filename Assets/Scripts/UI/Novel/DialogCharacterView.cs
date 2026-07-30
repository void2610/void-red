using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

[RequireComponent(typeof(CanvasGroup))]
public class DialogCharacterView : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private Image characterImageBack;

    private CanvasGroup _canvasGroup;

    public void SetSprite(Sprite sprite) => PlaySpriteChangeAnimation(sprite).Forget();
    public async UniTask FadeIn() => await _canvasGroup.FadeIn(0.5f);
    public async UniTask FadeOut() => await _canvasGroup.FadeOut(0.5f);

    /// <summary>
    /// キャラクター画像を設定（位置とスケールも調整）
    /// </summary>
    public void SetCharacterImage(Sprite sprite)
    {
        if (sprite == null)
        {
            characterImage.sprite = null;
            characterImageBack.sprite = null;
            return;
        }

        SetSprite(sprite);
    }

    /// <summary>
    /// 2枚の画像を使った真のクロスフェードアニメーション
    /// </summary>
    private async UniTask PlaySpriteChangeAnimation(Sprite newSprite)
    {
        // 現在のスプライトと同じ場合はアニメーションをスキップ
        if (characterImage.sprite == newSprite) return;

        // 背面画像に新しいスプライトを設定
        characterImageBack.sprite = newSprite;
        characterImageBack.color = new Color(1, 1, 1, 0);

        // 同時進行のクロスフェード
        var fadeOutFront = characterImage.FadeOut(0.2f, Ease.InOutQuad);

        var fadeInBack = characterImageBack.FadeIn(0.2f, Ease.InOutQuad);

        // 両方のアニメーションを同時実行
        await UniTask.WhenAll(
            fadeOutFront.AddTo(gameObject).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy()),
            fadeInBack.AddTo(gameObject).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
        );

        // アニメーション完了後、前面と背面を入れ替え
        (characterImage.sprite, characterImageBack.sprite) = (characterImageBack.sprite, characterImage.sprite);

        characterImage.color = Color.white;
        characterImageBack.color = new Color(1, 1, 1, 0);
    }

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }
}
