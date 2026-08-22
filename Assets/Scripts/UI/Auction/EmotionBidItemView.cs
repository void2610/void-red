using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 感情属性 1 行分の入札操作 (所持枚数 / 入札枚数 / 増減ボタン)
/// 行の上でマウスホイールを回しても増減できる
/// </summary>
public class EmotionBidItemView : MonoBehaviour, IScrollHandler
{
    [SerializeField] private Image colorBar;
    [SerializeField] private TextMeshProUGUI emotionNameText;
    [SerializeField] private TextMeshProUGUI ownedText;
    [SerializeField] private TextMeshProUGUI bidCountText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    public EmotionType Emotion { get; private set; }
    public Observable<EmotionType> OnPlus => plusButton.OnClickAsObservable().Select(_ => Emotion);
    public Observable<EmotionType> OnMinus => minusButton.OnClickAsObservable().Select(_ => Emotion);

    public void Bind(EmotionType emotion)
    {
        Emotion = emotion;
        name = $"Bid_{emotion}";
        emotionNameText.text = emotion.ToJapaneseName();
        colorBar.color = emotion.GetColor();
    }

    public void Refresh(int owned, int bid)
    {
        ownedText.text = owned.ToString();
        bidCountText.text = bid.ToString();
        plusButton.interactable = bid < owned;
        minusButton.interactable = bid > 0;
    }

    /// <summary>
    /// 競合の上乗せ用。マイナスは使わず、プラスだけを所持がある間有効にする
    /// </summary>
    public void RefreshAsRaise(int owned)
    {
        ownedText.text = owned.ToString();
        bidCountText.text = "";
        plusButton.interactable = owned > 0;
        minusButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// ホイール上で +、下で -。ボタンの onClick を経由して有効判定も共有する
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        var button = eventData.scrollDelta.y > 0 ? plusButton : minusButton;
        if (button.interactable && button.gameObject.activeInHierarchy) button.onClick.Invoke();
    }
}
