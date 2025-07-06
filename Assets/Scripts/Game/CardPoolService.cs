using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// カードプールを管理するサービスクラス
/// VContainerによりシングルトンとして管理される
/// </summary>
public class CardPoolService
{
    public int AvailableCardCount => _availableCards.Count;
    public int TotalCardCount => _allCardData.Count;
    
    private readonly AllCardData _allCardData;
    private readonly List<CardData> _availableCards;
    
    /// <summary>
    /// コンストラクタ（AllCardDataListをDIで受け取る）
    /// </summary>
    /// <param name="allCardData">全カードデータリスト</param>
    public CardPoolService(AllCardData allCardData)
    {
        _allCardData = allCardData;
        // 進化・劣化先カードを除外して利用可能カードリストを作成
        _availableCards = _allCardData.CardList
            .Where(card => !card.IsTransformationTarget)
            .ToList();
    }
    
    /// <summary>
    /// ランダムなカードを1枚取得
    /// </summary>
    /// <returns>ランダムなカード</returns>
    public CardData GetRandomCard()
    {
        if (_availableCards.Count == 0)
        {
            return null;
        }
        
        var randomIndex = Random.Range(0, _availableCards.Count);
        return _availableCards[randomIndex];
    }
    
    /// <summary>
    /// 複数のランダムなカードを取得（重複あり）
    /// </summary>
    /// <param name="count">取得するカード数</param>
    /// <returns>ランダムなカードのリスト</returns>
    public List<CardData> GetRandomCards(int count)
    {
        if (count <= 0) return new List<CardData>();
        if (_availableCards.Count == 0)
        {
            return new List<CardData>();
        }
        
        var result = new List<CardData>();
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(0, _availableCards.Count);
            result.Add(_availableCards[randomIndex]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 指定した条件に合うカードを取得
    /// </summary>
    /// <param name="predicate">検索条件</param>
    /// <returns>条件に合うカードのリスト</returns>
    public List<CardData> GetCardsWhere(System.Func<CardData, bool> predicate)
    {
        return _availableCards.Where(predicate).ToList();
    }
    
    /// <summary>
    /// 特定のカードIDでカードを取得
    /// </summary>
    /// <param name="cardName">カード名</param>
    /// <returns>見つかったカード（存在しない場合はnull）</returns>
    public CardData GetCardByName(string cardName)
    {
        return _availableCards.FirstOrDefault(card => card.CardName == cardName);
    }
    
    /// <summary>
    /// 初期デッキに使用可能なカードの数を取得
    /// </summary>
    /// <returns>進化・劣化先を除いたカード数</returns>
    public int GetInitialDeckCardCount()
    {
        return _availableCards.Count;
    }
    
}