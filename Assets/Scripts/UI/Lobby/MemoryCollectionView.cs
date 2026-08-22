using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ロビーの記憶コレクション。全階層の記憶を並べ、落札して持ち帰ったものだけ中身を見せる
/// </summary>
public class MemoryCollectionView : BaseWindowView
{
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI summaryText;

    private readonly List<MemoryCollectionEntryView> _entries = new();

    public void Show(AllFloorData floors, PersonaState persona)
    {
        foreach (var e in _entries) Destroy(e.gameObject);
        _entries.Clear();
        var total = 0;
        for (var floor = 0; floor < floors.Count; floor++)
        {
            foreach (var lot in floors.GetFloor(floor).Lots)
            {
                var entry = Instantiate(entryPrefab, entryContainer).GetComponent<MemoryCollectionEntryView>();
                entry.Bind(lot, persona.CollectionLotIds.Contains(lot.LotId), persona.IntegratedLotIds.Contains(lot.LotId));
                _entries.Add(entry);
                total++;
            }
        }
        summaryText.text = $"収集 {persona.CollectionLotIds.Count} / {total}";
        Show();
    }
}
