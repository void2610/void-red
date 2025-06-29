using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤーとNPCの基底クラス
/// カードデッキを持ち、カード選択の基本機能を提供
/// 簡略化されたMVPパターンでHandViewを直接使用
/// </summary>
public abstract class BasePlayer : MonoBehaviour
{
    [SerializeField] protected int maxMentalPower = 20;
    [SerializeField] protected int maxHandSize = 3;
    [SerializeField] protected HandView handView;
    
    public ReadOnlyReactiveProperty<CardView> SelectedCard => handView?.SelectedCard;
    public ReadOnlyReactiveProperty<int> MentalPower => _mentalPower;
    public int MaxMentalPower => maxMentalPower;
    
    private DeckModel _deckModel;
    private readonly ReactiveProperty<int> _mentalPower = new ();
    
    public void DrawCard(int count = 1) => DrawCardAsync(count).Forget();
    public void SetHandInteractable(bool interactable) => handView.SetInteractable(interactable);
    public void RestoreMentalPower(int amount) => _mentalPower.Value = Mathf.Min(_mentalPower.Value + amount, maxMentalPower);
    
    /// <summary>
    /// デッキを初期化
    /// </summary>
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deckModel = new DeckModel(cardDataList);
    }
    
    /// <summary>
    /// 精神力を消費する
    /// </summary>
    /// <param name="amount">消費する精神力</param>
    /// <returns>消費に成功したかどうか</returns>
    public void ConsumeMentalPower(int amount)
    {
        if (_mentalPower.Value < amount) return;
        
        _mentalPower.Value -= amount;
        return;
    }
    
    /// <summary>
    /// 選択したカードをプレイ
    /// </summary>
    /// <param name="shouldCollapse">カードが崩壊するかどうか</param>
    public void PlaySelectedCard(bool shouldCollapse)
    {
        var selectedCard = handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 手札からカードを削除
        if (shouldCollapse)
        {
            handView.CollapseCard(selectedCard);
        }
        else
        {
            handView.RemoveCard(selectedCard);
            // 崩壊しない場合はデッキに戻す
            _deckModel.ReturnCard(selectedCard.CardData);
        }
    }
    
    /// <summary>
    /// 選択したカードを崩壊させる
    /// </summary>
    public void CollapseSelectedCard()
    {
        var selectedCard = handView.SelectedCard.CurrentValue;
        if (!selectedCard) return;
        
        // 崩壊演出で手札からカードを削除
        handView.CollapseCard(selectedCard);
    }
    
    /// <summary>
    /// 手札をデッキに戻す
    /// </summary>
    public async UniTask ReturnHandToDeck()
    {
        // 現在の手札のカードデータを取得
        var cardDataList = handView.Cards.CurrentValue.Select(cardView => cardView.CardData).ToList();

        // 手札をデッキに戻すアニメーション
        await handView.ReturnCardsToDeck();
        
        // カードデータをデッキに追加
        _deckModel.ReturnCards(cardDataList);
    }
    
    /// <summary>
    /// カードを引く（アニメーション付き）
    /// </summary>
    private async UniTask DrawCardAsync(int count = 1)
    {
        var cardDataList = new List<CardData>();
        
        for (var i = 0; i < count; i++)
        {
            if (_deckModel.IsEmpty || handView.Count >= maxHandSize) break;
            
            var cardData = _deckModel.DrawCard();
            if (cardData) cardDataList.Add(cardData);
        }
        
        await handView.AddCardsAsync(cardDataList);
    }

    protected virtual void Awake()
    {
        // 精神力を最大値で初期化
        _mentalPower.Value = maxMentalPower;
    }
}