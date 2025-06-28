using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全てのCardDataを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "AllCardData", menuName = "VoidRed/All Card Data")]
public class AllCardData : ScriptableObject
{
    [SerializeField] private List<CardData> cardList = new ();
    
    // プロパティ
    public List<CardData> CardList => cardList;
    public int Count => cardList.Count;
    
    /// <summary>
    /// 同じディレクトリ内の全てのCardDataを自動的に登録
    /// </summary>
    public void RegisterAllCards()
    {
#if UNITY_EDITOR
        // このScriptableObjectと同じディレクトリパスを取得
        var path = AssetDatabase.GetAssetPath(this);
        path = System.IO.Path.GetDirectoryName(path);
        
        // 指定ディレクトリ内の全てのCardDataを検索
        var guids = AssetDatabase.FindAssets("t:CardData", new[] { path });
        
        // 検索結果をリストに追加
        cardList.Clear();
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            if (cardData) cardList.Add(cardData);
        }
        
        // カード名でソート
        cardList = cardList.OrderBy(x => x.CardName).ToList();
        
        // ScriptableObjectを更新
        EditorUtility.SetDirty(this);
        
        Debug.Log($"カードを{cardList.Count}枚登録しました");
#endif
    }
    
    /// <summary>
    /// ランダムなカードを取得
    /// </summary>
    public CardData GetRandomCard()
    {
        if (cardList.Count == 0) return null;
        return cardList[Random.Range(0, cardList.Count)];
    }
    
    /// <summary>
    /// 複数のランダムなカードを取得（重複なし）
    /// </summary>
    public List<CardData> GetRandomCards(int count)
    {
        if (count >= cardList.Count)
        {
            return new List<CardData>(cardList);
        }
        
        var shuffled = new List<CardData>(cardList);
        for (int i = 0; i < shuffled.Count; i++)
        {
            var temp = shuffled[i];
            var randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        
        return shuffled.Take(count).ToList();
    }
    
}