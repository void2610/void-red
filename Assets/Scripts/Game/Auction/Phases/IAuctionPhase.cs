using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// オークション進行の 1 フェーズ
/// フェーズを足すときはこれを実装して AuctionFlow に差し込む
/// </summary>
public interface IAuctionPhase
{
    /// <summary>このフェーズが今の状態で実行対象かどうか</summary>
    bool CanRun(AuctionSession session);

    UniTask RunAsync(AuctionContext context, CancellationToken ct);
}

/// <summary>
/// フェーズが共有する依存 (セッション / View / 進行度)
/// </summary>
public class AuctionContext
{
    public AuctionSession Session { get; }
    public AuctionSceneView View { get; }
    public GameProgressService Progress { get; }
    public SceneTransitionManager SceneTransition { get; }

    /// <summary>直前のロットで選ばれていた対話相手 (立ち絵の引き継ぎに使う)</summary>
    public AuctionParticipant DialogueTarget { get; set; }

    public AuctionContext(AuctionSession session, AuctionSceneView view, GameProgressService progress, SceneTransitionManager sceneTransition)
    {
        Session = session;
        View = view;
        Progress = progress;
        SceneTransition = sceneTransition;
    }
}
