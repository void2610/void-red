using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

/// <summary>
/// カードの進化・劣化を管理するクラス
/// </summary>
public class EvolutionManager
{
    private readonly StatsTracker _statsTracker;
    
    public EvolutionManager(StatsTracker statsTracker)
    {
        _statsTracker = statsTracker;
    }
    
    /// <summary>
    /// 進化可能なカードをチェックして進化を実行
    /// </summary>
    /// <param name="deckModel">デッキモデル</param>
    /// <returns>進化したカードのリスト</returns>
    private List<(CardData original, CardData evolved)> CheckAndEvolveCards(DeckModel deckModel)
    {
        var evolvedCards = new List<(CardData, CardData)>();
        
        // デッキ内の全カードをチェック
        var cardsToEvolve = new List<(int index, CardData original, CardData target)>();
        
        for (int i = 0; i < deckModel.AllCards.Count; i++)
        {
            var card = deckModel.AllCards[i];
            if (_statsTracker.CanCardEvolve(card))
            {
                cardsToEvolve.Add((i, card, card.EvolutionTarget));
            }
        }
        
        // 進化を実行
        foreach (var (index, original, target) in cardsToEvolve)
        {
            deckModel.ReplaceCard(index, target);
            evolvedCards.Add((original, target));
            
            Debug.Log($"カード進化: {original.CardName} → {target.CardName}");
        }
        
        return evolvedCards;
    }
    
    /// <summary>
    /// プレイヤーのデッキ内の全カードの進化チェックを実行（ゲーム終了時に呼ぶ）
    /// </summary>
    /// <param name="deckModel">デッキモデル</param>
    /// <returns>進化が発生したかどうか</returns>
    public bool ProcessEvolutions(DeckModel deckModel)
    {
        var evolvedCards = CheckAndEvolveCards(deckModel);
        
        if (evolvedCards.Count > 0)
        {
            // 進化通知（演出は後で実装）
            foreach (var (original, evolved) in evolvedCards)
            {
                Debug.Log($"🎉 {original.CardName} が {evolved.CardName} に進化しました！");
            }
            
            return true;
        }
        
        return false;
    }
}