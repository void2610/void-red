using System.Collections.Generic;
using System.Linq;
using R3;

/// <summary>
/// 手札のデータを管理するModelクラス
/// Pure C#で実装され、UIに依存しない
/// </summary>
public class HandModel
{
    // 公開プロパティ
    public ReadOnlyReactiveProperty<List<CardData>> Cards => _cards;
    public ReadOnlyReactiveProperty<int> SelectedIndex => _selectedIndex;
    public ReadOnlyReactiveProperty<CardData> SelectedCard { get; }
    public int Count => _cards.Value.Count;
    public int MaxHandSize { get; }
    
    // 手札のカードデータ
    private readonly ReactiveProperty<List<CardData>> _cards = new(new List<CardData>());
    private readonly ReactiveProperty<int> _selectedIndex = new(-1);
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="maxHandSize">手札の最大枚数</param>
    public HandModel(int maxHandSize = 3)
    {
        MaxHandSize = maxHandSize;
        
        // 選択されたカードを計算プロパティとして公開
        SelectedCard = _selectedIndex.CombineLatest(_cards, (index, cards) => 
            index >= 0 && index < cards.Count ? cards[index] : null)
            .ToReadOnlyReactiveProperty();
    }
    
    /// <summary>
    /// カードを追加
    /// </summary>
    /// <param name="card">追加するカード</param>
    /// <returns>追加に成功したか</returns>
    public bool TryAddCard(CardData card)
    {
        if (_cards.Value.Count >= MaxHandSize) return false;
        
        // リスト自体を変更しないと通知が行われないため、新しいリストを作成
        var newList = new List<CardData>(_cards.Value) { card };
        _cards.Value = newList;
        return true;
    }
    
    /// <summary>
    /// 複数のカードを追加
    /// </summary>
    /// <param name="cards">追加するカードリスト</param>
    /// <returns>追加されたカードの数</returns>
    public int TryAddCards(IEnumerable<CardData> cards)
    {
        var addedCount = 0;
        foreach (var card in cards)
        {
            if (TryAddCard(card))
                addedCount++;
            else
                break;
        }
        return addedCount;
    }
    
    /// <summary>
    /// カードを削除
    /// </summary>
    /// <param name="card">削除するカード</param>
    /// <returns>削除に成功したか</returns>
    public bool TryRemoveCard(CardData card)
    {
        var index = _cards.Value.IndexOf(card);
        if (index < 0) return false;
        
        var newList = new List<CardData>(_cards.Value);
        newList.RemoveAt(index);
        _cards.Value = newList;
        
        // 選択インデックスの調整
        if (_selectedIndex.Value == index)
        {
            _selectedIndex.Value = -1;
        }
        else if (_selectedIndex.Value > index)
        {
            _selectedIndex.Value--;
        }
        
        return true;
    }
    
    /// <summary>
    /// インデックスでカードを削除
    /// </summary>
    /// <param name="index">削除するカードのインデックス</param>
    /// <returns>削除に成功したか</returns>
    public bool TryRemoveCardAt(int index)
    {
        if (index < 0 || index >= _cards.Value.Count) return false;
        
        return TryRemoveCard(_cards.Value[index]);
    }
    
    /// <summary>
    /// カードを選択
    /// </summary>
    /// <param name="index">選択するカードのインデックス</param>
    public void SelectCardAt(int index)
    {
        if (index >= 0 && index < _cards.Value.Count)
            _selectedIndex.Value = index;
        else
            _selectedIndex.Value = -1;
    }
    
    /// <summary>
    /// カードデータで選択
    /// </summary>
    /// <param name="card">選択するカード</param>
    public void SelectCard(CardData card)
    {
        var index = _cards.Value.IndexOf(card);
        SelectCardAt(index);
    }
    
    /// <summary>
    /// 選択を解除
    /// </summary>
    public void DeselectCard()
    {
        _selectedIndex.Value = -1;
    }
    
    /// <summary>
    /// 手札をクリア
    /// </summary>
    private void Clear()
    {
        _cards.Value = new List<CardData>();
        _selectedIndex.Value = -1;
    }
    
    /// <summary>
    /// 全てのカードを取得してクリア
    /// </summary>
    /// <returns>取得したカードリスト</returns>
    public List<CardData> TakeAllCards()
    {
        var cards = new List<CardData>(_cards.Value);
        Clear();
        return cards;
    }
    
    /// <summary>
    /// インデックスでカードを取得
    /// </summary>
    /// <param name="index">インデックス</param>
    /// <returns>カードデータ（存在しない場合はnull）</returns>
    public CardData GetCardAt(int index)
    {
        if (index < 0 || index >= _cards.Value.Count) return null;
        return _cards.Value[index];
    }
    
    /// <summary>
    /// ランダムなカードを取得
    /// </summary>
    /// <returns>ランダムに選ばれたカード（手札が空の場合はnull）</returns>
    public CardData GetRandomCard()
    {
        if (_cards.Value.Count == 0) return null;
        var randomIndex = UnityEngine.Random.Range(0, _cards.Value.Count);
        return _cards.Value[randomIndex];
    }
}