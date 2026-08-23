using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 敵の表示を担当するViewクラス
/// カード属性に応じてSpriteを切り替える
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class EnemyView : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private Image enemyImage;
    [SerializeField] private Image enemyImageBack;

    [Header("アニメーション設定")]
    [SerializeField] private Color imageColor = Color.white;
    [SerializeField] private float crossFadeDuration = 0.4f;

    private ParticipantData _participant;

    /// <summary>立ち絵をクロスフェードで差し替える</summary>
    public UniTask ChangePortraitAsync(Sprite sprite) => PlaySpriteChangeAnimation(sprite);

    public void Initialize(ParticipantData participant)
    {
        _participant = participant;
        enemyImage.sprite = participant.Portrait;
    }

    /// <summary>
    /// 2枚の画像を使った真のクロスフェードアニメーション
    /// </summary>
    private async UniTask PlaySpriteChangeAnimation(Sprite newSprite)
    {

        // 現在のスプライトと同じ場合はアニメーションをスキップ
        if (enemyImage.sprite == newSprite) return;

        // 背面画像に新しいスプライトを設定
        enemyImageBack.sprite = newSprite;
        enemyImageBack.color = new Color(1, 1, 1, 0);

        // 同時進行のクロスフェード
        var fadeOutFront = enemyImage.FadeOut(crossFadeDuration, Ease.InOutQuad);

        var fadeInBack = enemyImageBack.FadeIn(crossFadeDuration, Ease.InOutQuad);

        // 両方のアニメーションを同時実行
        await UniTask.WhenAll(
            fadeOutFront.AddTo(gameObject).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy()),
            fadeInBack.AddTo(gameObject).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
        );

        // アニメーション完了後、前面と背面を入れ替え
        (enemyImage.sprite, enemyImageBack.sprite) = (enemyImageBack.sprite, enemyImage.sprite);

        enemyImage.color = imageColor;
        enemyImageBack.color = new Color(1, 1, 1, 0);
    }

    private void Awake()
    {
        enemyImage.color = imageColor;
        enemyImageBack.color = new Color(1, 1, 1, 0);
    }
}
