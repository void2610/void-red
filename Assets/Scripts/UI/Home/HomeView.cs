using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// ホーム画面のView
/// UI要素の参照とイベントの公開を担当
/// </summary>
public class HomeView : MonoBehaviour
{
    [SerializeField] private Button titleButton;
    [SerializeField] private Button deckButton;
    [SerializeField] private Button libraryButton;
    [SerializeField] private Button storyButton;
    [SerializeField] private Button personButton;
    [SerializeField] private Button dreamButton;
    [SerializeField] private TextMeshProUGUI speakingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private MemoryCollectionView collectionView;
    [SerializeField] private PersonaView personaView;

    // ボタンクリックイベントをObservableとして公開
    public Observable<Unit> TitleButtonClicked => titleButton.OnClickAsObservable();
    public Observable<Unit> StoryButtonClicked => storyButton.OnClickAsObservable();
    public Observable<Unit> PersonaButtonClicked => deckButton.OnClickAsObservable();
    public Observable<Unit> CollectionButtonClicked => libraryButton.OnClickAsObservable();
    public MemoryCollectionView CollectionView => collectionView;
    public PersonaView PersonaView => personaView;

    public void SetProgressText(string text) => progressText.text = text;

    /// <summary>
    /// Personボタンのinteractable設定
    /// </summary>
    public void SetPersonButtonInteractable(bool interactable) => personButton.interactable = interactable;

    /// <summary>
    /// Dreamボタンのinteractable設定
    /// </summary>
    public void SetDreamButtonInteractable(bool interactable) => dreamButton.interactable = interactable;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        // 未実装のボタンを無効化
        personButton.interactable = false;
        dreamButton.interactable = false;

        InitSpeaking().Forget();
    }

    /// <summary>
    /// セリフテキストの初期化
    /// </summary>
    private async UniTask InitSpeaking()
    {
        await UniTask.Delay(1000);
        speakingText.TypewriterAnimation("...").Forget();
    }
}
