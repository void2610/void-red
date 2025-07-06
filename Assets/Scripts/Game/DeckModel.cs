using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デッキのデータと操作を担当するModelクラス
/// カードの管理、シャッフル、ドロー機能を提供
/// </summary>
public class DeckModel
{
    public int Count => _deck.Count;
    public bool IsEmpty => _deck.Count == 0;
    public List<CardData> AllCards => new List<CardData>(_deck);
    
    private readonly List<CardData> _deck = new();
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="initialCards">初期カードリスト</param>
    public DeckModel(List<CardData> initialCards)
    {
        if (initialCards is { Count: > 0 })
        {
            _deck.AddRange(initialCards);
            Shuffle();
        }
    }
    
    /// <summary>
    /// デッキを初期化（既存のカードをクリアして新しいカードで置き換え）
    /// </summary>
    /// <param name="cardDataList">デッキに追加するカードリスト</param>
    public void InitializeDeck(List<CardData> cardDataList)
    {
        _deck.Clear();
        if (cardDataList is { Count: > 0 })
        {
            _deck.AddRange(cardDataList);
            Shuffle();
        }
    }
    
    /// <summary>
    /// デッキをクリア
    /// </summary>
    public void Clear()
    {
        _deck.Clear();
    }
    
    /// <summary>
    /// カードを1枚ドロー
    /// </summary>
    /// <returns>ドローしたカード（デッキが空の場合はnull）</returns>
    public CardData DrawCard()
    {
        if (_deck.Count == 0) return null;
        
        var card = _deck[0];
        _deck.RemoveAt(0);
        return card;
    }
    
    /// <summary>
    /// 指定した枚数のカードをドロー
    /// </summary>
    /// <param name="count">ドローする枚数</param>
    /// <returns>ドローしたカードのリスト</returns>
    public List<CardData> DrawCards(int count)
    {
        var drawnCards = new List<CardData>();
        
        for (int i = 0; i < count && _deck.Count > 0; i++)
        {
            drawnCards.Add(DrawCard());
        }
        
        return drawnCards;
    }
    
    /// <summary>
    /// カードをデッキに戻す
    /// </summary>
    /// <param name="cardData">戻すカードデータ</param>
    public void ReturnCard(CardData cardData)
    {
        if (!cardData) return;
        
        _deck.Add(cardData);
        Shuffle();
    }
    
    /// <summary>
    /// 複数のカードをデッキに戻す
    /// </summary>
    /// <param name="cardDataList">戻すカードデータのリスト</param>
    public void ReturnCards(List<CardData> cardDataList)
    {
        if (cardDataList == null || cardDataList.Count == 0) return;
        
        _deck.AddRange(cardDataList);
        Shuffle();
    }
    
    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    public void Shuffle()
    {
        for (int i = 0; i < _deck.Count; i++)
        {
            var temp = _deck[i];
            var randomIndex = Random.Range(i, _deck.Count);
            _deck[i] = _deck[randomIndex];
            _deck[randomIndex] = temp;
        }
    }
    
    /// <summary>
    /// デッキの内容を取得（読み取り専用）
    /// </summary>
    /// <returns>デッキのカードリスト（コピー）</returns>
    public List<CardData> GetDeckContents()
    {
        return new List<CardData>(_deck);
    }
    
    /// <summary>
    /// 指定したインデックスのカードを別のカードで置き換える
    /// </summary>
    /// <param name="index">置き換えるカードのインデックス</param>
    /// <param name="newCard">新しいカード</param>
    public void ReplaceCard(int index, CardData newCard)
    {
        if (index < 0 || index >= _deck.Count || !newCard) return;
        
        _deck[index] = newCard;
    }
}