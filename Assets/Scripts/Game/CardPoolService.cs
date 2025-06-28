using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// カードプールを管理するサービスクラス
/// VContainerによりシングルトンとして管理される
/// </summary>
public class CardPoolService
{
    private readonly AllCardDataList _allCardDataList;
    private readonly List<CardData> _availableCards;
    
    /// <summary>
    /// コンストラクタ（AllCardDataListをDIで受け取る）
    /// </summary>
    /// <param name="allCardDataList">全カードデータリスト</param>
    public CardPoolService(AllCardDataList allCardDataList)
    {
        _allCardDataList = allCardDataList;
        _availableCards = new List<CardData>(_allCardDataList.CardList);
    }
    
    /// <summary>
    /// ランダムなカードを1枚取得
    /// </summary>
    /// <returns>ランダムなカード</returns>
    public CardData GetRandomCard()
    {
        if (_availableCards.Count == 0)
        {
            Debug.LogWarning("利用可能なカードがありません");
            return null;
        }
        
        var randomIndex = Random.Range(0, _availableCards.Count);
        return _availableCards[randomIndex];
    }
    
    /// <summary>
    /// 複数のランダムなカードを取得（重複なし）
    /// </summary>
    /// <param name="count">取得するカード数</param>
    /// <returns>ランダムなカードのリスト</returns>
    public List<CardData> GetRandomCards(int count)
    {
        if (count <= 0) return new List<CardData>();
        if (count >= _availableCards.Count) return new List<CardData>(_availableCards);
        
        // Fisher-Yatesシャッフルアルゴリズムを使用
        var shuffled = new List<CardData>(_availableCards);
        for (var i = 0; i < shuffled.Count; i++)
        {
            var temp = shuffled[i];
            var randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        
        return shuffled.Take(count).ToList();
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
    /// 利用可能なカード数を取得
    /// </summary>
    public int AvailableCardCount => _availableCards.Count;
    
    /// <summary>
    /// 全カード数を取得
    /// </summary>
    public int TotalCardCount => _allCardDataList.Count;
}