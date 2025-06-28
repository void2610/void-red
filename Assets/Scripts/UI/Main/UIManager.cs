using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;
using LitMotion;
using LitMotion.Extensions;
using R3;
using System.Threading;

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [SerializeField] private TextMeshProUGUI themeText;
    [SerializeField] private Image announcementBackground;
    [SerializeField] private TextMeshProUGUI announcementText;
    
    [Header("プレイボタン")]
    [SerializeField] private Button playButton;
    
    [Header("カードの出し方選択")]
    [SerializeField] private Button hesitationButton;
    [SerializeField] private Button impulseButton;
    [SerializeField] private Button convictionButton;
    [SerializeField] private TextMeshProUGUI playStyleSelectedText;
    
    [Header("精神ベット")]
    [SerializeField] private Button mentalBetPlusButton;
    [SerializeField] private Button mentalBetMinusButton;
    [SerializeField] private TextMeshProUGUI mentalBetValueText;

    private const float FADE_IN_DURATION = 0.3f;
    private const float FADE_OUT_DURATION = 0.3f;
    private const float SLIDE_DISTANCE = 350f;
    
    // プレイボタンが押されたことを通知するSubject
    private readonly Subject<Unit> _playButtonClicked = new();
    public Observable<Unit> PlayButtonClicked => _playButtonClicked;
    
    // 選択されたプレイスタイルと精神ベット
    private PlayStyle _selectedPlayStyle = PlayStyle.Hesitation;
    private int _mentalBetValue;
    
    private const int MIN_MENTAL_BET = 0;
    private const int MAX_MENTAL_BET = 7;

    public void SetTheme(CardStatus theme)
    {
        var text = "";
        text += $"赦し: {theme.Forgiveness:F2}\n";
        text += $"拒絶: {theme.Rejection:F2}\n";
        text += $"空白: {theme.Blank:F2}\n";
        themeText.text = text;
    }
    
    public async UniTask ShowAnnouncement(string message, float duration = 2f)
    {
        // アプリケーション終了時にもキャンセルされるようにする  
        var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy(), 
            Application.exitCancellationToken
        ).Token;
        
        try
        {
            // メッセージを設定
            announcementText.text = message;
            
            // 初期状態を設定
            announcementBackground.gameObject.SetActive(true);
            announcementText.gameObject.SetActive(true);
            announcementBackground.color = new Color(announcementBackground.color.r, announcementBackground.color.g, announcementBackground.color.b, 0f);
            announcementText.color = new Color(announcementText.color.r, announcementText.color.g, announcementText.color.b, 0f);
            
            // テキストを左側に配置
            var textRect = announcementText.rectTransform;
            var originalPosition = textRect.anchoredPosition;
            textRect.anchoredPosition = new Vector2(originalPosition.x - SLIDE_DISTANCE, originalPosition.y);
            
            // フェードインアニメーション
            var fadeInTasks = new UniTask[3];
            
            // 背景のフェードイン
            var bgColor = announcementBackground.color;
            fadeInTasks[0] = LMotion.Create(new Color(bgColor.r, bgColor.g, bgColor.b, 0f), new Color(bgColor.r, bgColor.g, bgColor.b, 0.95f), FADE_IN_DURATION)
                .WithEase(Ease.OutQuart)
                .BindToColor(announcementBackground)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            // テキストのフェードイン
            var textColor = announcementText.color;
            fadeInTasks[1] = LMotion.Create(new Color(textColor.r, textColor.g, textColor.b, 0f), new Color(textColor.r, textColor.g, textColor.b, 1f), FADE_IN_DURATION)
                .WithEase(Ease.OutQuart)
                .BindToColor(announcementText)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            // テキストのスライドインアニメーション（左から中央へ）
            fadeInTasks[2] = LMotion.Create(new Vector2(originalPosition.x - SLIDE_DISTANCE, originalPosition.y), originalPosition, FADE_IN_DURATION)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(textRect)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            await UniTask.WhenAll(fadeInTasks);
            
            // 表示時間を待つ
            await UniTask.Delay((int)(duration * 1000), cancellationToken: cancellationToken);
            
            // フェードアウトアニメーション
            var fadeOutTasks = new UniTask[3];
            
            // 背景のフェードアウト
            var bgColorOut = announcementBackground.color;
            fadeOutTasks[0] = LMotion.Create(new Color(bgColorOut.r, bgColorOut.g, bgColorOut.b, 1f), new Color(bgColorOut.r, bgColorOut.g, bgColorOut.b, 0f), FADE_OUT_DURATION)
                .WithEase(Ease.InQuart)
                .BindToColor(announcementBackground)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            // テキストのフェードアウト
            var textColorOut = announcementText.color;
            fadeOutTasks[1] = LMotion.Create(new Color(textColorOut.r, textColorOut.g, textColorOut.b, 1f), new Color(textColorOut.r, textColorOut.g, textColorOut.b, 0f), FADE_OUT_DURATION)
                .WithEase(Ease.InQuart)
                .BindToColor(announcementText)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            // テキストのスライドアウトアニメーション（中央から右へ加速）
            fadeOutTasks[2] = LMotion.Create(originalPosition, new Vector2(originalPosition.x + SLIDE_DISTANCE, originalPosition.y), FADE_OUT_DURATION)
                .WithEase(Ease.InCubic)
                .BindToAnchoredPosition(textRect)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken);
            
            await UniTask.WhenAll(fadeOutTasks);
            
            // 最終クリーンアップ
            if (announcementBackground && announcementText)
            {
                announcementBackground.gameObject.SetActive(false);
                announcementText.gameObject.SetActive(false);
                textRect.anchoredPosition = originalPosition;
            }
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルされた場合のクリーンアップ
            if (announcementBackground && announcementText)
            {
                announcementBackground.gameObject.SetActive(false);
                announcementText.gameObject.SetActive(false);
                var textRect = announcementText.rectTransform;
                if (textRect)
                {
                    // 位置をリセット
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }
    }
    
    /// <summary>
    /// プレイスタイルが選択された時の処理
    /// </summary>
    private void OnPlayStyleSelected(PlayStyle playStyle)
    {
        _selectedPlayStyle = playStyle;
        UpdatePlayStyleButtonColors(playStyle);
        playStyleSelectedText.text = $"出し方: {_selectedPlayStyle.ToJapaneseString()}";
    }
    
    /// <summary>
    /// プレイスタイルボタンの色を更新
    /// </summary>
    private void UpdatePlayStyleButtonColors(PlayStyle playStyle)
    {
        // 全ボタンをデフォルト色にリセット
        hesitationButton.GetComponent<Image>().color = Color.white;
        impulseButton.GetComponent<Image>().color = Color.white;
        convictionButton.GetComponent<Image>().color = Color.white;
        
        // 選択されたボタンをハイライト
        var selectedButton = playStyle switch
        {
            PlayStyle.Hesitation => hesitationButton,
            PlayStyle.Impulse => impulseButton,
            PlayStyle.Conviction => convictionButton,
            _ => hesitationButton
        };
        
        selectedButton.GetComponent<Image>().color = new Color(0.8f, 1f, 0.8f, 1f); // 淡い緑
    }
    

    
    
    
    /// <summary>
    /// 精神ベットのプラスボタンが押された時の処理
    /// </summary>
    private void OnMentalBetPlus()
    {
        if (_mentalBetValue >= MAX_MENTAL_BET) return;
        
        _mentalBetValue++;
        UpdateMentalBetDisplay();
    }
    
    /// <summary>
    /// 精神ベットのマイナスボタンが押された時の処理
    /// </summary>
    private void OnMentalBetMinus()
    {
        if (_mentalBetValue <= MIN_MENTAL_BET) return;
        
        _mentalBetValue--;
        UpdateMentalBetDisplay();
    }
    
    /// <summary>
    /// 精神ベットの表示を更新
    /// </summary>
    private void UpdateMentalBetDisplay()
    {
        mentalBetValueText.text = _mentalBetValue.ToString();
        
        // ボタンの有効/無効を切り替え
        mentalBetPlusButton.interactable = (_mentalBetValue < MAX_MENTAL_BET);
        mentalBetMinusButton.interactable = (_mentalBetValue > MIN_MENTAL_BET);
    }
    
    /// <summary>
    /// プレイボタンが押された時の処理
    /// </summary>
    private void OnPlayButtonClicked()
    {
        _playButtonClicked.OnNext(Unit.Default);
    }
    
    /// <summary>
    /// プレイボタンとプレイスタイル選択を表示
    /// </summary>
    public void ShowPlayButton()
    {
        playButton.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 選択されたプレイスタイルを取得
    /// </summary>
    public PlayStyle GetSelectedPlayStyle()
    {
        return _selectedPlayStyle;
    }
    
    /// <summary>
    /// 選択された精神ベットを取得
    /// </summary>
    public int GetMentalBetValue()
    {
        return _mentalBetValue;
    }
    
    protected override void Awake()
    {
        base.Awake();
        announcementBackground.gameObject.SetActive(false);
        announcementText.gameObject.SetActive(false);
        
        playButton.onClick.AddListener(OnPlayButtonClicked);
        playButton.gameObject.SetActive(false);
        
        mentalBetPlusButton.onClick.AddListener(OnMentalBetPlus);
        mentalBetMinusButton.onClick.AddListener(OnMentalBetMinus);
        
        _selectedPlayStyle = PlayStyle.Hesitation;
        hesitationButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Hesitation));
        impulseButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Impulse));
        convictionButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Conviction));
        
        UpdateMentalBetDisplay();
    }
    
    private new void OnDestroy()
    {
        _playButtonClicked?.Dispose();
        
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
        
        // ボタンのイベントをクリーンアップ
        hesitationButton.onClick.RemoveAllListeners();
        impulseButton.onClick.RemoveAllListeners();
        convictionButton.onClick.RemoveAllListeners();
        
        // 精神ベットボタンのイベントをクリーンアップ
        mentalBetPlusButton.onClick.RemoveAllListeners();
        mentalBetMinusButton.onClick.RemoveAllListeners();
    }
}
