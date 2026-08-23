using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 競合フェーズのView
/// 引き分け時のリアルタイム上乗せUIを管理
/// </summary>
public class CompetitionView : BasePhaseView
{
    [Header("タイトル")]
    [SerializeField] private TMP_Text instructionText;

    [Header("タイマー")]
    [SerializeField] private Image timerImage;
    [SerializeField] private TMP_Text timerText;

    [Header("天秤")]
    [SerializeField] private BalanceTiltController balanceTilt;

    [Header("入札表示")]
    [SerializeField] private TMP_Text playerBidText;
    [SerializeField] private TMP_Text enemyBidText;

    [Header("感情リソース")]
    [SerializeField] private EmotionResourceDisplayView emotionResourceDisplayView;

    [Header("上乗せボタン")]
    [SerializeField] private Button raiseButton;

    [Header("競合者の立ち絵")]
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image rivalPortrait;

    // プレイヤーが上乗せボタンを押した時に発火
    public Observable<Unit> OnRaise => raiseButton.OnClickAsObservable();

    // 感情選択が変更された時に発火
    public Observable<EmotionType> OnEmotionSelected => emotionResourceDisplayView.OnEmotionSelected;

    private const int MAX_BID_FOR_TILT = 3;

    public override void Show() => CanvasGroup.Show();

    /// <summary>
    /// 感情リソース表示を更新
    /// </summary>
    public void UpdateResources(IReadOnlyDictionary<EmotionType, int> resources) => emotionResourceDisplayView.UpdateResources(resources);

    public void SetSelectedEmotion(EmotionType emotion) => emotionResourceDisplayView.SetSelectedEmotion(emotion);

    public void SetEmotionInteractable(bool interactable) => emotionResourceDisplayView.SetInteractable(interactable);

    public void SetRaiseInteractable(bool interactable) => raiseButton.interactable = interactable;

    /// <summary>
    /// 入札額を更新
    /// </summary>
    public void SetInstruction(string text) => instructionText.text = text;

    /// <summary>競合している 2 人の立ち絵を差し替える (素材が無ければ隠す)</summary>
    public void SetPortraits(Sprite player, Sprite rival)
    {
        playerPortrait.sprite = player;
        rivalPortrait.sprite = rival;
        playerPortrait.enabled = player;
        rivalPortrait.enabled = rival;
    }

    /// <summary>
    /// 競合UIを初期化して表示。enemyBid は自分以外の競合者の最高額
    /// </summary>
    public void Initialize(int playerBid, int enemyBid, IReadOnlyDictionary<EmotionType, int> resources)
    {
        playerBidText.text = playerBid.ToString();
        enemyBidText.text = enemyBid.ToString();
        UpdateBalanceTilt(playerBid, enemyBid, animate: false);
        emotionResourceDisplayView.UpdateResources(resources);
        emotionResourceDisplayView.SetSelectedEmotion(EmotionType.Joy);
        Show();
    }

    public void UpdateBids(int playerBid, int enemyBid)
    {
        playerBidText.text = playerBid.ToString();
        enemyBidText.text = enemyBid.ToString();
        UpdateBalanceTilt(playerBid, enemyBid);
    }

    /// <summary>
    /// タイマー表示を更新（0-1の割合）
    /// </summary>
    public void UpdateTimer(float remaining, float max)
    {
        var ratio = Mathf.Clamp01(remaining / max);
        timerImage.fillAmount = ratio;
        timerText.text = Mathf.CeilToInt(remaining).ToString();
    }

    /// <summary>
    /// 天秤の傾きを更新（プレイヤー対敵の差分で-1～+1に正規化）
    /// </summary>
    private void UpdateBalanceTilt(int playerBid, int enemyBid, bool animate = true)
    {
        var diff = playerBid - enemyBid;
        var tilt = Mathf.Clamp((float)diff / MAX_BID_FOR_TILT, -1f, 1f);

        if (animate)
            balanceTilt.AnimateTilt(tilt);
        else
            balanceTilt.SetTilt(tilt);
    }
}
