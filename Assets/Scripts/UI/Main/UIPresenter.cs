using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System;
using VContainer;
using VContainer.Unity;

/// <summary>
/// UIのビジネスロジックとイベント処理を担当するPresenterクラス
/// VContainerで依存性注入される
/// </summary>
public class UIPresenter : MonoBehaviour
{
    [Inject] private readonly CardPoolService _cardPoolService;
    [Inject] private readonly ThemeService _themeService;
    
    public Observable<Unit> PlayButtonClicked => _playButtonView.PlayButtonClicked;
    
    private const int MIN_MENTAL_BET = 1;
    private const int MAX_MENTAL_BET = 7;
    
    private ThemeView _themeView;
    private AnnouncementView _announcementView;
    private PlayButtonView _playButtonView;
    private PlayStyleView _playStyleView;
    private MentalBetView _mentalBetView;
    private PlayStyle _selectedPlayStyle = PlayStyle.Hesitation;
    private int _mentalBetValue = 1;
    [Inject] private Player _player;

    public void SetTheme(ThemeData theme) => _themeView.DisplayTheme(theme.Title);
    public async UniTask ShowAnnouncement(string message, float duration = 2f) => await _announcementView.DisplayAnnouncement(message, duration);
    public void ShowPlayButton() => _playButtonView.Show();
    public void HidePlayButton() => _playButtonView.Hide();
    public PlayStyle GetSelectedPlayStyle() => _selectedPlayStyle;
    public int GetMentalBetValue() => _mentalBetValue;
    
    private void OnPlayStyleSelected(PlayStyle playStyle)
    {
        _selectedPlayStyle = playStyle;
    }
    
    private void OnMentalBetChanged(int delta)
    {
        var newValue = _mentalBetValue + delta;
        
        // 範囲チェック
        if (newValue < MIN_MENTAL_BET || newValue > MAX_MENTAL_BET) return;
        
        _mentalBetValue = newValue;
        UpdateMentalBetDisplay();
    }
    
    private void UpdateMentalBetDisplay()
    {
        var currentMentalPower = _player.MentalPower.CurrentValue;
        
        // 現在のベット値が精神力を超えている場合や最小値を下回る場合は調整
        if (_mentalBetValue > currentMentalPower)
            _mentalBetValue = currentMentalPower;
        if (_mentalBetValue < MIN_MENTAL_BET)
            _mentalBetValue = MIN_MENTAL_BET;
        
        // MentalBetViewに表示を委譲
        _mentalBetView.UpdateDisplay(_mentalBetValue, currentMentalPower, PlayerPresenter.MaxMentalPower, MIN_MENTAL_BET, MAX_MENTAL_BET);
    }
    
    private void SetupViewEvents()
    {
        // プレイスタイル選択イベント
        _playStyleView.PlayStyleSelected.Subscribe(OnPlayStyleSelected).AddTo(this);
        // 精神ベット変更イベント
        _mentalBetView.MentalBetChanged.Subscribe(OnMentalBetChanged).AddTo(this);
    }
    
    private void Awake()
    {
        _themeView = FindFirstObjectByType<ThemeView>();
        _announcementView = FindFirstObjectByType<AnnouncementView>();
        _playButtonView = FindFirstObjectByType<PlayButtonView>();
        _playStyleView = FindFirstObjectByType<PlayStyleView>();
        _mentalBetView = FindFirstObjectByType<MentalBetView>();
    }
    
    private void Start()
    {
        // プレイヤーの精神力変化を監視
        _player.MentalPower.Subscribe(_ => UpdateMentalBetDisplay()).AddTo(this);
        
        // Viewイベントの設定
        SetupViewEvents();
        
        // 初期表示の更新
        OnPlayStyleSelected(_selectedPlayStyle);
        UpdateMentalBetDisplay();
    }
}