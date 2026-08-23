using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Void2610.UnityTemplate;

// カード上の入札情報表示View
// 入札額、WIN/LOSEを表示
public class CardBidInfoView : MonoBehaviour
{
    [Header("入札額表示")]
    [SerializeField] private BidAmountIconView bidAmountIconPrefab;
    [SerializeField] private Transform playerBidContainer;
    [SerializeField] private SerializableDictionary<EmotionType, Sprite> emotionSprites = new();
    [SerializeField] private TextMeshProUGUI enemyBidText;

    [Header("結果表示")]
    [SerializeField] private TextMeshProUGUI resultText;

    private static readonly Color _playerColor = Color.green;
    private static readonly Color _enemyColor = Color.red;

    private readonly List<BidAmountIconView> _spawnedIcons = new();

    // 結果を非表示
    public void HideResult()
    {
        resultText.gameObject.SetActive(false);
        ClearPlayerBidIcons();
        enemyBidText.text = "";
    }

    // 他参加者の最高入札額を表示
    public void ShowBidAmounts(int enemyBid)
    {
        ClearPlayerBidIcons();
        enemyBidText.text = $"{enemyBid}";
    }

    // 入札対象公開用（自分の入札額は数値、相手は?）
    public void ShowBidTargetReveal(bool enemyHasBid)
    {
        ClearPlayerBidIcons();
        enemyBidText.text = enemyHasBid ? "?" : "";
    }

    // 感情別入札をアイコンで表示
    public void ShowPlayerBidsWithEmotion(Dictionary<EmotionType, int> bids)
    {
        ClearPlayerBidIcons();

        var activeBids = bids
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value);

        foreach (var (emotion, amount) in activeBids)
        {
            if (!emotionSprites.TryGetValue(emotion, out var sprite)) continue;
            var icon = Instantiate(bidAmountIconPrefab, playerBidContainer);
            icon.Setup(sprite, amount);
            _spawnedIcons.Add(icon);
        }
    }

    // 結果を表示（WIN/LOSE）
    public void ShowResult(bool isPlayerWon)
    {
        resultText.gameObject.SetActive(true);
        resultText.text = isPlayerWon ? "WIN" : "LOSE";
        resultText.color = isPlayerWon ? _playerColor : _enemyColor;
    }

    // 引き分け結果を表示
    public void ShowDraw()
    {
        resultText.gameObject.SetActive(true);
        resultText.text = "DRAW";
        resultText.color = Color.yellow;
    }

    private void ClearPlayerBidIcons()
    {
        foreach (var icon in _spawnedIcons)
            Destroy(icon.gameObject);
        _spawnedIcons.Clear();
    }
}
