using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// ロビーの人格画面。統合した記憶と持ち越しリソースを見せる。感情状態そのものは表示しない
/// </summary>
public class PersonaView : BaseWindowView
{
    [SerializeField] private TextMeshProUGUI integratedText;
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI collapsedText;

    public void Show(AllFloorData floors, PersonaState persona, EmotionWallet wallet, System.Collections.Generic.IReadOnlyCollection<string> collapsedIds)
    {
        var integrated = persona.IntegratedLotIds.Select(id => floors.FindLot(id)).Where(l => l != null).Select(l => $"『{l.Title}』").ToList();
        integratedText.text = integrated.Count == 0 ? "統合した記憶: まだ無い" : $"統合した記憶:\n{string.Join("\n", integrated)}";
        walletText.text = "持ち越しリソース: " + string.Join("  ", EmotionWallet.ALL_EMOTIONS.Select(e => $"{e.ToJapaneseName()}{wallet.Get(e)}"));
        var collapsed = collapsedIds.Select(id => floors.FindParticipant(id)).Where(p => p != null).Select(p => p.DisplayName).ToList();
        collapsedText.text = collapsed.Count == 0 ? "人格崩壊した参加者: いない" : $"人格崩壊した参加者: {string.Join(", ", collapsed)}";
        Show();
    }
}
