using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 競合フェーズの表示。残り時間と競合者の現在額を見せる。上乗せ操作は BidPanelView の行を流用する
/// </summary>
public class CompetitionPanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI totalsText;
    [SerializeField] private Image timerFill;

    public void Hide() => gameObject.SetActive(false);

    public void Show(CompetitionState competition)
    {
        gameObject.SetActive(true);
        titleText.text = $"競合！ {competition.Competitors.Count} 人が同額";
        Refresh(competition, 0f);
    }

    public void Refresh(CompetitionState competition, float now)
    {
        totalsText.text = string.Join("\n", competition.Competitors.Select(c => $"{c.DisplayName}: {competition.TotalOf(c)} 枚"));
        timerFill.fillAmount = competition.TimeoutSeconds <= 0f ? 0f : competition.RemainingSeconds(now) / competition.TimeoutSeconds;
    }
}
