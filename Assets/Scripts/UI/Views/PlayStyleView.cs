using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using R3;

/// <summary>
/// プレイスタイル選択を担当するViewクラス
/// </summary>
public class PlayStyleView : MonoBehaviour
{
    [SerializeField] private Button hesitationButton;
    [SerializeField] private Button impulseButton;
    [SerializeField] private Button convictionButton;
    [SerializeField] private TextMeshProUGUI playStyleSelectedText;
    
    public Observable<PlayStyle> PlayStyleSelected => _playStyleSelected;
    
    private readonly Subject<PlayStyle> _playStyleSelected = new();
    private PlayStyle _currentPlayStyle = PlayStyle.Hesitation;
    
    /// <summary>
    /// プレイスタイル表示を更新
    /// </summary>
    private void UpdateDisplay(PlayStyle playStyle)
    {
        _currentPlayStyle = playStyle;
        playStyleSelectedText.text = $"出し方: {playStyle.ToJapaneseString()}";
        UpdateButtonColors(playStyle);
    }
    
    /// <summary>
    /// プレイスタイルボタンの色を更新
    /// </summary>
    private void UpdateButtonColors(PlayStyle playStyle)
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
    /// プレイスタイルが選択された時の処理
    /// </summary>
    private void OnPlayStyleSelected(PlayStyle playStyle)
    {
        UpdateDisplay(playStyle);
        _playStyleSelected.OnNext(playStyle);
    }
    
    private void Awake()
    {
        // ボタンイベントの設定
        hesitationButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Hesitation));
        impulseButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Impulse));
        convictionButton.onClick.AddListener(() => OnPlayStyleSelected(PlayStyle.Conviction));
        
        // 初期表示
        UpdateDisplay(_currentPlayStyle);
    }
    
    private void OnDestroy()
    {
        _playStyleSelected?.Dispose();
        hesitationButton.onClick.RemoveAllListeners();
        impulseButton.onClick.RemoveAllListeners();
        convictionButton.onClick.RemoveAllListeners();
    }
}