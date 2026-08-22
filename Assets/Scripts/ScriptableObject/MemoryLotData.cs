using UnityEngine;

/// <summary>
/// オークションに出品される記憶 (アーティファクト / ロット)
/// </summary>
[CreateAssetMenu(fileName = "Lot", menuName = "VoidRed/Memory Lot")]
public class MemoryLotData : ScriptableObject
{
    [SerializeField] private string title;
    [SerializeField, TextArea] private string flavor;
    [SerializeField] private EmotionType emotion;
    [Tooltip("記憶テーマとの適合度。プレイヤーには見せず名称とフレーバーから推測させる")]
    [SerializeField, Range(0, 100)] private int resonance;
    [SerializeField] private Sprite image;

    public string LotId => name;
    public string Title => title;
    public string Flavor => flavor;
    public EmotionType Emotion => emotion;
    public int Resonance => resonance;
    public Sprite Image => image;
}
