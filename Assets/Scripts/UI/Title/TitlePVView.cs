using System.IO;
using LitMotion;
using UnityEngine;
using UnityEngine.Video;
using Void2610.UnityTemplate;

/// <summary>
/// タイトル画面のPV再生View
/// VideoPlayerとCanvasGroupのラッパー
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(VideoPlayer))]
public class TitlePVView : MonoBehaviour
{
    public bool IsPlaying => _videoPlayer && _videoPlayer.isPlaying;

    private const float FADE_DURATION = 0.5f;

    // StreamingAssets配下からの相対パス。VideoClipではなくURL経由でAVFoundationに直接渡す
    private const string RELATIVE_VIDEO_PATH = "Title/pv.mp4";

    private CanvasGroup _canvasGroup;
    private VideoPlayer _videoPlayer;
    private MotionHandle _fadeHandle;

    public void Play()
    {
        _fadeHandle.TryCancel();
        _videoPlayer.Play();
        _fadeHandle = _canvasGroup.FadeIn(FADE_DURATION, Ease.OutQuart, ignoreTimeScale: true);
    }

    public void Stop()
    {
        _fadeHandle.TryCancel();
        _videoPlayer.Stop();
        _fadeHandle = _canvasGroup.FadeOut(FADE_DURATION, Ease.InQuart, ignoreTimeScale: true);
    }

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.Hide();
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = Path.Combine(Application.streamingAssetsPath, RELATIVE_VIDEO_PATH);
        _videoPlayer.isLooping = true;
        _videoPlayer.Stop();
    }

    private void OnDestroy()
    {
        _fadeHandle.TryCancel();
    }
}
