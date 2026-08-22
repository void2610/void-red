using System.Collections.Generic;

/// <summary>
/// 主人公の人格。洗礼で統合した記憶と、そこから決まる感情状態
/// 感情状態はプレイヤーに表示しない
/// </summary>
public class PersonaState
{
    public EmotionType? EmotionState { get; private set; }
    public List<string> IntegratedLotIds { get; } = new();
    public List<string> CollectionLotIds { get; } = new();
    public int TotalDistortion { get; private set; }

    /// <summary>
    /// 落札した記憶のうち 1 つを人格に統合する。残りはコレクションに入るだけ
    /// 感情状態は入札内訳で最も多かった属性になる (同数なら記憶の属性。歪みが無ければ必ず一致する)
    /// </summary>
    public void Integrate(WonLot chosen, IEnumerable<WonLot> allWon)
    {
        IntegratedLotIds.Add(chosen.Lot.LotId);
        TotalDistortion += chosen.Distortion;
        EmotionState = chosen.Bid.DominantEmotion(chosen.Lot.Emotion);
        foreach (var w in allWon) if (!CollectionLotIds.Contains(w.Lot.LotId)) CollectionLotIds.Add(w.Lot.LotId);
    }

    public void Load(int emotionState, IEnumerable<string> integrated, IEnumerable<string> collection, int totalDistortion)
    {
        EmotionState = emotionState < 0 ? null : (EmotionType)emotionState;
        IntegratedLotIds.Clear();
        IntegratedLotIds.AddRange(integrated);
        CollectionLotIds.Clear();
        CollectionLotIds.AddRange(collection);
        TotalDistortion = totalDistortion;
    }

    public void Reset()
    {
        EmotionState = null;
        IntegratedLotIds.Clear();
        CollectionLotIds.Clear();
        TotalDistortion = 0;
    }
}
