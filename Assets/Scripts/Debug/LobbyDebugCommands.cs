using System;
using System.Linq;
using UnityEngine;
using Void2610.LiminalPalette;

/// <summary>
/// ロビー (HomeScene) の記憶コレクション / 人格画面を LiminalPalette から観測するデバッグコマンド群
/// </summary>
public sealed class LobbyDebugCommands
{
    private readonly GameProgressService _progress;
    private readonly AllFloorData _floors;

    public LobbyDebugCommands(GameProgressService progress, AllFloorData floors)
    {
        _progress = progress;
        _floors = floors;
    }

    [LiminalCommand("Lobby/PersonaIntegratedContains", Description = "人格画面に、統合した記憶 (統合順の番号) の名前が表示されているか")]
    public bool PersonaIntegratedContains(int lotIndex) => PersonaIntegratedText().Contains(_floors.FindLot(_progress.Persona.IntegratedLotIds[lotIndex]).Title);

    [LiminalCommand("Lobby/ProgressText", Description = "ホームに表示中の進行案内を返す")]
    public string ProgressText() => UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "ProgressText").text;

    [LiminalCommand("Lobby/CollectionShowing", Description = "記憶コレクション画面が開いているか")]
    public bool CollectionShowing() => Home().CollectionView.IsShowing;

    [LiminalCommand("Lobby/CollectionSummary", Description = "記憶コレクションの収集数表示を返す")]
    public string CollectionSummary() => Home().CollectionView.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(t => t.name == "SummaryText").text;

    [LiminalCommand("Lobby/CollectionEntryCount", Description = "記憶コレクションに並んだ行数を返す")]
    public int CollectionEntryCount() => Home().CollectionView.GetComponentsInChildren<MemoryCollectionEntryView>(true).Length;

    [LiminalCommand("Lobby/CollectionRevealedCount", Description = "伏せ字でない (収集済みの) 行数を返す")]
    public int CollectionRevealedCount() => Home().CollectionView.GetComponentsInChildren<MemoryCollectionEntryView>(true).Count(e => !e.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(t => t.name == "TitleText").text.Contains("？？？"));

    [LiminalCommand("Lobby/PersonaShowing", Description = "人格画面が開いているか")]
    public bool PersonaShowing() => Home().PersonaView.IsShowing;

    [LiminalCommand("Lobby/PersonaIntegratedText", Description = "人格画面の統合済み記憶の表示を返す")]
    public string PersonaIntegratedText() => Home().PersonaView.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(t => t.name == "IntegratedText").text;

    private static HomeView Home()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<HomeView>();
        if (view == null) throw new InvalidOperationException("HomeScene ではない");
        return view;
    }
}
