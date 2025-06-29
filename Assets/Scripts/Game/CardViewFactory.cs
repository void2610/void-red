using UnityEngine;
using R3;

/// <summary>
/// CardViewの生成を担当するFactoryクラス
/// カード生成ロジックの一元化と再利用性を提供
/// </summary>
public class CardViewFactory
{
    private readonly CardView _cardPrefab;
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="cardPrefab">カードプレハブ</param>
    public CardViewFactory(CardView cardPrefab)
    {
        _cardPrefab = cardPrefab;
    }
    
    /// <summary>
    /// 基本的なカードを生成
    /// </summary>
    /// <param name="cardData">カードデータ</param>
    /// <param name="parent">親Transform</param>
    /// <returns>生成されたCardView</returns>
    public CardView CreateCard(CardData cardData, Transform parent)
    {
        var cardView = Object.Instantiate(_cardPrefab, parent);
        cardView.Initialize(cardData);
        return cardView;
    }
    
    /// <summary>
    /// インタラクト設定付きでカードを生成
    /// </summary>
    /// <param name="cardData">カードデータ</param>
    /// <param name="parent">親Transform</param>
    /// <param name="isInteractable">インタラクト可能かどうか</param>
    /// <returns>生成されたCardView</returns>
    public CardView CreateInteractiveCard(CardData cardData, Transform parent, bool isInteractable)
    {
        var cardView = CreateCard(cardData, parent);
        cardView.SetInteractable(isInteractable);
        return cardView;
    }
    
    /// <summary>
    /// クリックイベント付きでカードを生成
    /// </summary>
    /// <param name="cardData">カードデータ</param>
    /// <param name="parent">親Transform</param>
    /// <param name="onClickAction">クリック時のアクション</param>
    /// <param name="disposableContainer">Disposableを追加するGameObject</param>
    /// <param name="isInteractable">インタラクト可能かどうか</param>
    /// <returns>生成されたCardView</returns>
    public CardView CreateCardWithClickEvent(
        CardData cardData, 
        Transform parent, 
        System.Action<CardView> onClickAction,
        GameObject disposableContainer,
        bool isInteractable = true)
    {
        var cardView = CreateInteractiveCard(cardData, parent, isInteractable);
        
        // クリックイベントを登録
        if (onClickAction != null)
        {
            cardView.OnClicked.Subscribe(onClickAction).AddTo(disposableContainer);
        }
        
        return cardView;
    }
    
    /// <summary>
    /// 設定済みカードを生成（HandView専用）
    /// </summary>
    /// <param name="cardData">カードデータ</param>
    /// <param name="parent">親Transform</param>
    /// <param name="onClickAction">クリック時のアクション</param>
    /// <param name="disposableContainer">Disposableを追加するGameObject</param>
    /// <param name="isInteractable">インタラクト可能かどうか</param>
    /// <returns>生成されたCardView</returns>
    public CardView CreateHandCard(
        CardData cardData, 
        Transform parent, 
        System.Action<CardView> onClickAction,
        GameObject disposableContainer,
        bool isInteractable)
    {
        return CreateCardWithClickEvent(cardData, parent, onClickAction, disposableContainer, isInteractable);
    }
}