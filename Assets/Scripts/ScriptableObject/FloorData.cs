using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1 階層分のオークション定義 (参加者 / 記憶テーマ / 出品ロット)
/// </summary>
[CreateAssetMenu(fileName = "Floor", menuName = "VoidRed/Floor")]
public class FloorData : ScriptableObject
{
    [SerializeField] private int floorIndex;
    [SerializeField] private string themeTitle;
    [SerializeField] private string clarifiedTheme;
    [Tooltip("主人公を除く 4 名")]
    [SerializeField] private List<ParticipantData> rivals = new();
    [Tooltip("出品される 5 個。出現順は実行時にシャッフルする")]
    [SerializeField] private List<MemoryLotData> lots = new();

    public int FloorIndex => floorIndex;
    public string ThemeTitle => themeTitle;
    public string ClarifiedTheme => clarifiedTheme;
    public IReadOnlyList<ParticipantData> Rivals => rivals;
    public IReadOnlyList<MemoryLotData> Lots => lots;
}
