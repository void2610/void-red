using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主人公が 1 つも落札できなかったときの表示。その階層の最初からやり直す
/// </summary>
public class GameOverView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    public Observable<Unit> OnRetry => retryButton.OnClickAsObservable();
    public Observable<Unit> OnLobby => lobbyButton.OnClickAsObservable();

    public void Hide() => gameObject.SetActive(false);

    public void Show(int floorIndex)
    {
        gameObject.SetActive(true);
        messageText.text = $"記憶を 1 つも落札できなかった。\n第 {floorIndex} 階層の洗礼を受ける権利はない。";
    }
}
