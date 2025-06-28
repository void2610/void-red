using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;
using System.Threading;

/// <summary>
/// アナウンスメント表示を担当するViewクラス
/// </summary>
public class AnnouncementView : MonoBehaviour
{
    [SerializeField] private Image announcementBackground;
    [SerializeField] private TextMeshProUGUI announcementText;
    
    // アニメーション定数
    private const float FADE_IN_DURATION = 0.3f;
    private const float FADE_OUT_DURATION = 0.3f;
    private const float SLIDE_DISTANCE = 350f;
    
    // 現在実行中のアナウンスメントのキャンセレーショントークン
    private CancellationTokenSource _currentAnnouncementCts;

    /// <summary>
    /// アナウンスメントを表示
    /// </summary>
    public async UniTask DisplayAnnouncement(string message, float duration = 2f)
    {
        // 現在実行中のアナウンスメントをキャンセル
        _currentAnnouncementCts?.Cancel();
        _currentAnnouncementCts?.Dispose();
        
        // 新しいキャンセレーショントークンを作成
        _currentAnnouncementCts = new CancellationTokenSource();
        
        // アプリケーション終了時にもキャンセルされるようにする  
        var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
            _currentAnnouncementCts.Token,
            this.GetCancellationTokenOnDestroy(), 
            Application.exitCancellationToken
        ).Token;
        
        try
        {
            // テキストの位置をリセット（前回のアニメーションの影響を除去）
            var textRect = announcementText.rectTransform;
            var originalPosition = Vector2.zero;
            textRect.anchoredPosition = originalPosition;
            
            // メッセージを設定
            announcementText.text = message;
            
            // 初期状態を設定
            announcementBackground.gameObject.SetActive(true);
            announcementText.gameObject.SetActive(true);
            announcementBackground.color = new Color(announcementBackground.color.r, announcementBackground.color.g, announcementBackground.color.b, 0f);
            announcementText.color = new Color(announcementText.color.r, announcementText.color.g, announcementText.color.b, 0f);
            
            // テキストを左側に配置
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
            
            // テキストのスライドインアニメーション
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
            fadeOutTasks[0] = LMotion.Create(new Color(bgColorOut.r, bgColorOut.g, bgColorOut.b, 0.95f), new Color(bgColorOut.r, bgColorOut.g, bgColorOut.b, 0f), FADE_OUT_DURATION)
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
            
            // テキストのスライドアウトアニメーション
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
                textRect.anchoredPosition = Vector2.zero;
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
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }
    }
    
    private void Awake()
    {
        // 初期状態の設定
        announcementBackground.gameObject.SetActive(false);
        announcementText.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // アナウンスメントのキャンセレーショントークンをクリーンアップ
        _currentAnnouncementCts?.Cancel();
        _currentAnnouncementCts?.Dispose();
    }
}