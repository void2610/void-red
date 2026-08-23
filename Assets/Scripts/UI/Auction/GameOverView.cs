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

    /// <summary>選ばれた行き先 (未選択なら null)。押下の取りこぼしを避けるためフラグで持つ</summary>
    public bool? RetryRequested { get; private set; }
    private readonly CompositeDisposable _disposables = new();

    public void Hide() => gameObject.SetActive(false);

    public void Show(int floorIndex, bool missedKey)
    {
        gameObject.SetActive(true);
        RetryRequested = null;
        messageText.text = missedKey
            ? $"楽園への鍵を取り逃した。\n第 {floorIndex} 階層の洗礼を受ける権利はない。"
            : $"記憶を 1 つも落札できなかった。\n第 {floorIndex} 階層の洗礼を受ける権利はない。";
    }

    private void Awake()
    {
        retryButton.OnClickAsObservable().Subscribe(_ => RetryRequested = true).AddTo(_disposables);
        lobbyButton.OnClickAsObservable().Subscribe(_ => RetryRequested = false).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
