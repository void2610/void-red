using UnityEngine;
using UnityEngine.UI;

/// <summary>3本先取の勝敗状況を表示するダイヤモンドインジケータ。プレイヤー勝利は右端から、敵勝利は左端から画像が切り替わる。</summary>
public class DiamondIndicatorView : MonoBehaviour
{
    [Header("ダイヤモンド画像（左→右の順で割り当て）")]
    [SerializeField] private Image[] diamondImages;

    [Header("スプライト設定")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite undecidedSprite;

    [Header("色設定")]
    [SerializeField] private Color undecidedColor = new(0.4f, 0.4f, 0.4f, 1f);

    /// <summary>勝敗カウントに応じて画像を更新する</summary>
    public void UpdateIndicators(int playerWins, int enemyWins)
    {
        var length = diamondImages.Length;
        for (var i = 0; i < length; i++)
        {
            // プレイヤーは右端 (length-1) から左に向かって埋める
            var fromRight = length - 1 - i;
            Sprite targetSprite;
            Color targetColor;
            if (fromRight < playerWins)
            {
                targetSprite = playerSprite;
                targetColor = Color.white;
            }
            else if (i < enemyWins)
            {
                targetSprite = enemySprite;
                targetColor = Color.white;
            }
            else
            {
                targetSprite = undecidedSprite;
                targetColor = undecidedColor;
            }

            if (diamondImages[i].sprite != targetSprite)
                diamondImages[i].sprite = targetSprite;
            if (diamondImages[i].color != targetColor)
                diamondImages[i].color = targetColor;
        }
    }
}
