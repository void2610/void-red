using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 対話フェーズのパネル。対象を選んでから 4 コマンドのどれかを押す
/// </summary>
public class DialoguePanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button observeButton;
    [SerializeField] private Button provokeButton;
    [SerializeField] private Button empathizeButton;
    [SerializeField] private Button persuadeButton;
    [SerializeField] private Button toBiddingButton;

    public Observable<DialogueCommand> OnCommand => Observable.Merge(
        observeButton.OnClickAsObservable().Select(_ => DialogueCommand.Observe),
        provokeButton.OnClickAsObservable().Select(_ => DialogueCommand.Provoke),
        empathizeButton.OnClickAsObservable().Select(_ => DialogueCommand.Empathize),
        persuadeButton.OnClickAsObservable().Select(_ => DialogueCommand.Persuade));

    public Observable<Unit> OnToBidding => toBiddingButton.OnClickAsObservable();
    public string ResultText => resultText.text;

    public void Hide() => gameObject.SetActive(false);

    public void Show()
    {
        gameObject.SetActive(true);
        resultText.text = "";
        SetTarget(null, null);
    }

    /// <summary>
    /// 対象に対してまだ使えるコマンドだけ押せるようにする
    /// </summary>
    public void SetTarget(AuctionParticipant target, AuctionSession session)
    {
        targetText.text = target != null ? $"対象: {target.DisplayName}" : "対象を選んでください";
        observeButton.interactable = target != null && session.CanUseDialogue(target, DialogueCommand.Observe);
        provokeButton.interactable = target != null && session.CanUseDialogue(target, DialogueCommand.Provoke);
        empathizeButton.interactable = target != null && session.CanUseDialogue(target, DialogueCommand.Empathize);
        persuadeButton.interactable = target != null && session.CanUseDialogue(target, DialogueCommand.Persuade);
    }

    public void ShowOutcome(DialogueOutcome outcome)
    {
        var head = outcome.Success ? "成功" : "失敗";
        var observed = outcome.ObservedTotal.HasValue ? $" (入札予定: {outcome.ObservedTotal.Value} 枚)" : "";
        resultText.text = $"[{head}] {outcome.Target.DisplayName}「{outcome.Line}」{observed}";
    }

    public void SetInteractable(bool on)
    {
        toBiddingButton.interactable = on;
        if (on) return;
        observeButton.interactable = false;
        provokeButton.interactable = false;
        empathizeButton.interactable = false;
        persuadeButton.interactable = false;
    }
}
