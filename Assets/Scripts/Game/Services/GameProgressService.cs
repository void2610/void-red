using System.Collections.Generic;
using R3;
using UnityEngine;

/// <summary>
/// ゲーム全体の進行度を管理するサービス（ファサード）
/// GameStateRepositoryに委譲し、ビジネスロジックを提供
/// </summary>
public class GameProgressService
{
    public Observable<Unit> OnDataSaved => _repository.OnDataSaved;

    /// <summary>主人公が持ち越している感情リソース。階層開始時に補充される</summary>
    public EmotionWallet PlayerWallet => _repository.PlayerWallet;

    public PersonaState Persona => _repository.Persona;
    public IReadOnlyCollection<string> CollapsedParticipantIds => _repository.CollapsedParticipantIds;

    private readonly GameStateRepository _repository;

    public GameProgressService(SaveDataManager saveDataManager)
    {
        _repository = new GameStateRepository(saveDataManager);

        // 現在のノードを初期化
        _repository.StoryProgress.CurrentNode = GetNextNode();
    }

    public bool HasSaveData() => _repository.HasSaveData();

    public StoryNode GetCurrentNode() => _repository.StoryProgress.CurrentNode;

    public SceneType GetNextSceneType() => GetSceneTypeForNode(GetNextNode());

    public string GetNovelKitState() => _repository.NovelProgress.NovelKitState;

    public bool HasCollapsed(string participantId) => _repository.CollapsedParticipantIds.Contains(participantId);

    public void SaveNovelKitState(string state)
    {
        _repository.NovelProgress.NovelKitState = state;
        _repository.SaveAll();
    }

    public void ResetToDefaultData()
    {
        _repository.ResetAll();
        _repository.StoryProgress.CurrentNode = GetNextNode();
    }

    /// <summary>
    /// 次に発生するストーリーノードを決定
    /// ノベルとオークションを交互に並べ、0〜4 階層を順に回る
    /// </summary>
    public StoryNode GetNextNode()
    {
        var currentStep = _repository.StoryProgress.CurrentStep;
        return currentStep switch
        {
            0 => new NovelNode("prologue1", false),
            1 => new AuctionNode(0, false),
            2 => new NovelNode("prologue2"),
            3 => new NovelNode("cerica1", false),
            4 => new NovelNode("cerica2", false),
            5 => new AuctionNode(1),
            6 => new AuctionNode(2),
            7 => new AuctionNode(3),
            8 => new AuctionNode(4),
            _ => new DemoEnding(),
        };
    }

    /// <summary>
    /// 階層開始時の主人公の手持ち。持ち越し分に規定枚数を補充した状態を返す (セーブはしない)
    /// </summary>
    public EmotionWallet PrepareWalletForFloor()
    {
        var wallet = _repository.PlayerWallet.Clone();
        wallet.Refill(GameConstants.EMOTION_REFILL_PER_FLOOR);
        return wallet;
    }

    /// <summary>
    /// 洗礼を終えた階層の結果を記録し、次ノードへ進めてセーブする
    /// </summary>
    public void RecordAuctionClearAndSave(EmotionWallet remainingWallet, WonLot integrated, IEnumerable<WonLot> allWon, IEnumerable<string> collapsedIds)
    {
        _repository.PlayerWallet.LoadCounts(remainingWallet.ToCounts());
        _repository.Persona.Integrate(integrated, allWon);
        _repository.CollapsedParticipantIds.UnionWith(collapsedIds);
        _repository.StoryProgress.AdvanceStep();
        _repository.StoryProgress.CurrentNode = GetNextNode();
        _repository.SaveAll();
    }

    public void RecordNovelResultAndSave()
    {
        _repository.StoryProgress.AdvanceStep();
        _repository.StoryProgress.CurrentNode = GetNextNode();
        _repository.SaveAll();
    }

    private SceneType GetSceneTypeForNode(StoryNode node)
    {
        return node switch
        {
            AuctionNode => SceneType.Auction,
            NovelNode => SceneType.Novel,
            DemoEnding => SceneType.Thanks,
            _ => SceneType.Home
        };
    }
}
