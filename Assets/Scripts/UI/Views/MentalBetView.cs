using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using R3;

/// <summary>
/// 精神ベット管理を担当するViewクラス
/// </summary>
public class MentalBetView : MonoBehaviour
{
    [SerializeField] private Button mentalBetPlusButton;
    [SerializeField] private Button mentalBetMinusButton;
    [SerializeField] private TextMeshProUGUI mentalBetValueText;
    [SerializeField] private TextMeshProUGUI mentalPowerText;
    
    public Observable<int> MentalBetChanged => _mentalBetChanged;
    
    private readonly Subject<int> _mentalBetChanged = new();
    private int _currentBetValue = 1;
    
    /// <summary>
    /// 精神ベット表示を更新
    /// </summary>
    public void UpdateDisplay(int betValue, int currentMentalPower, int maxMentalPower, int minBet, int maxBet)
    {
        _currentBetValue = betValue;
        mentalBetValueText.text = betValue.ToString();
        mentalPowerText.text = $"{currentMentalPower} / {maxMentalPower}";
        
        // ボタンの有効/無効を切り替え
        mentalBetPlusButton.interactable = (betValue < maxBet && betValue < currentMentalPower);
        mentalBetMinusButton.interactable = (betValue > minBet);
    }
    
    /// <summary>
    /// プラスボタンが押された時の処理
    /// </summary>
    private void OnPlusButtonClicked()
    {
        _mentalBetChanged.OnNext(1); // +1を通知
    }
    
    /// <summary>
    /// マイナスボタンが押された時の処理
    /// </summary>
    private void OnMinusButtonClicked()
    {
        _mentalBetChanged.OnNext(-1); // -1を通知
    }
    
    private void Awake()
    {
        // ボタンイベントの設定
        mentalBetPlusButton.onClick.AddListener(OnPlusButtonClicked);
        mentalBetMinusButton.onClick.AddListener(OnMinusButtonClicked);
    }
    
    private void OnDestroy()
    {
        _mentalBetChanged?.Dispose();
        
        // ボタンのイベントをクリーンアップ
        mentalBetPlusButton.onClick.RemoveListener(OnPlusButtonClicked);
        mentalBetMinusButton.onClick.RemoveListener(OnMinusButtonClicked);
    }
}