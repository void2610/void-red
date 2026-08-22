using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 全階層の定義を階層番号で引くカタログ
/// </summary>
[CreateAssetMenu(fileName = "AllFloorData", menuName = "VoidRed/All Floor Data")]
public class AllFloorData : ScriptableObject
{
    [SerializeField] private List<FloorData> floors = new();

    public int Count => floors.Count;

    public FloorData GetFloor(int floorIndex) => floors.First(f => f.FloorIndex == floorIndex);

    public bool HasFloor(int floorIndex) => floors.Any(f => f.FloorIndex == floorIndex);

    /// <summary>
    /// 全階層のロットと参加者から ID で引く (セーブデータの復元用)
    /// </summary>
    public MemoryLotData FindLot(string lotId) => floors.SelectMany(f => f.Lots).FirstOrDefault(l => l.LotId == lotId);

    public ParticipantData FindParticipant(string participantId) => floors.SelectMany(f => f.Rivals).FirstOrDefault(p => p.ParticipantId == participantId);
}
