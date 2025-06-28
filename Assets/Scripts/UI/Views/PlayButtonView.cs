using UnityEngine;
using UnityEngine.UI;
using System;
using R3;

/// <summary>
/// プレイボタン管理を担当するViewクラス
/// </summary>
public class PlayButtonView : MonoBehaviour
{
    [SerializeField] private Button playButton;

    public Observable<Unit> PlayButtonClicked => _playButtonClicked;
    public void Show() => playButton.gameObject.SetActive(true);
    public void Hide() => playButton.gameObject.SetActive(false);
    
    private void OnPlayButtonClicked() => _playButtonClicked.OnNext(Unit.Default);
    private readonly Subject<Unit> _playButtonClicked = new();
    
    private void Awake()
    {
        // 初期状態は非表示
        playButton.gameObject.SetActive(false);
        // ボタンイベントの設定
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }
    
    private void OnDestroy()
    {
        _playButtonClicked?.Dispose();
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }
}