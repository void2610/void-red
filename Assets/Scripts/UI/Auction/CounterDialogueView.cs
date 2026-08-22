using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// キャラ側から仕掛けてくる二択の問いかけ
/// </summary>
public class CounterDialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button choiceAButton;
    [SerializeField] private Button choiceBButton;
    [SerializeField] private TextMeshProUGUI choiceAText;
    [SerializeField] private TextMeshProUGUI choiceBText;

    public Observable<int> OnChoice => Observable.Merge(
        choiceAButton.OnClickAsObservable().Select(_ => 0),
        choiceBButton.OnClickAsObservable().Select(_ => 1));

    public void Hide() => gameObject.SetActive(false);

    public void Show(AuctionParticipant speaker, CounterDialogue counter)
    {
        gameObject.SetActive(true);
        speakerText.text = speaker.DisplayName;
        promptText.text = counter.Prompt;
        choiceAText.text = counter.ChoiceA;
        choiceBText.text = counter.ChoiceB;
    }
}
